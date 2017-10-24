/*----------------------------------------------------------------
// 模块名：Resources加载类封装
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年6月1日
// 模块描述：
//         1、用于测试环境资源加载
//		   2、已支持异步加载回调
//----------------------------------------------------------------*/

#define USE_HAS_EXT
//#define USE_UNLOADASSET

using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class ResourceAssetCache: AssetCache
{
#if UNITY_EDITOR
	// 用于在编辑器中标识用
	public string Tag
	{
		get
		{
			CheckTag();
			return mTag;
		}
	}
	
	void CheckTag()
	{
		if (mTargetType == null || mTarget == null)
			return;
		if (string.IsNullOrEmpty(mTag))
		{
			if (mTargetType == typeof(GameObject))
				mTag = "obj";
			else
				if (mTargetType == typeof(AudioClip))
					mTag = "audio";
			else
				if (mTargetType == typeof(Texture))
					mTag = "tex";
			else
				if (mTargetType == typeof(Shader))
					mTag = "shader";
			else
				if (mTargetType == typeof(Material))
					mTag = "mat";
			else
				if (mTargetType == typeof(RuntimeAnimatorController))
					mTag = "AniController";
			else
				mTag = "[UnKnown]";						
			
			if (!string.IsNullOrEmpty(mTag))
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(mTarget);
				path = System.IO.Path.GetDirectoryName(path);
				mTag += "(" + path + ")";
			}
			
		}
	}
	
	private string mTag = string.Empty;
#endif
	public ResourceAssetCache(UnityEngine.Object target, string fileName, System.Type targetType)
	{
		mTarget = target;
        mTargetType = targetType;
        mFileName = fileName;
		CheckGameObject();
	}

	public ResourceAssetCache()
	{}

	public static ResourceAssetCache Create(UnityEngine.Object target, string fileName, System.Type targetType)
	{
		ResourceAssetCache ret;
		if (m_PoolUsed)
			ret = GetPool(target, fileName, targetType);
		else
			ret = new ResourceAssetCache(target, fileName, targetType);
		return ret;
	}

	public UnityEngine.Object Target {
		get 
		{
			return mTarget;
		}
	}

	public System.Type TargetType
	{
		get
		{
			return mTargetType;
		}
	}

	public string FileName
	{
		get
		{
			return mFileName;
		}
	}

	public static int GetPoolCount()
	{
		return m_Pool.Count;
	}

	private void CheckGameObject()
	{
		mIsGameObject = (mTargetType == typeof(GameObject));
	}

    protected override void OnUnUsed()
    {
		ResourcesLoader loader = ResourceMgr.Instance.ResLoader as ResourcesLoader;
		if (loader != null)
			loader.OnCacheDestroy(this);
		
        mTarget = null;
		mTargetType = null;
		
		mFileName = string.Empty;
		if (m_PoolUsed) {
			InPool (this);
		} else {
			ClearLinkListNode ();
		}
    }

    protected override void OnUnloadAsset(UnityEngine.Object asset)
    {
        if (!mIsGameObject)
        {
            bool isTarget = asset == mTarget;

            AssetCacheManager.Instance._RemoveOrgObj(asset, true);

            if (isTarget)
            {
                // 直接立即清理
                AssetCacheManager.Instance._Unload(this);
            }
            else if (IsNotUsed())
            {
                AssetCacheManager.Instance._RemoveOrgObj(mTarget, true);
                AssetCacheManager.Instance._Unload(this);
            }
        }
    }

    protected override void OnUnLoad()
    {
        ResourcesLoader loader = ResourceMgr.Instance.ResLoader as ResourcesLoader;
        if (loader != null)
            loader.OnCacheDestroy(this);

        if (mIsGameObject)
        {
            /*
			 * 使用GameObject.DestroyImmediate(mTarget, true) 会导致UnityEditor中 文件的操作无法使用,以及第二次使用Resources.Load失败
			 * GameObject.DestroyObject 和 GameObject.Destroy， 使用GameObject.DestroyImmediate(mTarget, false)会提示使用 DestroyImmediate(mTarget, true)
			 * 使用Resources.UnloadAsset 会提示错误：只能释放不可见的资源，不能是GameObject
			 */

            // GameObject.DestroyImmediate(mTarget, true);
            // GameObject.DestroyObject(mTarget);

            if (Application.isEditor)
                LogMgr.Instance.LogWarning("ResourceAssetCache OnUnLoad: GameObject is not UnLoad in EditorMode!");
            //	else
            //		Resources.UnloadAsset(mTarget);
            // GameObject.DestroyImmediate(mTarget, true);
#if USE_UNLOADASSET
            // 必须不能在Editor中跑
            if (!Application.isEditor)
                GameObject.DestroyImmediate(mTarget);
#endif
        }
        else
        {
            // texture, material etc.
            if (mTarget != null)
            {
#if USE_UNLOADASSET
				Resources.UnloadAsset (mTarget);
#endif
            }
            //if (Application.isEditor)
            //	LogMgr.Instance.LogWarning("ResourceAssetCache OnUnLoad: GameObject is not UnLoad in EditorMode!");
            //GameObject.DestroyImmediate(mTarget, true);
        }

        mTarget = null;
        mTargetType = null;

        mFileName = string.Empty;

		if (m_PoolUsed) {
			InPool (this);
		} else
			ClearLinkListNode ();
    }

    private static void InitPool() {
		if (!m_PoolInited) {
			m_Pool.Init(0);
			m_PoolInited = true;
		}
	}
	
	private static void InPool(ResourceAssetCache cache)
	{
		if (cache == null)
			return;
		InitPool();
		m_Pool.Store(cache);
	}

	private static ResourceAssetCache GetPool(UnityEngine.Object target, string fileName, System.Type targetType)
	{
		InitPool();
		ResourceAssetCache ret = m_Pool.GetObject();
		ret.mTarget = target;
		ret.mTargetType = targetType;
		ret.mFileName = fileName;
		ret.CheckGameObject();
		return ret;
	}

	private UnityEngine.Object mTarget = null;
	private Type mTargetType = null;
	private bool mIsGameObject = false;
	private string mFileName = string.Empty;

	// 缓冲池
	private static bool m_PoolUsed = true;
	private static bool m_PoolInited = false;
	private static Utils.ObjectPool<ResourceAssetCache> m_Pool = new Utils.ObjectPool<ResourceAssetCache>();
}

public class ResourcesLoader: IResourceLoader
{
	private static readonly string cResourcesStartPath = "resources/";
	
	protected bool IsResLoaderFileName(ref string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return false;
		int startIdx = fileName.IndexOf (cResourcesStartPath, StringComparison.CurrentCultureIgnoreCase);
		if (startIdx >= 0) {
			startIdx +=  cResourcesStartPath.Length;
#if USE_HAS_EXT
			int idx = fileName.LastIndexOf('.');
			if (idx > 0)
			{
				fileName = fileName.Substring(startIdx, idx - startIdx);
			} else if (idx == 0)
				fileName = string.Empty;
#else
			fileName = fileName.Remove (0, startIdx);
#endif
			return true;
		}
		
		return false;
	}

	private void AddRefSprites(ResourceAssetCache cache, Sprite[] sprites, ResourceCacheType cacheType) {
		if (cache == null || sprites == null || sprites.Length <= 0)
			return;

		if (cacheType != ResourceCacheType.rctNone) {
			if (cacheType == ResourceCacheType.rctRefAdd)
				AssetCacheManager.Instance._AddOrUpdateUsedList(cache, sprites.Length);

			for (int i = 0; i < sprites.Length; ++i) {
				AssetCacheManager.Instance._OnLoadObject(sprites[i], cache);
			}
		}
	}

	private AssetCache AddRefCache(string orgFileName, UnityEngine.Object obj, ResourceCacheType cacheType, System.Type objType)
	{
		AssetCache cache = null;
		if (obj && (cacheType != ResourceCacheType.rctNone)) {
			cache = AssetCacheManager.Instance.FindOrgObjCache (obj);
			if (cache == null)
				cache = ResourceMgr.Instance.ResLoader.CreateCache (obj, orgFileName, objType);
			if (cache != null) {
				if (cacheType == ResourceCacheType.rctRefAdd)
					AssetCacheManager.Instance._AddOrUpdateUsedList (cache);
				AssetCacheManager.Instance._OnLoadObject (obj, cache);
			}
		} else if (obj && (cacheType == ResourceCacheType.rctNone)) {
			cache = AssetCacheManager.Instance.FindOrgObjCache (obj);
			if (cache == null)
			{
				// 第一次加载
				cache = ResourceMgr.Instance.ResLoader.CreateCache (obj, orgFileName, objType);
				if (cache != null)
				{
					AssetCacheManager.Instance._AddTempAsset(cache);
				}
			}
		}

		return cache;
		
	}

#region public function

	public T LoadObject<T>(string fileName, ResourceCacheType cacheType) where T: UnityEngine.Object
	{
		if (string.IsNullOrEmpty (fileName))
			return null;

		T ret = null;

		string orgFileName = fileName;

#if USE_HAS_EXT
		ret = FindCache<T>(orgFileName);
#endif
		bool isFirstLoad = (ret == null);
		if (isFirstLoad)
		{	
			if (IsResLoaderFileName (ref fileName))
				ret = Resources.Load<T>(fileName);
			else {
				return null;
			}
		}

		AssetCache cache = AddRefCache(orgFileName, ret, cacheType, typeof(T));

#if USE_HAS_EXT
		if (isFirstLoad && cache != null)
		{
			AddCacheMap(cache);
		}
#endif

		return ret;
	}

	public bool LoadObjectAsync<T>(string fileName, ResourceCacheType cacheType, int priority, Action<float, bool, T> onProcess) where T: UnityEngine.Object
	{
		if (string.IsNullOrEmpty (fileName))
			return false;

		string orgFileName = fileName;
#if USE_HAS_EXT
		T obj = FindCache<T>(orgFileName);
		if (obj != null)
		{
			if (AddRefCache(orgFileName, obj, cacheType, typeof(T)) != null)
			{
				if (onProcess != null)
					onProcess(1.0f, true, obj);
				return true;
			}
		}
#endif

		if (!IsResLoaderFileName (ref fileName)) {
			return false;
		}

		// 同时反复调用可能产生多个Timer
		ResourceRequest request = Resources.LoadAsync (fileName, typeof(T));
		if (request == null)
			return false;
		if (request.isDone) {
			T orgObj = request.asset as T;
			if (orgObj == null)
			{
				string err = string.Format("LoadObjectAsync: ({0}) error!", fileName);
				LogMgr.Instance.LogError(err);
				return false;
			}

			AssetCache cache = AddRefCache(orgFileName, orgObj, cacheType, typeof(T));

#if USE_HAS_EXT
			AddCacheMap(cache);
#endif

			if (onProcess != null)
				onProcess(request.progress, request.isDone, orgObj);
			return true;
		}

		request.priority = priority;

		var ret = AsyncOperationMgr.Instance.AddAsyncOperation<ResourceRequest, System.Object> (request,
		                                              delegate (ResourceRequest req) {
			if (req.isDone)
			{
				T orgObj = req.asset as T;
				if (orgObj == null)
				{
					string err = string.Format("LoadObjectAsync: ({0}) error!", fileName);
					LogMgr.Instance.LogError(err);
					return;
				}

				AssetCache cache = AddRefCache(orgFileName, orgObj, cacheType, typeof(T));
#if USE_HAS_EXT
				AddCacheMap(cache);
#endif

				if (onProcess != null)
					onProcess(req.progress, req.isDone, orgObj);
			} else
			{
				if (onProcess != null)
					onProcess(req.progress, req.isDone, null);
			}
			
		}
		);
		
		return ret != null;
	}

	public override Shader LoadShader(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Shader>(fileName, cacheType);
	}

	public override bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Shader> (fileName, cacheType, priority, onProcess);
	}

	public override GameObject LoadPrefab(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<GameObject> (fileName, cacheType);
	}

	public override bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess, int priority = 0)
	{
		return LoadObjectAsync<GameObject> (fileName, cacheType, priority, onProcess);
	}

	public override AudioClip LoadAudioClip(string fileName, ResourceCacheType cache)
	{
		return LoadObject<AudioClip> (fileName, cache);
	}

	public override bool LoadAudioClipAsync(string fileName, ResourceCacheType cache, Action<float, bool, AudioClip> onProcess, int priority = 0)
	{
		return LoadObjectAsync<AudioClip> (fileName, cache, priority, onProcess);
	}

	public override string LoadText(string fileName, ResourceCacheType cache)
	{
		TextAsset text = LoadObject<TextAsset>(fileName, cache);
		if (text == null)
			return null;
        return text.text;
	}

	public override byte[] LoadBytes(string fileName, ResourceCacheType cache)
	{
		TextAsset text = LoadObject<TextAsset>(fileName, cache);
		if (text == null)
			return null;
		return text.bytes;
	}

	public override bool LoadTextAsync (string fileName, ResourceCacheType cache, Action<float, bool, TextAsset> onProcess, int priority = 0)
	{
		return LoadObjectAsync<TextAsset> (fileName, cache, priority, onProcess);
	}

	// not used addToCache
	public override Material LoadMaterial(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Material> (fileName, cacheType);
	}

	// not used addToCache
	public override bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Material> (fileName, cacheType, priority, onProcess);
	}

	// not used addToCache
	public override Texture LoadTexture(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Texture> (fileName, cacheType);
	}

	// not used addToCache
	public override bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Texture> (fileName, cacheType, priority, onProcess);
	}

	public override Font LoadFont (string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Font> (fileName, cacheType);
	}

	public override bool LoadFontAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, Font> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Font> (fileName, cacheType, priority, onProcess);
	}

	public override RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<RuntimeAnimatorController> (fileName, cacheType);
	}

	public override bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess, int priority = 0)
	{
		return LoadObjectAsync<RuntimeAnimatorController> (fileName, cacheType, priority, onProcess);
	}

	public override AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<AnimationClip> (fileName, cacheType);
	}

	public override bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess, int priority = 0)
	{
		return LoadObjectAsync<AnimationClip> (fileName, cacheType, priority, onProcess);
	}

	public override ScriptableObject LoadScriptableObject (string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<ScriptableObject> (fileName, cacheType);
	}

	public override bool LoadScriptableObjectAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.ScriptableObject> onProcess, int priority = 0)
	{
		return LoadObjectAsync<ScriptableObject> (fileName, cacheType, priority, onProcess);
	}

		public override Sprite[] LoadSprites(string fileName) {
			if (string.IsNullOrEmpty(fileName))
				return null;

			Texture tex = LoadObject<Texture>(fileName, ResourceCacheType.rctTemp);
			if (tex == null)
				return null;
			AssetCache cache = AssetCacheManager.Instance.FindOrgObjCache(tex);
			if (cache == null)
				return null;

            ResourceAssetCache resCache = cache as ResourceAssetCache;
            if (resCache == null)
				return null;

			if (!IsResLoaderFileName(ref fileName))
				return null;

            Sprite[] ret = Resources.LoadAll<Sprite>(fileName);

            if (ret == null || ret.Length <= 0)
				return null;

			AddRefSprites(resCache, ret, ResourceCacheType.rctRefAdd);

			return ret;
		}

	public override bool LoadSpritesAsync(string fileName, Action<float, bool, UnityEngine.Object[]> onProcess, int priority = 0) {
		return LoadObjectAsync<Texture>(fileName, ResourceCacheType.rctRefAdd, priority,
				delegate(float process, bool isDone, Texture obj) {
					if (isDone) {
						if (obj == null) {
							if (onProcess != null)
								onProcess(process, isDone, null);
							return;
						}

						AssetCache cache = AssetCacheManager.Instance.FindOrgObjCache(obj);
						if (cache == null) {
							if (onProcess != null)
								onProcess(process, isDone, null);
							return;
						}

						ResourceMgr.Instance.DestroyObject(obj);

                        ResourceAssetCache resCache = cache as ResourceAssetCache;
                        if (resCache == null)
                        {
                            if (onProcess != null)
                                onProcess(process, isDone, null);
                            return;
						}

                        if (!IsResLoaderFileName(ref fileName))
                        {
                            if (onProcess != null)
                                onProcess(process, isDone, null);
                            return;
						}
                        Sprite[] ret = Resources.LoadAll<Sprite>(fileName);

                        if (ret == null || ret.Length <= 0) {
							if (onProcess != null)
								onProcess(process, isDone, null);
							return;
						}

						AddRefSprites(resCache, ret, ResourceCacheType.rctRefAdd);

						if (onProcess != null)
							onProcess(process, isDone, ret);

						return;
					}

					if (onProcess != null)
						onProcess(process, isDone, null);
				}
			);
		}

#if UNITY_5
	public override ShaderVariantCollection LoadShaderVarCollection(string fileName, 
	                                                                ResourceCacheType cacheType)
	{
		return LoadObject<ShaderVariantCollection> (fileName, cacheType);
	}
	
	public override bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType cacheType, 
		Action<float, bool, ShaderVariantCollection> onProcess, int priority = 0)
	{
		return LoadObjectAsync<ShaderVariantCollection> (fileName, cacheType, priority, onProcess);
	}
#endif

	public override bool OnSceneLoad(string sceneName)
	{
		// 不建议将Scene放到Resources目录下，不好做对资源引用计数管理
		// LogMgr.Instance.LogWarning ("Don't load scene From Resources: because AssetCache maybe error!");
		return true;
	}

	public override bool OnSceneLoadAsync(string sceneName, Action onEnd, int priority = 0)
	{
		if (onEnd != null)
			onEnd();
		return true;
	}

	public override bool OnSceneClose(string sceneName)
	{
		if (string.IsNullOrEmpty (sceneName))
			return false;
		return true;
	}

	public override AssetCache CreateCache(UnityEngine.Object orgObj, string fileName, System.Type orgType)
	{
		if (orgObj == null)
			return null;

		ResourceAssetCache cache = ResourceAssetCache.Create(orgObj, fileName, orgType);
		return cache;
	}

#endregion public function

	private AssetCache FindCache(string fileName, System.Type resType)
	{
		AssetCache ret;
		CacheKey key = CreateCacheKey(fileName, resType);
		if (m_CacheMap.TryGetValue(key, out ret))
			return ret;
		return null;
	}

	private T FindCache<T>(string fileName) where T: UnityEngine.Object
	{
		AssetCache cache = FindCache(fileName, typeof(T));
		if (cache == null)
			return null;

		ResourceAssetCache resCache = cache as ResourceAssetCache;
		if (resCache == null)
			return null;

		return resCache.Target as T;
	}

	internal void OnCacheDestroy(ResourceAssetCache cache)
	{
		if (cache == null)
			return;

		if (cache.TargetType == null)
			return;
		
		string fileName = cache.FileName;
		if (string.IsNullOrEmpty(fileName))
			return;

		System.Type resType = cache.TargetType;
		CacheKey key = CreateCacheKey(fileName, resType);
		if (m_CacheMap.ContainsKey(key))
			m_CacheMap.Remove(key);
	}

	private void AddCacheMap(AssetCache cache)
	{
		if (cache == null)
			return;
		ResourceAssetCache resCache = cache as ResourceAssetCache;
		if (resCache == null)
			return;
		AddCacheMap(resCache);
	}

	private void AddCacheMap(ResourceAssetCache cache)
	{
		if (cache == null || cache.Target == null || cache.TargetType == null)
			return;

		string fileName = cache.FileName;
		if (string.IsNullOrEmpty(fileName))
			return;
		
		System.Type resType = cache.TargetType;
		CacheKey key = CreateCacheKey(fileName, resType);

		if (m_CacheMap.ContainsKey(key))
		{
			if (m_CacheMap[key] != cache)
			{
				//AssetCache oldCache = m_CacheMap[key];
				m_CacheMap[key] = cache;
				Debug.LogErrorFormat("[AddCacheMap] CacheMap {0} exists!", fileName);
			}

			return;
		}

		m_CacheMap.Add(key, cache);
	}

	private sealed class CacheKeyComparser: StructComparser<CacheKey>
	{}

	private struct CacheKey: IEquatable<CacheKey>
	{
		public string fileName;
		public System.Type resType;

		public bool Equals(CacheKey other) {
			return this == other;
		}

		public override bool Equals(object obj) {
			if (obj == null)
				return false;

			if (GetType() != obj.GetType())
				return false;

			if (obj is CacheKey) {
				CacheKey other = (CacheKey)obj;
				return Equals(other);
			}
			else
				return false;

		}

		public override int GetHashCode() {
			int ret = FilePathMgr.InitHashValue();
			FilePathMgr.HashCode(ref ret, fileName);
			FilePathMgr.HashCode(ref ret, resType);
			return ret;
		}

		public static bool operator ==(CacheKey a, CacheKey b) {
			return (a.resType == b.resType) && (string.Compare(a.fileName, b.fileName) == 0);
		}

		public static bool operator !=(CacheKey a, CacheKey b) {
			return !(a == b);
		}
	}

	private CacheKey CreateCacheKey(string fileName, System.Type resType)
	{
		CacheKey ret = new CacheKey();
		ret.fileName = fileName;
		ret.resType = resType;
		return ret;
	}

    public void AutoUpdateClear()
    {
        m_CacheMap.Clear ();
    }

	private Dictionary<CacheKey, AssetCache> m_CacheMap = new Dictionary<CacheKey, AssetCache>(CacheKeyComparser.Default);
}