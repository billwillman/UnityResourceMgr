using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceMgr: Singleton<ResourceMgr>
{

	public void LoadConfigs(Action<bool> OnFinish)
	{
		AssetLoader loader = mAssetLoader as AssetLoader;
		if (loader != null) {
			loader.LoadConfigs(OnFinish);
		}
	}

	public bool LoadScene(string sceneName, bool isAdd)
	{
		/*string realSceneName = sceneName;


		IResourceLoader loader = GetLoader (ref realSceneName);
		if (loader == null)
			return false;

		if (!loader.OnSceneLoad (realSceneName))
			return false;*/

		if (mAssetLoader.OnSceneLoad (sceneName)) {
			LogMgr.Instance.Log (string.Format ("Loading AssetBundle Scene: {0}", sceneName));
		} else {
#if UNITY_EDITOR
			if (!Application.CanStreamedLevelBeLoaded (sceneName))
				return false;
#endif
			if (mResLoader.OnSceneLoad(sceneName))
				LogMgr.Instance.Log(string.Format("Loading Resources Scene: {0}", sceneName));
			else
				return false;
		}

		if (isAdd)
			Application.LoadLevelAdditive (sceneName);
		else
			Application.LoadLevel (sceneName);
		
		return true;
	}

	private bool DoLoadSceneAsync(string sceneName, bool isAdd, Action<AsyncOperation> onProcess)
	{
		if (isAdd) {
			AsyncOperation opt = Application.LoadLevelAdditiveAsync (sceneName);
			if (opt == null)
				return false;
			
			if (opt.isDone)
			{
				if (onProcess != null)
					onProcess(opt);
				return true;
			}
			
			return AsyncOperationMgr.Instance.AddAsyncOperation<AsyncOperation> (opt, onProcess) != null;
		} else {
			AsyncOperation opt = Application.LoadLevelAsync(sceneName);
			if (opt == null)
				return false;
			if (opt.isDone)
			{
				if (onProcess != null)
					onProcess(opt);
				return true;
			}
			return AsyncOperationMgr.Instance.AddAsyncOperation<AsyncOperation> (opt, onProcess) != null;
		}
	}

	public bool LoadSceneAsync(string sceneName, bool isAdd, Action<AsyncOperation> onProcess)
	{
		if (mAssetLoader.OnSceneLoad (sceneName)) {
			LogMgr.Instance.Log (string.Format ("Loading AssetBundle Scene: {0}", sceneName));
		} else {
#if UNITY_EDITOR
			if (!Application.CanStreamedLevelBeLoaded (sceneName))
				return false;
#endif
			if (mResLoader.OnSceneLoad(sceneName))
				LogMgr.Instance.Log(string.Format("Loading Resources Scene: {0}", sceneName));
			else
				return false;
		}

		return DoLoadSceneAsync(sceneName, isAdd, onProcess);
	}

	public void CloseScene(string sceneName)
	{
		/*
		string realSceneName = sceneName;
		IResourceLoader loader = GetLoader (ref realSceneName);
		if (loader == null)
			return;
		loader.OnSceneClose (realSceneName);*/
		if (!mAssetLoader.OnSceneClose (sceneName))
			mResLoader.OnSceneClose (sceneName);
#if UNITY_5_2
		Application.UnloadLevel(sceneName);
#endif
		// 清除
		AssetCacheManager.Instance.ClearUnUsed ();
	}

	public GameObject CreateGameObject(string fileName)
	{
		GameObject ret = CreatePrefab(fileName);
		if (ret != null)
		{
			ret.AddComponent<ResInstDestroy>();
		}

		return ret;
	}

	public GameObject CreateGameObject(string fileName, float delayDestroyTime)
	{
		GameObject ret = CreatePrefab(fileName);
		if (ret != null)
		{
			ResInstDelayDestroy script = ret.AddComponent<ResInstDelayDestroy>();
			script.DelayDestroyTime = delayDestroyTime;
		}

		return ret;
	}

	private GameObject CreateGameObject(GameObject orgObject)
	{
		if (orgObject == null)
			return null;
		GameObject ret = GameObject.Instantiate (orgObject) as GameObject;
		if (ret != null) {
			AssetCacheManager.Instance._OnCreateGameObject (ret, orgObject);
			// 加入一个脚�?			// ret.AddComponent<ResInstDestroy>();
		}

		return ret;
	}

	public void OnDestroyInstObject(UnityEngine.GameObject instObj)
	{
		if (instObj == null)
			return;
		OnDestroyInstObject (instObj.GetInstanceID ());
	}

	public void OnDestroyInstObject(int instID)
	{
		AssetCacheManager.Instance._OnDestroyGameObject (instID);
	}

	public void DestroyObject(UnityEngine.Object obj)
	{
		if (obj == null)
			return;

		if (obj is Transform)
			obj = (obj as Transform).gameObject;

		UnityEngine.Component comp = obj as UnityEngine.Component;
		if (comp != null) {

			if (Application.isPlaying)
				UnityEngine.GameObject.Destroy (obj);
			else
				UnityEngine.GameObject.DestroyImmediate(obj);
			//UnityEngine.GameObject.DestroyImmediate(obj);
			return;
		}

		if (!UnLoadOrgObject (obj)) {
			UnityEngine.GameObject gameObj = obj as GameObject;
			if (gameObj != null)
			{
				int instId = obj.GetInstanceID ();

				// gameObj.transform.parent = null;
				if (Application.isPlaying)
					UnityEngine.GameObject.Destroy (obj);
				else
					UnityEngine.GameObject.DestroyImmediate (obj);
				//UnityEngine.GameObject.DestroyImmediate (obj);

				AssetCacheManager.Instance._OnDestroyGameObject (instId);
			} else
			{
				if (Application.isPlaying)
					UnityEngine.GameObject.Destroy (obj);
				else
					UnityEngine.GameObject.DestroyImmediate (obj);

			}
		}
	}
	
	public GameObject LoadPrefab(string fileName, ResourceCacheType cacheType)
	{
		GameObject ret = mAssetLoader.LoadPrefab (fileName, cacheType);
		if (ret != null)
			return ret;

		/*string realFileName = fileName;

		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadPrefab (realFileName, cacheType);*/
		return mResLoader.LoadPrefab(fileName, cacheType);
	}

	public bool LoadPrefabAsync(string fileName, Action<float, bool, GameObject> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadPrefabAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadPrefabAsync (realFileName, cacheType, onProcess);*/
		return mResLoader.LoadPrefabAsync(fileName, cacheType, onProcess);
	}

	public bool CreateGameObjectAsync(string fileName, Action<float, bool, GameObject> onProcess)
	{
		bool ret = CreatePrefabAsync(fileName,
		  delegate (float process, bool isDone, GameObject instObj){
			if (isDone)
			{
				if (instObj != null)
					instObj.AddComponent<ResInstDestroy>();
				if (onProcess != null)
					onProcess(process, isDone, instObj);
				return;
			}

			if (onProcess != null)
				onProcess(process, isDone, instObj);
		  }
		);

		return ret;
	}

	public bool CreateGameObjectAsync(string fileName, float delayDestroyTime, Action<float, bool, GameObject> onProcess)
	{
		bool ret = CreatePrefabAsync(fileName,
		                             delegate (float process, bool isDone, GameObject instObj){
			if (isDone)
			{
				if (instObj != null)
				{
					ResInstDelayDestroy script = instObj.AddComponent<ResInstDelayDestroy>();
					script.DelayDestroyTime = delayDestroyTime;
				}
				if (onProcess != null)
					onProcess(process, isDone, instObj);
				return;
			}
			
			if (onProcess != null)
				onProcess(process, isDone, instObj);
		}
		);
		
		return ret;
	}

	private GameObject CreatePrefab(string fileName)
	{
		GameObject orgObj = LoadPrefab (fileName, ResourceCacheType.rctTemp);
		if (orgObj != null) {
			return CreateGameObject(orgObj);
		}

		return null;
	}

	private bool CreatePrefabAsync(string fileName, Action<float, bool, GameObject> onProcess)
	{
		bool ret = LoadPrefabAsync (fileName,
		                           delegate (float t, bool isDone, GameObject orgObj) {
			if (isDone)
			{
				GameObject obj = null;
				if (orgObj != null)
					obj = CreateGameObject(orgObj);
				if (onProcess != null)
					onProcess(t, isDone, obj);
				return;
			}

			if (onProcess != null)
				onProcess(t, isDone, null);
		}, ResourceCacheType.rctTemp
		);

		return ret;
	}

	// if use addRefCount you must call UnLoadOrgObject
	public Texture LoadTexture(string fileName, ResourceCacheType cacheType)
	{
		Texture ret = mAssetLoader.LoadTexture (fileName, cacheType);
		if (ret != null)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadTexture (realFileName, cacheType);*/
	
		return mResLoader.LoadTexture(fileName, cacheType);
	}

	// if use addRefCount you must call UnLoadOrgObject
	public bool LoadTextureAsync(string fileName, Action<float, bool, Texture> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadTextureAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadTextureAsync (realFileName, cacheType, onProcess);*/
		return mResLoader.LoadTextureAsync (fileName, cacheType, onProcess);
	}

	// if use addRefCount you must call UnLoadOrgObject
	public Material LoadMaterial(string fileName, ResourceCacheType cacheType)
	{
		Material ret = mAssetLoader.LoadMaterial (fileName, cacheType);
		if (ret != null)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadMaterial (realFileName, cacheType);*/
		return mResLoader.LoadMaterial (fileName, cacheType);
	}

	// if use addRefCount you must call UnLoadOrgObject
	public bool LoadMaterialAsync(string fileName, Action<float, bool, Material> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadMaterialAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadMaterialAsync (realFileName, cacheType, onProcess);*/
		return mResLoader.LoadMaterialAsync (fileName, cacheType, onProcess);
	}

	// if use ResourceCacheType.rctTempAdd you must call UnLoadOrgObject
	private bool UnLoadOrgObject(UnityEngine.Object orgObj)
	{
		if (orgObj == null)
			return false;
		AssetCache cache = AssetCacheManager.Instance.FindOrgObjCache (orgObj);
		if (cache == null)
			return false;
		return AssetCacheManager.Instance.CacheDecRefCount (cache);
	}
	
	public AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType)
	{
		AudioClip ret = mAssetLoader.LoadAudioClip (fileName, cacheType);
		if (ret != null)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadAudioClip (realFileName, cacheType);*/
		return mResLoader.LoadAudioClip (fileName, cacheType);
	}

	public bool LoadAudioClipAsync(string fileName, Action<float, bool, AudioClip> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadAudioClipAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadAudioClipAsync (fileName, cacheType, onProcess);*/
		return mResLoader.LoadAudioClipAsync (fileName, cacheType, onProcess);
	}

	public byte[] LoadBytes(string fileName, ResourceCacheType cacheType = ResourceCacheType.rctNone)
	{
		byte[] ret = mAssetLoader.LoadBytes (fileName, cacheType);
		if (ret != null)
			return ret; 
		return mResLoader.LoadBytes(fileName, cacheType);
	}

	public string LoadText(string fileName, ResourceCacheType cacheType = ResourceCacheType.rctNone)
	{
		string ret = mAssetLoader.LoadText (fileName, cacheType);
		if (ret != null)
			return ret; 

		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadText (realFileName, cacheType);*/
		return mResLoader.LoadText (fileName, cacheType);
	}

	public bool LoadTextAsync(string fileName, Action<float, bool, TextAsset> onProcess, ResourceCacheType cacheType = ResourceCacheType.rctNone)
	{
		bool ret = mAssetLoader.LoadTextAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadTextAsync (realFileName, cacheType, onProcess);*/
		return mResLoader.LoadTextAsync (fileName, cacheType, onProcess);
	}

	public AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType)
	{
		AnimationClip ret = mAssetLoader.LoadAnimationClip (fileName, cacheType);
		if (ret != null)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadAnimationClip (realFileName, cacheType);*/
		return mResLoader.LoadAnimationClip (fileName, cacheType);
	}

	public bool LoadAnimationClipAsync(string fileName, Action<float, bool, AnimationClip> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadAnimationClipAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadAnimationClipAsync (realFileName, cacheType, onProcess);*/
		return mResLoader.LoadAnimationClipAsync(fileName, cacheType, onProcess);
	}

	public RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType)
	{
		RuntimeAnimatorController ret = mAssetLoader.LoadAniController (fileName, cacheType);
		if (ret != null)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadAniController (realFileName, cacheType);*/
		return mResLoader.LoadAniController (fileName, cacheType);
	}

	public bool LoadAniControllerAsync(string fileName, Action<float, bool, RuntimeAnimatorController> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadAniControllerAsync (fileName, cacheType, onProcess);
		if (ret)
			return ret;
		/*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadAniControllerAsync (realFileName, cacheType, onProcess);*/
		return mResLoader.LoadAniControllerAsync (fileName, cacheType, onProcess);
	}

	public Shader LoadShader(string fileName, ResourceCacheType cacheType)
	{
		Shader ret = mAssetLoader.LoadShader(fileName, cacheType);
		if (ret != null)
			return ret;
		return mResLoader.LoadShader(fileName, cacheType);
	}

	public bool LoadShaderAsync(string fileName, Action<float, bool, Shader> onProcess, ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadShaderAsync(fileName, cacheType, onProcess);
		if (ret)
			return ret;
		return mResLoader.LoadShaderAsync(fileName, cacheType, onProcess);
	}

	public bool PreLoadAndBuildAssetBundleShaders(string abFileName)
	{

		System.Type type = typeof(Shader);

		if (!PreLoadAssetBundle(abFileName, type))
			return false;

		Shader.WarmupAllShaders();
		return true;
	}

	public bool PreLoadAssetBundle(string abFileName, System.Type type)
	{
		if (mAssetLoader == null || type == null)
			return false;

		AssetLoader loader = mAssetLoader as AssetLoader;
		if (loader == null)
			return false;

		if (!loader.PreloadAllType(abFileName, type))
			return false;

		return true;
	}

#if UNITY_5

	public ShaderVariantCollection LoadShaderVarCollection(string fileName, ResourceCacheType cacheType)
	{
		ShaderVariantCollection ret = mAssetLoader.LoadShaderVarCollection(fileName, cacheType);
		if (ret != null)
			return ret;
		return mResLoader.LoadShaderVarCollection(fileName, cacheType);
	}

	public bool LoadShaderVarCollectionAsync(string fileName, Action<float, bool, ShaderVariantCollection> onProcess, 
	                                         ResourceCacheType cacheType)
	{
		bool ret = mAssetLoader.LoadShaderVarCollectionAsync(fileName, cacheType, onProcess);
		if (ret)
			return ret;
		return mResLoader.LoadShaderVarCollectionAsync(fileName, cacheType, onProcess);
	}

#endif

	public void UnloadUnUsed()
	{
		// 清除所有未使用�?		AssetCacheManager.Instance.ClearUnUsed ();
		Resources.UnloadUnusedAssets ();
	}

	public IResourceLoader AssetLoader
	{
		get {
			return mAssetLoader;
		}
	}

	public IResourceLoader ResLoader {
		get {
			return mResLoader;
		}
	}

	/*
	protected IResourceLoader GetLoader(ref string path)
	{
		if (string.IsNullOrEmpty (path))
			return null;

		if (path.StartsWith (cResourcesStartPath)) {
			path = path.Remove (0, cResourcesStartPath.Length);
			return mResLoader;
		} else {
			return mAssetLoader;
		}
	}

	protected string GetResLoaderFileName(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		if (fileName.StartsWith (cResourcesStartPath)) {
			return fileName.Remove (0, cResourcesStartPath.Length);
		}

		return fileName;
	}
	private static readonly string cResourcesStartPath = "Resources/";*/
		
	/*
	private void RegisterResLoaders()
	{
	}

	private delegate UnityEngine.Object OnResLoadEvent(string fileName, ResourceCacheType cacheType);
	private Dictionary<System.Type, OnResLoadEvent> m_ResLoaderMap = new Dictionary<Type, OnResLoadEvent>();
	*/

	private IResourceLoader mResLoader = new ResourcesLoader();
	private IResourceLoader mAssetLoader = new AssetLoader();
}