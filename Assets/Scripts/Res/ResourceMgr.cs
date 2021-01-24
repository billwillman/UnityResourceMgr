﻿using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2018 || UNITY_2019 || UNITY_2017
using UnityEngine.SceneManagement;
#endif

public class ResourceMgr : Singleton<ResourceMgr>
{

    // isThreadMode: 是否是真多线程异步加载 async: 采用协程。在多线程和协程同时传入，优先使用多线程
    public void LoadConfigs(Action<bool> OnFinish, MonoBehaviour async = null, bool isThreadMode = false)
    {
        AssetLoader loader = mAssetLoader as AssetLoader;
        if (loader != null)
        {
            loader.LoadConfigs(OnFinish, async, isThreadMode);
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

        if (mAssetLoader.OnSceneLoad(sceneName))
        {
            DebugSceneLoadInfo();
            LogMgr.Instance.Log(StringHelper.Format("Loading AssetBundle Scene: {0}", sceneName));
        }
        else
        {
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                return false;
#endif
            if (mResLoader.OnSceneLoad(sceneName))
                LogMgr.Instance.Log(StringHelper.Format("Loading Resources Scene: {0}", sceneName));
            else
                return false;
        }

        if (isAdd)
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2018 || UNITY_2019 || UNITY_2017
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
#else
			Application.LoadLevelAdditive (sceneName);
#endif
        else
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2018 || UNITY_2019 || UNITY_2017
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
#else
			Application.LoadLevel (sceneName);
#endif

        return true;
    }

    private bool DoLoadSceneAsync(string sceneName, bool isAdd, Action<AsyncOperation, bool> onProcess, bool isLoadedActive, int priority)
    {
        AsyncOperation opt;
        if (isAdd)
        {
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2018 || UNITY_2019 || UNITY_2017
            opt = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
#else
			opt = Application.LoadLevelAdditiveAsync (sceneName);
#endif
        }
        else
        {
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2018 || UNITY_2019 || UNITY_2017
            opt = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
#else
			opt = Application.LoadLevelAsync(sceneName);
#endif
        }

        if (opt == null)
            return false;

        if (opt.isDone)
        {
            if (onProcess != null)
                onProcess(opt, true);
            return true;
        }

        opt.allowSceneActivation = isLoadedActive;
        opt.priority = priority;
        if (isLoadedActive)
            return AsyncOperationMgr.Instance.AddAsyncOperation<AsyncOperation, System.Object>(opt, onProcess) != null;
        else
        {
            //return AsyncOperationMgr.Instance.AddAsyncOperation<AsyncOperation, System.Object>(opt, onProcess) != null;

            return AsyncOperationMgr.Instance.AddAsyncOperation<AsyncOperation, System.Object>(opt,
                delegate (AsyncOperation obj, bool isDone)
                {
                    if (onProcess != null)
                        onProcess(obj, isDone);
                    if (obj.progress >= 0.9f)
                    {
                        AsyncOperationMgr.Instance.RemoveAsyncOperation(obj);
                    }
                }) != null;
        }
    }

    private static void DebugSceneLoadInfo()
    {
#if UNITY_EDITOR
        Debug.LogError("【场景加载】注意AB读取模式，SceneName必须大小写一致，但如果未开启USE_LOWERCHAR编译指令，而场景名含有大写，会导致出现：\n" +
            "【Unity AssetBundle Scene couldn't be loaded...】的提示，是因为LoadScene必须与场景大小写一致。两种修改方法:\n" +
            "1.底层开启USE_LOWERCHAR，传入大小写对应的SceneName.\n" +
            "2.场景名直接全部小写模式，不开启编译指令（推荐，减少GC）");
#endif
    }

    // isLoadedActive: 是否加载完就激活
    // bool isDone，是否已经完成
    public bool LoadSceneAsync(string sceneName, bool isAdd, Action<AsyncOperation, bool> onProcess, bool isLoadedActive = true, int priority = 0)
    {
        if (mAssetLoader.OnSceneLoadAsync(sceneName,
                                           delegate
                                           {
                                               DebugSceneLoadInfo();
                                               DoLoadSceneAsync(sceneName, isAdd, onProcess, isLoadedActive, priority);
                                           }, priority))
        {
            LogMgr.Instance.Log(StringHelper.Format("Loading AssetBundle Scene: {0}", sceneName));
        }
        else
        {
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                return false;
#endif
            if (mResLoader.OnSceneLoadAsync(sceneName,
                                            delegate
                                            {
                                                DoLoadSceneAsync(sceneName, isAdd, onProcess, isLoadedActive, priority);
                                            }, priority))
                LogMgr.Instance.Log(StringHelper.Format("Loading Resources Scene: {0}", sceneName));
            else
                return false;
        }

        //return DoLoadSceneAsync(sceneName, isAdd, onProcess);
        return true;
    }

    public void CloseScene(string sceneName)
    {
        /*
		string realSceneName = sceneName;
		IResourceLoader loader = GetLoader (ref realSceneName);
		if (loader == null)
			return;
		loader.OnSceneClose (realSceneName);*/
        if (!mAssetLoader.OnSceneClose(sceneName))
            mResLoader.OnSceneClose(sceneName);

#if UNITY_5_2
		Application.UnloadLevel(sceneName);
#elif UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2018 || UNITY_2019 || UNITY_2017
        SceneManager.UnloadScene(sceneName);
#endif
        // 娓呴櫎
        AssetCacheManager.Instance.ClearUnUsed();
    }

    public GameObject CreateGameObject(string fileName)
    {
        GameObject ret = CreatePrefab(fileName);
        if (ret != null)
        {
            var script = ret.AddComponent<ResInstDestroy>();
            script.CheckVisible();
        }

        return ret;
    }

    public GameObject CreateGameObject(string fileName, Vector3 position, Quaternion rotation)
    {
        GameObject ret = CreatePrefab(fileName);
        if (ret != null)
        {
            Transform trans = ret.transform;
            trans.position = position;
            trans.rotation = rotation;
            var script = ret.AddComponent<ResInstDestroy>();
            script.CheckVisible();
        }

        return ret;
    }

    public GameObject CreateGameObject(string fileName, Vector3 position, Quaternion rotation, float delayDestroyTime)
    {
        GameObject ret = CreatePrefab(fileName);
        if (ret != null)
        {
            Transform trans = ret.transform;
            trans.position = position;
            trans.rotation = rotation;
            ResInstDelayDestroy script = ret.AddComponent<ResInstDelayDestroy>();
            script.DelayDestroyTime = delayDestroyTime;
            script.CheckVisible();
        }

        return ret;
    }

    public GameObject InstantiateGameObj(GameObject orgObj)
    {
        if (orgObj == null)
            return null;
        GameObject ret = GameObject.Instantiate(orgObj);
        if (ret != null)
        {
            // 优化GC,只有在必要的地方才加脚本
            if (AssetCacheManager.Instance.FindOrgObjCache(orgObj) != null)
            {
                AssetCacheManager.Instance._OnCreateGameObject(ret, orgObj);
                var script = ret.AddComponent<ResInstDestroy>();
                script.CheckVisible();
            }
        }
        return ret;
    }

    public GameObject InstantiateGameObj(GameObject orgObj, Vector3 position, Quaternion rotation)
    {
        GameObject ret = InstantiateGameObj(orgObj);
        if (ret == null)
            return null;
        Transform trans = ret.transform;
        trans.position = position;
        trans.rotation = rotation;
        return ret;
    }

    public GameObject InstantiateGameObj(GameObject orgObj, float delayDestroyTime)
    {
        if (orgObj == null)
            return null;
        GameObject ret = GameObject.Instantiate(orgObj);
        if (ret != null)
        {
            // 优化GC,只有在必要的地方才加脚本
            if (AssetCacheManager.Instance.FindOrgObjCache(orgObj) != null)
            {
                AssetCacheManager.Instance._OnCreateGameObject(ret, orgObj);
                ResInstDelayDestroy script = ret.AddComponent<ResInstDelayDestroy>();
                script.DelayDestroyTime = delayDestroyTime;
                script.CheckVisible();
            }
        }
        return ret;
    }

    public GameObject InstantiateGameObj(GameObject orgObj, Vector3 position, Quaternion rotation, float delayDestroyTime)
    {
        GameObject ret = InstantiateGameObj(orgObj, delayDestroyTime);
        if (ret == null)
            return null;
        Transform trans = ret.transform;
        trans.position = position;
        trans.rotation = rotation;
        return ret;
    }

    public GameObject CreateGameObject(string fileName, float delayDestroyTime)
    {
        GameObject ret = CreatePrefab(fileName);
        if (ret != null)
        {
            ResInstDelayDestroy script = ret.AddComponent<ResInstDelayDestroy>();
            script.DelayDestroyTime = delayDestroyTime;
            script.CheckVisible();
        }

        return ret;
    }

    private GameObject CreateGameObject(GameObject orgObject)
    {
        if (orgObject == null)
            return null;
        GameObject ret = GameObject.Instantiate(orgObject) as GameObject;
        if (ret != null)
        {
            AssetCacheManager.Instance._OnCreateGameObject(ret, orgObject);
            // 鍔犲叆涓?涓?剼鏈?			// ret.AddComponent<ResInstDestroy>();
        }

        return ret;
    }

    public void OnDestroyInstObject(UnityEngine.GameObject instObj)
    {
        if (instObj == null)
            return;
        OnDestroyInstObject(instObj.GetInstanceID());
    }

    public void OnDestroyInstObject(int instID)
    {
        AssetCacheManager.Instance._OnDestroyGameObject(instID);
    }

    // 删除实例化的GameObj   isImm: 是否是立即模式
    public void DestroyInstGameObj(UnityEngine.GameObject obj, bool isImm = false)
    {
        if (obj == null)
            return;
        int instId = obj.GetInstanceID();
        if (isImm)
            UnityEngine.GameObject.DestroyImmediate(obj);
        else
            UnityEngine.GameObject.Destroy(obj);

        AssetCacheManager.Instance._OnDestroyGameObject(instId);
    }

    // AudioClip Unload False的坑
    public void CoroutneAudioClipABUnloadFalse(AudioClip clip, MonoBehaviour parent)
    {
        if (clip == null || parent == null)
            return;
        parent.StartCoroutine(CoroutneAudioClipABUnloadFalse(clip));
    }

    private System.Collections.IEnumerator CoroutneAudioClipABUnloadFalse(AudioClip clip)
    {
        while (true)
        {
            if (clip == null)
            {
                yield break;
            }

            if (clip.loadState == AudioDataLoadState.Loading || clip.loadState == AudioDataLoadState.Unloaded)
                yield return null;
            else
            {
                if (!clip.UnloadAudioData())
                    yield return null;
                ABUnloadFalse(clip, true);
                break;
            }
        }
    }

    // NGUI在同步加载的时候使用UISprite, AB Unload False会出现问题(主要是因为UIATLAS序列化问题, UIATLAS里会导致有部分丢失)
    public void CoroutineEndFrameABUnloadFalse(UnityEngine.Object obj, MonoBehaviour parent, bool unMySelf = false)
    {
        if (obj == null || parent == null)
            return;
        parent.StartCoroutine(CoroutineEndFrameABUnloadFalse(obj, unMySelf));
    }

    private WaitForEndOfFrame m_EndFrame = null;

    private System.Collections.IEnumerator CoroutineEndFrameABUnloadFalse(UnityEngine.Object obj, bool unMySelf)
    {
        if (obj == null)
            yield break;
        if (m_EndFrame == null)
            m_EndFrame = new WaitForEndOfFrame();
        yield return m_EndFrame;
        ABUnloadFalse(obj, unMySelf);
    }

    public void ABUnloadTrue(UnityEngine.Object target)
    {
        if (target == null)
            return;
        AssetCache cache = AssetCacheManager.Instance.FindOrgObjCache(target);
        if (cache == null)
        {
            GameObject gameObj = target as GameObject;
            if (gameObj != null)
            {
                cache = AssetCacheManager.Instance.FindInstGameObjectCache(gameObj);
                if (cache == null)
                    return;
            }
            else
                return;
        }

        AssetCacheManager.Instance._Unload(cache, true);
    }

    public void ABUnloadFalse(UnityEngine.Object[] targets, bool unMySelf = true)
    {
        if (targets == null || targets.Length <= 0)
            return;
        for (int i = 0; i < targets.Length; ++i)
        {
            ABUnloadFalse(targets[i], unMySelf);
        }
    }

    // AssetBundle.Unload(false)
    // 使用这个函数，如果是非实例化的资源，不需要调用DestroyObject释放它
    // 例如：LoadPrefab，refAdd,然后调用ABUnloadFalse，并且unMySelf参数是True的时候，不需要俚饔肦esourceMgr.Instance.Destroy释放它
    // 总结来说：没有ABUnloadFalse的refAdd资源，都要用ResourceMgr.Instance.Destroy对引用计数-1
    public bool ABUnloadFalse(UnityEngine.Object target, bool unMySelf = true)
    {

        if (target == null)
            return false;

        AssetCache cache = AssetCacheManager.Instance.FindOrgObjCache(target);

        bool isOrgRes = cache != null;
        if (cache == null)
        {
            GameObject gameObj = target as GameObject;
            if (gameObj != null)
            {
                cache = AssetCacheManager.Instance.FindInstGameObjectCache(gameObj);
                if (cache == null)
                    return false;
            }
            else
                return false;
        }

        if (unMySelf)
        {
            bool ret = (!isOrgRes) || AssetCacheManager.Instance.CacheDecRefCount(cache);
            if (ret)
            {
                if (cache.IsNotUsed())
                    ABUnloadFalse(cache);
                else
                    ABUnloadFalseDepend(cache);
            }
        }
        else
        {
            ABUnloadFalseDepend(cache);
        }

        return true;
    }

    private void ABUnloadFalseDepend(AssetCache cache)
    {
        // ----------------------------针对AB处理----------------------
        if (cache != null)
        {
            AssetInfo target = null;
            AssetBundleCache abCache = cache as AssetBundleCache;
            if (abCache != null)
            {
                if (!abCache.IsloadDecDepend)
                    return;
                target = abCache.Target;
                if (target != null)
                {
                    if (target.IsUsing)
                        return;
                }
            }
            if (target != null)
            {
                AssetLoader loader = mAssetLoader as AssetLoader;
                if (loader != null)
                {

                    // 先释放自己
                    target._BundleUnLoadFalse();

                    // 再释放依赖
                    if (abCache.IsloadDecDepend)
                    {
                        abCache.IsloadDecDepend = false;
                        for (int i = 0; i < target.DependFileCount; ++i)
                        {
                            string depFileName = target.GetDependFileName(i);
                            if (string.IsNullOrEmpty(depFileName))
                                continue;
                            int refCnt = target.GetDependFileRef(i);
                            AssetInfo depInfo = loader.FindAssetInfo(depFileName);
                            if (depInfo != null && depInfo.Cache != null)
                            {
                                bool ret = AssetCacheManager.Instance.CacheDecRefCount(depInfo.Cache, refCnt);
                                if (ret)
                                    ABUnloadFalse(depInfo.Cache);
                            }
                        }
                    }


                }

            }
        }
        //-------------------------------------------------------------
    }

    private void ABUnloadFalse(AssetCache cache)
    {
        if (cache != null && cache.IsNotUsed())
        {
            AssetInfo target = null;
            AssetBundleCache abCache = cache as AssetBundleCache;
            if (abCache != null)
                target = abCache.Target;
            AssetCacheManager.Instance._Unload(cache, false);
            // ----------------------------针对AB处理----------------------
            if (abCache != null && !abCache.IsloadDecDepend)
                return;
            if (target != null)
            {
                AssetLoader loader = mAssetLoader as AssetLoader;
                if (loader != null)
                {

                    // 先对自己处理
                    if (abCache.IsloadDecDepend)
                    {
                        abCache.IsloadDecDepend = false;
                        // 再对依赖处理
                        for (int i = 0; i < target.DependFileCount; ++i)
                        {
                            string depFileName = target.GetDependFileName(i);
                            if (string.IsNullOrEmpty(depFileName))
                                continue;
                            AssetInfo depInfo = loader.FindAssetInfo(depFileName);
                            if (depInfo != null && depInfo.Cache != null)
                            {
                                ABUnloadFalse(depInfo.Cache);
                            }
                        }
                    }
                }
            }
            //-------------------------------------------------------------
        }
    }

    // isUnloadAsset is used by not GameObject
    public void DestroyObject(UnityEngine.Object obj, bool isUnloadAsset = false)
    {
        if (obj == null)
            return;

        if (obj is Transform)
            obj = (obj as Transform).gameObject;

        UnityEngine.Component comp = obj as UnityEngine.Component;
        if (comp != null)
        {

            if (Application.isPlaying)
                UnityEngine.GameObject.Destroy(obj);
            else
                UnityEngine.GameObject.DestroyImmediate(obj);
            //UnityEngine.GameObject.DestroyImmediate(obj);
            return;
        }

        if (!UnLoadOrgObject(obj, isUnloadAsset))
        {
            bool isGameObj = (obj != null) && (obj is GameObject);
            if (isGameObj)
            {
                if (!isUnloadAsset)
                {
                    // 删除实例化的GameObject
                    int instId = obj.GetInstanceID();
                    // gameObj.transform.parent = null;
                    if (Application.isPlaying)
                    {
                        if (!IsQuitApp)
                            UnityEngine.GameObject.Destroy(obj);
                    }
                    else
                        UnityEngine.GameObject.DestroyImmediate(obj);
                    AssetCacheManager.Instance._OnDestroyGameObject(instId);
                }
            }
            else
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                    {
                        if (!IsQuitApp)
                            UnityEngine.GameObject.Destroy(obj);
                    }
                    else
                        UnityEngine.GameObject.DestroyImmediate(obj);
                }

            }
        }
    }

    public GameObject LoadPrefab(string fileName, ResourceCacheType cacheType)
    {
        GameObject ret = mAssetLoader.LoadPrefab(fileName, cacheType);
        if (ret != null)
            return ret;

        /*string realFileName = fileName;

		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadPrefab (realFileName, cacheType);*/
        return mResLoader.LoadPrefab(fileName, cacheType);
    }

    public bool LoadPrefabAsync(string fileName, Action<float, bool, GameObject> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadPrefabAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadPrefabAsync (realFileName, cacheType, onProcess);*/
        return mResLoader.LoadPrefabAsync(fileName, cacheType, onProcess, priority);
    }

    public bool CreateGameObjectAsync(string fileName, Action<float, bool, GameObject> onProcess, int priority = 0)
    {
        bool ret = CreatePrefabAsync(fileName,
          delegate (float process, bool isDone, GameObject instObj)
          {
              if (isDone)
              {
                  if (instObj != null)
                  {
                      var script = instObj.AddComponent<ResInstDestroy>();
                      script.CheckVisible();
                  }
                  if (onProcess != null)
                      onProcess(process, isDone, instObj);
                  return;
              }

              if (onProcess != null)
                  onProcess(process, isDone, instObj);
          }, priority
        );

        return ret;
    }

    public bool CreateGameObjectAsync(string fileName, float delayDestroyTime, Action<float, bool, GameObject> onProcess, int priority = 0)
    {
        bool ret = CreatePrefabAsync(fileName,
                                     delegate (float process, bool isDone, GameObject instObj)
                                     {
                                         if (isDone)
                                         {
                                             if (instObj != null)
                                             {
                                                 ResInstDelayDestroy script = instObj.AddComponent<ResInstDelayDestroy>();
                                                 script.DelayDestroyTime = delayDestroyTime;
                                                 script.CheckVisible();
                                             }
                                             if (onProcess != null)
                                                 onProcess(process, isDone, instObj);
                                             return;
                                         }

                                         if (onProcess != null)
                                             onProcess(process, isDone, instObj);
                                     }, priority
        );

        return ret;
    }

    private GameObject CreatePrefab(string fileName)
    {
        GameObject orgObj = LoadPrefab(fileName, ResourceCacheType.rctTemp);
        if (orgObj != null)
        {
            return CreateGameObject(orgObj);
        }

        return null;
    }

    private bool CreatePrefabAsync(string fileName, Action<float, bool, GameObject> onProcess, int priority)
    {
        bool ret = LoadPrefabAsync(fileName,
                                   delegate (float t, bool isDone, GameObject orgObj)
                                   {
                                       if (isDone)
                                       {
                                           GameObject obj = null;
                                           if (orgObj != null)
                                           {
                                               obj = CreateGameObject(orgObj);
                                               ResourceMgr.Instance.DestroyObject(orgObj);
                                           }
                                           if (onProcess != null)
                                               onProcess(t, isDone, obj);
                                           return;
                                       }

                                       if (onProcess != null)
                                           onProcess(t, isDone, null);
                                   }, ResourceCacheType.rctRefAdd, priority
        );

        return ret;
    }

    // if use addRefCount you must call UnLoadOrgObject
    public Texture LoadTexture(string fileName, ResourceCacheType cacheType)
    {
        Texture ret = mAssetLoader.LoadTexture(fileName, cacheType);
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
    public bool LoadTextureAsync(string fileName, Action<float, bool, Texture> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadTextureAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadTextureAsync (realFileName, cacheType, onProcess);*/
        return mResLoader.LoadTextureAsync(fileName, cacheType, onProcess, priority);
    }

    // if use addRefCount you must call UnLoadOrgObject
    public Material LoadMaterial(string fileName, ResourceCacheType cacheType)
    {
        Material ret = mAssetLoader.LoadMaterial(fileName, cacheType);
        if (ret != null)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadMaterial (realFileName, cacheType);*/
        return mResLoader.LoadMaterial(fileName, cacheType);
    }

    // if use addRefCount you must call UnLoadOrgObject
    public bool LoadMaterialAsync(string fileName, Action<float, bool, Material> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadMaterialAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadMaterialAsync (realFileName, cacheType, onProcess);*/
        return mResLoader.LoadMaterialAsync(fileName, cacheType, onProcess, priority);
    }

    // if use ResourceCacheType.rctTempAdd you must call UnLoadOrgObject
    // isUnloadAsset只针对原始资源
    private bool UnLoadOrgObject(UnityEngine.Object orgObj, bool isUnloadAsset)
    {
        if (orgObj == null)
            return false;
        AssetCache cache = AssetCacheManager.Instance.FindOrgObjCache(orgObj);
        if (cache == null)
        {
            if (isUnloadAsset)
            {
                if (!(orgObj is GameObject))
                {
                    Sprite sp = orgObj as UnityEngine.Sprite;
                    if (sp != null && sp.texture != null)
                    {
                        Resources.UnloadAsset(sp.texture);
                    }
                    Resources.UnloadAsset(orgObj);
                    return true;
                }
                else
                {
                    if (!Application.isEditor && cache is AssetBundleCache)
                    {
                        // 只有AB的原始GAMEOBJECT才支持这个
                        GameObject.DestroyImmediate(orgObj, true);
                        return true;
                    }
                }
            }
            return false;
        }

        bool ret = AssetCacheManager.Instance.CacheDecRefCount(cache);
        if (ret && isUnloadAsset)
        {
            AssetCacheManager.Instance._OnUnloadAsset(cache, orgObj);
        }

        return ret;
    }

    public AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType)
    {
        AudioClip ret = mAssetLoader.LoadAudioClip(fileName, cacheType);
        if (ret != null)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadAudioClip (realFileName, cacheType);*/
        return mResLoader.LoadAudioClip(fileName, cacheType);
    }

    public bool LoadAudioClipAsync(string fileName, Action<float, bool, AudioClip> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadAudioClipAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadAudioClipAsync (fileName, cacheType, onProcess);*/
        return mResLoader.LoadAudioClipAsync(fileName, cacheType, onProcess, priority);
    }

    public byte[] LoadBytes(string fileName, ResourceCacheType cacheType = ResourceCacheType.rctNone)
    {
        byte[] ret = mAssetLoader.LoadBytes(fileName, cacheType);
        if (ret != null)
            return ret;
        return mResLoader.LoadBytes(fileName, cacheType);
    }

    public string LoadText(string fileName, ResourceCacheType cacheType = ResourceCacheType.rctNone)
    {
        string ret = mAssetLoader.LoadText(fileName, cacheType);
        if (ret != null)
            return ret;

        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadText (realFileName, cacheType);*/
        return mResLoader.LoadText(fileName, cacheType);
    }

    public bool LoadTextAsync(string fileName, Action<float, bool, TextAsset> onProcess, ResourceCacheType cacheType = ResourceCacheType.rctNone, int priority = 0)
    {
        bool ret = mAssetLoader.LoadTextAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadTextAsync (realFileName, cacheType, onProcess);*/
        return mResLoader.LoadTextAsync(fileName, cacheType, onProcess, priority);
    }

    public AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType)
    {
        AnimationClip ret = mAssetLoader.LoadAnimationClip(fileName, cacheType);
        if (ret != null)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadAnimationClip (realFileName, cacheType);*/
        return mResLoader.LoadAnimationClip(fileName, cacheType);
    }

    public bool LoadAnimationClipAsync(string fileName, Action<float, bool, AnimationClip> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadAnimationClipAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadAnimationClipAsync (realFileName, cacheType, onProcess);*/
        return mResLoader.LoadAnimationClipAsync(fileName, cacheType, onProcess, priority);
    }

    public RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType)
    {
        RuntimeAnimatorController ret = mAssetLoader.LoadAniController(fileName, cacheType);
        if (ret != null)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return null;
		return loader.LoadAniController (realFileName, cacheType);*/
        return mResLoader.LoadAniController(fileName, cacheType);
    }

    public bool LoadAniControllerAsync(string fileName, Action<float, bool, RuntimeAnimatorController> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadAniControllerAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        /*
		string realFileName = fileName;
		IResourceLoader loader = GetLoader (ref realFileName);
		if ((loader == null) || (mAssetLoader == loader))
			return false;
		return loader.LoadAniControllerAsync (realFileName, cacheType, onProcess);*/
        return mResLoader.LoadAniControllerAsync(fileName, cacheType, onProcess, priority);
    }

    public Shader LoadShader(string fileName, ResourceCacheType cacheType)
    {
        Shader ret = mAssetLoader.LoadShader(fileName, cacheType);
        if (ret != null)
            return ret;
        return mResLoader.LoadShader(fileName, cacheType);
    }

    public bool LoadShaderAsync(string fileName, Action<float, bool, Shader> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadShaderAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        return mResLoader.LoadShaderAsync(fileName, cacheType, onProcess, priority);
    }

    public Font LoadFont(string fileName, ResourceCacheType cacheType)
    {
        Font ret = mAssetLoader.LoadFont(fileName, cacheType);
        if (ret != null)
            return ret;
        return mResLoader.LoadFont(fileName, cacheType);
    }

    public bool LoadFontAsync(string fileName, Action<float, bool, Font> onProcess, ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadFontAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        return mResLoader.LoadFontAsync(fileName, cacheType, onProcess, priority);
    }

    public bool PreLoadAndBuildAssetBundleShaders(string abFileName, Action onEnd = null, int priority = 0)
    {

        System.Type type = typeof(Shader);

        // 预先加载Shader
        if (!PreLoadAssetBundle(abFileName, type,
            delegate ()
            {
                Shader.WarmupAllShaders();
                if (onEnd != null)
                    onEnd();
            }, priority
            ))
            return false;


        return true;
    }

    public bool PreLoadAssetBundle(string abFileName, System.Type type, Action onEnd = null, int priority = 0)
    {
        if (mAssetLoader == null || type == null)
            return false;

        AssetLoader loader = mAssetLoader as AssetLoader;
        if (loader == null)
            return false;

        if (!loader.PreloadAllType(abFileName, type, onEnd, priority))
            return false;

        return true;
    }

    private void DestroyObjects<T>(T[] objs, bool isUnloadAsset = false) where T : UnityEngine.Object
    {
        if (objs == null || objs.Length <= 0)
            return;
        for (int i = 0; i < objs.Length; ++i)
        {
            DestroyObject(objs[i], isUnloadAsset);
        }
    }

    public void DestroySprites(Sprite[] sprites, bool isUnloadAsset = false)
    {
        DestroyObjects<Sprite>(sprites, isUnloadAsset);
    }

    public void DestroyObjects(UnityEngine.Object[] objs, bool isUnloadAsset = false)
    {
        DestroyObjects<UnityEngine.Object>(objs, isUnloadAsset);
    }

    public Sprite[] LoadSprites(string fileName)
    {
        Sprite[] ret = mAssetLoader.LoadSprites(fileName);
        if (ret != null)
            return ret;
        return mResLoader.LoadSprites(fileName);
    }

    public bool LoadSpritesAsync(string fileName, Action<float, bool, UnityEngine.Object[]> onProcess, int priority = 0)
    {
        bool ret = mAssetLoader.LoadSpritesAsync(fileName, onProcess, priority);
        if (ret)
            return ret;
        return mResLoader.LoadSpritesAsync(fileName, onProcess, priority);
    }

    public ScriptableObject LoadScriptableObject(string fileName, ResourceCacheType cacheType)
    {
        ScriptableObject ret = mAssetLoader.LoadScriptableObject(fileName, cacheType);
        if (ret != null)
            return ret;
        return mResLoader.LoadScriptableObject(fileName, cacheType);
    }

    public bool LoadScriptableObjectAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, ScriptableObject> onProcess, int priority = 0)
    {
        bool ret = mAssetLoader.LoadScriptableObjectAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        return mResLoader.LoadScriptableObjectAsync(fileName, cacheType, onProcess, priority);
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
        ResourceCacheType cacheType, int priority = 0)
    {
        bool ret = mAssetLoader.LoadShaderVarCollectionAsync(fileName, cacheType, onProcess, priority);
        if (ret)
            return ret;
        return mResLoader.LoadShaderVarCollectionAsync(fileName, cacheType, onProcess, priority);
    }

#endif

    private AsyncOperation m_UnUsedOpr = null;

    public void UnloadUnUsed()
    {
        // 娓呴櫎鎵?鏈夋湭浣跨敤鐨?		AssetCacheManager.Instance.ClearUnUsed ();

        if (m_UnUsedOpr == null || m_UnUsedOpr.isDone)
            m_UnUsedOpr = Resources.UnloadUnusedAssets();
    }

    public IResourceLoader AssetLoader
    {
        get
        {
            return mAssetLoader;
        }
    }

    public IResourceLoader ResLoader
    {
        get
        {
            return mResLoader;
        }
    }

    public bool IsQuitApp
    {
        get;
        private set;
    }

    private void OnAppExit(bool isUnloadTrue = true)
    {
        AsyncOperationMgr.Instance.Clear();
        AssetCacheManager.Instance.AutoUpdateClear(isUnloadTrue);
        AssetLoader loader = mAssetLoader as AssetLoader;
        if (loader != null)
            loader.AutoUpdateClear();
        ResourcesLoader resLoader = mResLoader as ResourcesLoader;
        if (resLoader != null)
        {
            resLoader.AutoUpdateClear();
        }
    }

    // 应用QUIT通知
    public void OnApplicationQuit()
    {
        IsQuitApp = true;
        OnAppExit();
    }

    // 资源更新清理
    public void AutoUpdateClear()
    {
        OnAppExit(false);
        UnloadUnUsed();
    }

    public string GetABShaderFileNameByName(string shaderName)
    {
        AssetLoader loader = mAssetLoader as AssetLoader;
        if (loader != null)
            return loader.GetShaderFileNameByName(shaderName);
        return string.Empty;
    }

    public string GetAssetBundleFileName(string fileName)
    {
        AssetLoader loader = mAssetLoader as AssetLoader;
        if (loader != null)
            return loader.GetAssetBundleFileName(fileName);
        return string.Empty;
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