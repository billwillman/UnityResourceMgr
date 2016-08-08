/*----------------------------------------------------------------
// 模块名：AssetBundle Loader
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年9月12日
// 模块描述：
//----------------------------------------------------------------*/

#define USE_UNITY5_X_BUILD
// #define USE_LOWERCHAR
#define USE_HAS_EXT

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using XmlParser;

public class AssetBundleCache: AssetCache
{
	public AssetBundleCache(AssetInfo target)
	{
		mTarget = target;
	}

	public AssetBundleCache()
	{}

	public override bool IsNotUsed() {
		return (RefCount <= 0) && ((mTarget == null) || (!mTarget.IsUsing));
	}

	public static AssetBundleCache Create(AssetInfo target)
	{
		AssetBundleCache ret;
		if (m_PoolUsed)
			ret = GetPool(target);
		else
			ret = new AssetBundleCache(target);
		return ret;
	}
	
	protected override void OnUnLoad()
	{
		if (mTarget != null) {
			mTarget.UnLoad ();
			mTarget = null;
		}

		if (m_PoolUsed)
		{
			InPool(this);
		}
	}

	protected override void OnUnUsed()
	{
		if (mTarget != null)
		{
			mTarget.UnUsed();
			mTarget = null;
		}

		if (m_PoolUsed)
		{
			InPool(this);
		}
	}
	
	public AssetInfo Target {
		get
		{
			return mTarget;
		}
	}

	private static AssetBundleCache GetPool(AssetInfo target)
	{
		InitPool();
		AssetBundleCache ret = m_Pool.GetObject();
		ret.mTarget = target;
		return ret;
	}

	private static void InPool(AssetBundleCache cache)
	{
		if (cache == null)
			return;
		InitPool();
		m_Pool.Store(cache);
	}

	private static void InitPool() {
		if (!m_PoolInited) {
			m_Pool.Init(0);
			m_PoolInited = true;
		}
	}
	
	private AssetInfo mTarget = null;

	private static Utils.ObjectPool<AssetBundleCache> m_Pool = new Utils.ObjectPool<AssetBundleCache>();
	private static bool m_PoolUsed = true;
	private static bool m_PoolInited = false;
}


public enum AssetCompressType
{
	// 未压缩
	astNone = 0,
	// Unity Zip
	astUnityZip = 1,
	// Lzo压缩
	astUnityLzo = 2,
	// Zip压缩
	astZip
}

public struct DependFileInfo
{
	public string fileName;
	public int refCount;
}

// Asset info
public class AssetInfo
{

	public AssetInfo(string fileName)
	{
		mFileName = fileName;
	}

	public string FileName
	{
		get {
			return mFileName;
		}
	}

	// 正在加载中
	public bool IsUsing {
		get;
		set;
	}

	internal WWWFileLoadTask WWWTask
	{
		get
		{
			return m_WWWTask;
		}
	}

	#if UNITY_5_3
	internal BundleCreateAsyncTask AsyncTask
	{
		get
		{
			return m_AsyncTask;
		}
	}
	#endif
	
	private WWWFileLoadTask m_WWWTask = null;
	#if UNITY_5_3
	private BundleCreateAsyncTask m_AsyncTask = null;
	#endif
	private TaskList m_TaskList = null;
	private Timer m_Timer = null;
	private Action<bool> m_EndEvt = null;
	internal void ClearTaskData()
	{
		m_EndEvt = null;

		if (m_Timer != null)
		{
			m_Timer.Dispose();
			m_Timer = null;
		}

		if (m_WWWTask != null)
		{
			m_WWWTask.Release();
			m_WWWTask = null;
		}

		#if UNITY_5_3
		if (m_AsyncTask != null)
		{
			m_AsyncTask.Release();
			m_AsyncTask = null;
		}
		#endif

		if (m_TaskList != null)
		{
			m_TaskList.Clear();
			m_TaskList = null;
		}
	}

	private void OnTimerEvt(Timer obj, float timer)
	{
		if (m_TaskList != null)
		{
			m_TaskList.Process();
			if (m_TaskList.IsEmpty)
			{
				if (m_EndEvt != null)
					m_EndEvt(true);
				ClearTaskData();
			}
		}
	}

	internal void OnTaskResult(ITask task)
	{
		if (task == null)
			return;
		if (task.IsDone)
		{
			if (task.IsFail)
			{
				if (m_EndEvt != null)
					m_EndEvt(false);
				ClearTaskData();
			}
		}
	}

	public TaskList CreateTaskList(Action<bool> onEnd)
	{
		if (m_TaskList == null)
		{
			m_TaskList = new TaskList();
			m_TaskList.UserData = this;
			if (m_Timer == null)
			{
				m_Timer = TimerMgr.Instance.CreateTimer(false, 0, true);
				m_Timer.AddListener(OnTimerEvt);
			}
		}

		m_EndEvt += onEnd;
		return m_TaskList;
	}

	#if UNITY_5_3
	private static void OnLocalAsyncResult(ITask task)
	{
		BundleCreateAsyncTask asycTask = task as BundleCreateAsyncTask;
		if (asycTask == null)
			return;
		AssetInfo info = asycTask.UserData as AssetInfo;
		if (info == null)
			return;
		if (asycTask.IsDone && asycTask.IsOk)
		{
			info.mBundle = asycTask.Bundle;
		}

		info.IsUsing = false;
	}
	#endif

	private static void OnLocalWWWResult(ITask task)
	{
		WWWFileLoadTask wwwTask = task as WWWFileLoadTask;
		if (wwwTask == null)
			return;
		AssetInfo info = wwwTask.UserData as AssetInfo;
		if (info == null)
			return;
		if (wwwTask.IsDone && wwwTask.IsOk)
		{
			info.mBundle = wwwTask.Bundle;
		}

		info.IsUsing = false;
	}

	internal void AddNoOwnerToTaskList(TaskList taskList, ITask task) {
		if (task == null || taskList == null)
			return;
		if (!taskList.Contains(task)) {
			taskList.AddTask(task, false);
			if (taskList.UserData != null) {
				AssetInfo parent = taskList.UserData as AssetInfo;
				if (parent != null) {
					task.AddResultEvent(parent.OnTaskResult);
				}
			}
		}
	}

#if UNITY_5_3

	// 5.3新的异步加载方法
	public bool LoadAsync(TaskList taskList)
	{
		if (IsVaild())
			return true;
		if (string.IsNullOrEmpty (mFileName))
			return false;
		
		if (taskList == null)
			return false;

		if (m_AsyncTask != null)
		{
			AddNoOwnerToTaskList(taskList, m_AsyncTask);
			return true;
		}

		m_AsyncTask = new BundleCreateAsyncTask(mFileName);
		if (m_AsyncTask != null)
		{
			m_AsyncTask.UserData = this;
			m_AsyncTask.AddResultEvent(OnLocalAsyncResult);
			taskList.AddTask(m_AsyncTask, true);
			if (taskList.UserData != null)
			{
				AssetInfo parent = taskList.UserData as AssetInfo;
				if (parent != null)
				{
					m_AsyncTask.AddResultEvent(parent.OnTaskResult);
				}
			}
		} else
			return false;
		
		return true;
	}

#endif

	public bool LoadWWW(TaskList taskList)
	{
		if (IsVaild ())
			return true;
		if (string.IsNullOrEmpty (mFileName))
			return false;

		if (taskList == null)
			return false;

		if (m_WWWTask != null)
		{
			AddNoOwnerToTaskList(taskList, m_WWWTask);
			return true;
		}

		m_WWWTask = WWWFileLoadTask.LoadFileName(mFileName);
		if (m_WWWTask != null)
		{
			m_WWWTask.UserData = this;
			m_WWWTask.AddResultEvent(OnLocalWWWResult);
			taskList.AddTask(m_WWWTask, true);
			if (taskList.UserData != null)
			{
				AssetInfo parent = taskList.UserData as AssetInfo;
				if (parent != null)
				{
					m_WWWTask.AddResultEvent(parent.OnTaskResult);
				}
			}
		} else
			return false;
		
		return true;
	}
	
	internal void PreLoadAll(System.Type allType, Action<AssetInfo, UnityEngine.Object> OnLoadEvt)
	{
		var iter = mChildFileNameHashs.GetEnumerator();
		while (iter.MoveNext())
		{
			UnityEngine.Object obj = LoadObject(iter.Current, allType);
			if (obj != null)
			{
				if (OnLoadEvt != null)
					OnLoadEvt(this, obj);
			}
		}
		iter.Dispose();
	}

	public bool Load()
	{
		if (IsVaild ())
			return true;

		if (string.IsNullOrEmpty (mFileName))
			return false;

	//	mIsLoading = false;
		if (mCompressType == AssetCompressType.astNone) {
			ClearTaskData();
#if UNITY_5_3
			mBundle = AssetBundle.LoadFromFile (mFileName);
#else
			mBundle = AssetBundle.CreateFromFile(mFileName);
#endif
			if (mBundle == null)
				return false;
		} else 
		if (mCompressType == AssetCompressType.astUnityLzo
#if UNITY_5_3
			|| mCompressType == AssetCompressType.astUnityZip
#endif
			) {
			// Lz4 new compressType
			ClearTaskData();
#if UNITY_5_3
			mBundle = AssetBundle.LoadFromFile(mFileName);
#else
			mBundle = AssetBundle.CreateFromFile(mFileName);
#endif
			if (mBundle == null)
				return false;
			return true;
		} else {
			// zan shi
			return false;
		}

		return true;
	}

	internal T[] LoadSubs<T>(string fileName) where T: UnityEngine.Object {
		if (string.IsNullOrEmpty(fileName) || (!IsVaild()))
			return null;

#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		// int hashCode = Animator.StringToHash (fileName);
		if (!ContainFileNameHash(fileName))
			return null;

		string realFileName = Path.GetFileNameWithoutExtension(fileName);
		return mBundle.LoadAssetWithSubAssets<T>(realFileName);
	}

	// 所有文件不要重名（不包含后缀的）
	public UnityEngine.Object LoadObject(string fileName, Type objType)
	{
		if (string.IsNullOrEmpty (fileName) || (!IsVaild()))
			return null;
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		// int hashCode = Animator.StringToHash (fileName);
		if (!ContainFileNameHash (fileName))
			return null;

#if USE_UNITY5_X_BUILD
#else
		if (IsMainAsset) {
			return mBundle.mainAsset;
		}
#endif

		UnityEngine.Object search = null;
		if (GetOrgResMap(fileName, out search))
			return search;

		string realFileName = Path.GetFileNameWithoutExtension (fileName);

#if USE_UNITY5_X_BUILD
        UnityEngine.Object ret = mBundle.LoadAsset(realFileName, objType);
#else
		UnityEngine.Object ret = mBundle.Load (realFileName, objType);
#endif

		if (ret != null)
			AddOrgResMap(fileName, ret);

		return ret;

	}

	protected bool IsMainAsset
	{
		get {
			if (!IsVaild())
				return false;
			if ((mChildFileNameHashs == null) || (mChildFileNameHashs.Count != 1))
				return false;
			return true;
		}
	}

	public bool GetOrgResMap(string fileName, out UnityEngine.Object obj)
	{
		obj = null;
		if (string.IsNullOrEmpty(fileName))
			return false;
		return m_OrgResMap.TryGetValue(fileName, out obj);
	}

	public void AddOrgResMap(string fileName, UnityEngine.Object obj)
	{
		if (string.IsNullOrEmpty(fileName) || obj == null)
			return;
		if (m_OrgResMap.ContainsKey(fileName))
			m_OrgResMap[fileName] = obj;
		else
			m_OrgResMap.Add(fileName, obj);
	}

	internal bool LoadSubsAsync<T>(string fileName, Action<AssetBundleRequest> onProcess) where T: UnityEngine.Object {
		if (string.IsNullOrEmpty(fileName) || (!IsVaild()))
			return false;
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif

		if (!ContainFileNameHash(fileName))
			return false;

		System.Type objType = typeof(T);
		// 目的：減少I/O操作
		var item = FindAsyncLoadDict(fileName, objType);
		if (item != null)
		{
			item.onProcess += onProcess;
			return true;
		}

		string realFileName = Path.GetFileNameWithoutExtension(fileName);
		AssetBundleRequest request = mBundle.LoadAssetWithSubAssetsAsync<T>(realFileName);
		if (request == null)
			return false;

		if (request.isDone) {
			if (onProcess != null)
				onProcess(request);

			return true;
		}


		return AddAsyncOperation(fileName, objType, request, onProcess);
	}

	protected struct AsyncLoadKey
	{
		public string fileName;
		public System.Type type;
	}

	private bool AddAsyncOperation(string fileName, System.Type objType, AssetBundleRequest request, Action<AssetBundleRequest> onProcess)
	{
		if (string.IsNullOrEmpty(fileName) || objType == null || request == null)
			return false;
		
		var item = AsyncOperationMgr.Instance.AddAsyncOperation<AssetBundleRequest, AsyncLoadKey>(request, onProcess);
		bool ret = item != null;
		if (ret)
		{
			AddAsyncLoadDict(fileName, objType, item);
			item.onProcess += OnAsyncLoadEvt;
		}
		return ret;
	}

	private void AddAsyncLoadDict(string fileName, System.Type objType, AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey> req)
	{
		if (req == null || string.IsNullOrEmpty(fileName) || objType == null)
			return;
		AsyncLoadKey key = new AsyncLoadKey();
		key.fileName = fileName;
		key.type = objType;
		req.UserData = key;
		if (m_AsyncLoadDict.ContainsKey(key))
			m_AsyncLoadDict[key] = req;
		else
			m_AsyncLoadDict.Add(key, req);
	}

	private AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey> FindAsyncLoadDict(string fileName, System.Type objType)
	{
		if (string.IsNullOrEmpty(fileName) || objType == null)
			return null;
		AsyncLoadKey key = new AsyncLoadKey();
		key.fileName = fileName;
		key.type = objType;
		AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey> ret;
		if (!m_AsyncLoadDict.TryGetValue(key, out ret))
			return null;
		return ret;
	}

	private void RemoveAsyncLoadDict(AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey> item)
	{
		if (item == null)
			return;
		if (string.IsNullOrEmpty(item.UserData.fileName))
			return;
		if (m_AsyncLoadDict.ContainsKey(item.UserData))
			m_AsyncLoadDict.Remove(item.UserData);
	}

	private void OnAsyncLoadEvt(AssetBundleRequest req)
	{
		if (req == null)
			return;
		if (!req.isDone)
			return;
		
		var item = AsyncOperationMgr.Instance.FindItem<AssetBundleRequest, AsyncLoadKey>(req);
		if (item == null)
			return;
		RemoveAsyncLoadDict(item);
	}

	public bool LoadObjectAsync(string fileName, Type objType, Action<AssetBundleRequest> onProcess)
	{
		if (string.IsNullOrEmpty (fileName) || (!IsVaild()))
			return false;
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		//int hashCode = Animator.StringToHash (fileName);
		if (!ContainFileNameHash (fileName))
			return false;

		// 目的：減少I/O操作(Unity 同時讀取統一個東西，會產生多份Request)
		var req = FindAsyncLoadDict(fileName, objType);
		if (req != null)
		{
			if (onProcess != null)
				req.onProcess += onProcess;
			return true;
		}

		string realFileName = Path.GetFileNameWithoutExtension(fileName);
#if USE_UNITY5_X_BUILD
        AssetBundleRequest request = mBundle.LoadAssetAsync(realFileName, objType);
#else
		AssetBundleRequest request = mBundle.LoadAsync(realFileName, objType);
#endif
		if (request == null)
			return false;

		if (request.isDone)
		{
			if (onProcess != null)
				onProcess(request);
				
			return true;
		}

		return AddAsyncOperation(fileName, objType, request, onProcess);
	}

	/*
	public bool IsNew()
	{
		return !IsVaild() && !IsUsing;
	}*/

	public bool IsVaild()
	{
		return (mBundle != null);
	}

	public bool HasChildFiles()
	{
		return IsVaild() && (mChildFileNameHashs != null) && (mChildFileNameHashs.Count > 0);
	}

	public int DependFileCount
	{
		get {
			if (mDependFileNames == null)
				return 0;
			return mDependFileNames.Count;
		}
	}

	public DateTime UpdateTime
	{
		get {
			return mUpdateTime;
		}
	}

	public void _SetUpdateTime(DateTime time)
	{
		mUpdateTime = time;
	}

	public string GetDependFileName(int index)
	{
		if (mDependFileNames == null)
			return string.Empty;
		if ((index < 0) || (index >= mDependFileNames.Count))
			return string.Empty;

		return mDependFileNames [index].fileName;
	}

	public int GetDependFileRef(int index)
	{
		if (mDependFileNames == null)
			return 0;
		if ((index < 0) || (index >= mDependFileNames.Count))
			return 0;
		
		return mDependFileNames [index].refCount;
	}

	/*
	public bool ContainFileNameHash(int fileNameHash)
	{
		if (HasChildFiles ()) {
			return mChildFileNameHashs.Contains (fileNameHash);
		}
	
		return false;
	}*/

	public bool ContainFileNameHash(string fileName)
	{
		if (HasChildFiles ()) {
			return mChildFileNameHashs.Contains (fileName);
		}
		
		return false;
	}

	public List<DependFileInfo> DependFileNames 
	{
		get {
			return mDependFileNames;
		}
	}

	public AssetCompressType CompressType
	{
		get {
			return mCompressType;
		}
	}

	public void _SetCompressType(AssetCompressType compress)
	{
		mCompressType = compress;
	}

	// 让依赖库减1
	private void DecDependInfo()
	{
		// 处理依赖关系
		RemoveDepend(this);
	}

	public void UnLoad()
	{
		if (IsVaild() && !IsUsing) {

			// LogMgr.Instance.Log(string.Format("Bundle unload=>{0}", Path.GetFileNameWithoutExtension(mFileName)));
			m_OrgResMap.Clear();
			m_AsyncLoadDict.Clear();

			mBundle.Unload(true);
			mBundle = null;
			mCache = null;

			ClearTaskData();

			DecDependInfo();
		}
	}

	public void UnUsed()
	{
		if (IsVaild() && !IsUsing) {

			m_OrgResMap.Clear();
			m_AsyncLoadDict.Clear();

			mBundle.Unload(false);
			mBundle = null;
			mCache = null;
			ClearTaskData();
			// 处理依赖关系
			DecDependInfo();
		}
	}

	private static AssetLoader Loader {
		get {
			if (mLoader == null)
				mLoader = ResourceMgr.Instance.AssetLoader as AssetLoader;
			return mLoader;
		}
	}

	private static void RemoveDepend(AssetInfo asset)
	{
		if (asset == null)
			return;
		if ((asset.DependFileCount <= 0) || asset.IsUsing)
			return;

		asset.IsUsing = true;

		AssetLoader loader = Loader;
		for (int i = 0; i < asset.DependFileCount; ++i) {
			string fileName = asset.GetDependFileName(i);
			AssetInfo dependInfo = loader.FindAssetInfo(fileName);
			if ((dependInfo != null) && (!dependInfo.IsUsing))
			{
				if (dependInfo.Cache != null)
				{
					int refCount = asset.GetDependFileRef(i);
					AssetCacheManager.Instance.CacheDecRefCount(dependInfo.Cache, refCount);
				}
				//RemoveDepend(dependInfo);
			}
		}

		asset.IsUsing = false;
	}

	// 是否是预加载
	public bool IsPreLoad
	{
		get {
			return mIsPreLoad;
		}
	}

	// 是否是预加载资源(预加载资源不会被释放，永远在内存)
	public void _SetIsPreLoad(bool isPreLoad)
	{
		mIsPreLoad = isPreLoad;
	}

	public AssetCache Cache
	{
		get {
			return mCache;
		}

		set {
			mCache = value;
		}
	}

	/*
	public void _AddSubFile(int hashCode)
	{
		if (mChildFileNameHashs == null)
			mChildFileNameHashs = new HashSet<int> ();
		else
		if (mChildFileNameHashs.Contains (hashCode))
			return;
		mChildFileNameHashs.Add (hashCode);
	}*/

	internal void _AddSubFile(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return;

		if (mChildFileNameHashs == null)
			mChildFileNameHashs = new HashSet<string> ();
		else
		if (mChildFileNameHashs.Contains (fileName))
			return;
		mChildFileNameHashs.Add (fileName);
	}

	internal void _AddDependFile(string fileName, int refCount = 1)
	{
		if (string.IsNullOrEmpty (fileName))
			return;
		if (mDependFileNames == null)
			mDependFileNames = new List<DependFileInfo> ();
		DependFileInfo info = new DependFileInfo();
		info.fileName = fileName;
		info.refCount = refCount;
		mDependFileNames.Add (info);
	}

	// 包含的Child的文件名 HashCode
	//private HashSet<int> mChildFileNameHashs = null;
	private HashSet<string> mChildFileNameHashs = null;
	// 異步加載保存池(減少IO操作)
	private Dictionary<AsyncLoadKey, AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey>> m_AsyncLoadDict = new Dictionary<AsyncLoadKey, AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey>>();
	private Dictionary<string, UnityEngine.Object> m_OrgResMap = new Dictionary<string, UnityEngine.Object>();
	// 依赖的AssetBundle文件名（包含路径）
	private List<DependFileInfo> mDependFileNames = null;
	private AssetBundle mBundle = null;
	// 文件名HashCode
	private string mFileName = string.Empty;
	// 更新时间
	private DateTime mUpdateTime;
	// 是否是需要预加载AssetBundle
	private bool mIsPreLoad = false;
	// 是否正在加载状态
//	private bool mIsLoading = false;
	// 压缩方式
	private AssetCompressType mCompressType = AssetCompressType.astNone;
	private AssetCache mCache = null;
	private static AssetLoader mLoader = null;
}

public class AssetLoader: IResourceLoader
{
	public override bool OnSceneLoad(string sceneName)
	{
		if (string.IsNullOrEmpty (sceneName))
			return false;
#if USE_LOWERCHAR
		sceneName = sceneName.ToLower();
#endif
		sceneName += ".unity";
		AssetInfo asset = FindAssetInfo (sceneName);
		if (asset == null)
			return false;

		int addCount = 0;
		if (!LoadAssetInfo (asset, ref addCount))
			return false;
		
		AddOrUpdateAssetCache (asset);

		return true;
	}

	public override bool OnSceneLoadAsync(string sceneName, Action onEnd)
	{
		if (string.IsNullOrEmpty (sceneName))
			return false;
		#if USE_LOWERCHAR
		sceneName = sceneName.ToLower();
		#endif
		sceneName += ".unity";
		AssetInfo asset = FindAssetInfo (sceneName);
		if (asset == null)
			return false;
		int addCount = 0;
#if UNITY_5_3
		if (asset.CompressType == AssetCompressType.astUnityLzo || 
			asset.CompressType == AssetCompressType.astUnityZip || 
			asset.CompressType == AssetCompressType.astNone
		   )
		{
				return LoadAsyncAssetInfo(asset, null, ref addCount,
					delegate (bool isOk) {
						if (isOk)
						{
							AddOrUpdateAssetCache (asset);
							if (onEnd != null)
								onEnd();
						}
					}
				);
		} else
#endif
		if (asset.CompressType == AssetCompressType.astUnityZip)
		{
			return LoadWWWAsseetInfo(asset, null, ref addCount, 
			                  delegate (bool isOk) {
								if (isOk)
								{
									AddOrUpdateAssetCache (asset);
									if (onEnd != null)
										onEnd();
								}
							 }
			);
		} else
		{
			if (!LoadAssetInfo (asset, ref addCount))
				return false;
			AddOrUpdateAssetCache (asset);
			if (onEnd != null)
				onEnd();
		}
		return true;
	}

	public override bool OnSceneClose(string sceneName)
	{
		if (string.IsNullOrEmpty (sceneName))
			return false;
		#if USE_LOWERCHAR
		sceneName = sceneName.ToLower();
		#endif
		sceneName += ".unity";
		AssetInfo asset = FindAssetInfo (sceneName);
		if (asset == null)
			return false;

		bool ret = AssetCacheManager.Instance.CacheDecRefCount (asset.Cache);
		return ret;
	}

	private void OnPreaLoadObj(AssetInfo asset, UnityEngine.Object obj)
	{
		if (asset.Cache != null)
			AssetCacheManager.Instance._OnLoadObject(obj, asset.Cache);
	}

	private void DoPreloadAllType(AssetInfo asset, System.Type type)
	{
		bool isNew = asset.Cache == null;
		if (isNew)
		{
			AddOrUpdateAssetCache (asset);
		} else
			AssetCacheManager.Instance.CacheAddRefCount(asset.Cache);
		
		asset.PreLoadAll(type, OnPreaLoadObj);
	}

	public bool PreloadAllType(string abFileName, System.Type type, Action onEnd = null)
	{
		if (type == null || string.IsNullOrEmpty(abFileName))
			return false;

		AssetInfo asset = FindAssetInfo(abFileName);
		if (asset == null)
			return false;

		int addCount = 0;
		//bool isNew = asset.IsNew();
#if UNITY_5_3
		if (asset.CompressType == AssetCompressType.astUnityLzo || 
			asset.CompressType == AssetCompressType.astUnityZip ||
			asset.CompressType == AssetCompressType.astNone
		   ) {
			return LoadAsyncAssetInfo(asset, null, ref addCount,
				delegate(bool isOk) {
					if (isOk) {
						DoPreloadAllType(asset, type);
					}

					if (onEnd != null)
						onEnd();
				}
			);
		}
		else
#endif
		if (asset.CompressType == AssetCompressType.astUnityZip)
		{
			return LoadWWWAsseetInfo(asset, null, ref addCount, 
			                  delegate (bool isOk){
				if (isOk)
				{
					DoPreloadAllType(asset, type);
				}

				if (onEnd != null)
					onEnd();
			}
			);
		} else
		{

			if (!LoadAssetInfo (asset, ref addCount))
				return false;
		
			DoPreloadAllType(asset, type);
			if (onEnd != null)
				onEnd();
		}

		return true;
	}

	// ab 中包含全部这个类型
	public bool PreloadAllType<T>(string abFileName, Action onEnd = null) where T: UnityEngine.Object
	{
		System.Type tt = typeof(T);
		return PreloadAllType(abFileName, tt, onEnd);
	}

	public AssetInfo FindAssetInfo(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		//int hashCode = Animator.StringToHash (fileName);
		AssetInfo asset;
		//if (!mAssetFileNameMap.TryGetValue(hashCode, out asset))
		if (!mAssetFileNameMap.TryGetValue(fileName, out asset))
			asset = null;

		return asset;
	}

	public override bool LoadSpritesAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.Object[]> onProcess) {
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		return LoadObjectAsync<Texture>(fileName, ResourceCacheType.rctRefAdd,
			delegate(float process, bool isDone, Texture obj) {
				if (isDone) {
					if (obj != null) {
						AssetInfo asset = FindAssetInfo(fileName);
						if (asset == null || asset.Cache == null) {
							if (onProcess != null)
								onProcess(process, isDone, null);
							return;
						}

						bool b = _LoadSpritesAsync(fileName, obj, cacheType, onProcess);
						if (!b)
						{
							ResourceMgr.Instance.DestroyObject(obj);
							if (onProcess != null)
								onProcess(process, isDone, null);
							return;
						}


						if (onProcess != null)
							onProcess(process * 0.9f, false, null);
					}
					else {
						if (onProcess != null)
							onProcess(process, isDone, null);
					}

					return;
				}

				if (onProcess != null)
					onProcess(process * 0.9f, isDone, null);
			}
		);
	}

	private bool _LoadSpritesAsync(string fileName, Texture texture, ResourceCacheType cacheType, Action<float, bool, UnityEngine.Object[]> onProcess)
	{
		if (texture == null)
			return false;
		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null || asset.Cache == null)
			return false;
		bool ret = asset.LoadSubsAsync<Sprite>(fileName, 
		   delegate (AssetBundleRequest req) {

			if (req.isDone)
			{
				ResourceMgr.Instance.DestroyObject(texture);
				if (req.allAssets != null && req.allAssets.Length > 0 && cacheType == ResourceCacheType.rctRefAdd)
				{
					AssetCacheManager.Instance.CacheAddRefCount(asset.Cache, req.allAssets.Length);
					for (int i = 0; i < req.allAssets.Length; ++i) {
						AssetCacheManager.Instance._OnLoadObject(req.allAssets[i], asset.Cache);
					}
				}
			}

			if (onProcess != null)
				onProcess (req.progress/10.0f + 0.9f, req.isDone, req.allAssets);

		  }
		);

		return ret;
	}

	private Sprite[] _LoadSprites(string fileName, ResourceCacheType cacheType) {
		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null || asset.Cache == null)
			return null;

		Sprite[] ret = asset.LoadSubs<Sprite>(fileName);

		if (ret != null && ret.Length > 0 && cacheType == ResourceCacheType.rctRefAdd) {
			AssetCacheManager.Instance.CacheAddRefCount(asset.Cache, ret.Length);
			for (int i = 0; i < ret.Length; ++i) {
				AssetCacheManager.Instance._OnLoadObject(ret[i], asset.Cache);
			}
		}

		return ret;
	}

	// 加载Sprite
	public override Sprite[] LoadSprites(string fileName, ResourceCacheType cacheType) {

#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		Texture tex = LoadObject<Texture>(fileName, ResourceCacheType.rctTemp);
		if (tex == null)
			return null;

		Sprite[] ret = _LoadSprites(fileName, cacheType);

		return ret;
	}

	public T LoadObject<T>(string fileName, ResourceCacheType cacheType) where T: UnityEngine.Object
	{
		#if USE_LOWERCHAR
		fileName = fileName.ToLower();
		#endif
		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null)
			return null;

		bool isNew = asset.Cache == null;
		int addCount = 0;
		if (!LoadAssetInfo (asset, ref addCount))
			return null;

		if (isNew) {
			// 第一次才会这样处理
			if (cacheType == ResourceCacheType.rctRefAdd)
				AddOrUpdateAssetCache (asset);
			else
				AddOrUpdateDependAssetCache (asset);
		} else {
			if (cacheType == ResourceCacheType.rctRefAdd)
			{
				// 只对自己+1处理
				AssetCacheManager.Instance.CacheAddRefCount(asset.Cache);
			}
		}

		T ret = asset.LoadObject (fileName, typeof(T)) as T;
		if (cacheType == ResourceCacheType.rctNone) {
			if (isNew)
			{
				if (asset.Cache == null)
					asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(ret, fileName);
				AssetCacheManager.Instance._AddTempAsset(asset.Cache);
				// asset.UnUsed ();
			}
		}
		else
		if (ret != null) {
			if (asset.Cache == null)
				asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(ret, fileName);
	
			if (asset.Cache != null)
				AssetCacheManager.Instance._OnLoadObject(ret, asset.Cache);
		}
		return ret;
	}

	private void AddRefAssetCache(AssetInfo asset, bool isNew, ResourceCacheType cacheType)
	{
		if (isNew) {
			// 第一次才会这样处理
			if (cacheType == ResourceCacheType.rctRefAdd)
				AddOrUpdateAssetCache (asset);
			else
				AddOrUpdateDependAssetCache (asset);
		} else {
			if (cacheType == ResourceCacheType.rctRefAdd)
			{
				// 只对自己+1处理
				AssetCacheManager.Instance.CacheAddRefCount(asset.Cache);
			}
		}
	}

	private bool DoLoadObjectAsync<T>(AssetInfo asset, string fileName, ResourceCacheType cacheType, 
	                                  Action<float, bool, T> onProcess) where T: UnityEngine.Object
	{
		// AddRefAssetCache(asset, isNew, cacheType);

		UnityEngine.Object search;
		if (asset.GetOrgResMap(fileName, out search))
		{
			bool isNew = asset.Cache == null;
			AddRefAssetCache(asset, isNew, cacheType);
			OnLoadObjectAsync(fileName, asset, isNew, search, cacheType);
			if (onProcess != null)
				onProcess (1.0f, true, search as T);
			return true;
		}

		// todo: 後面換成計數，而不是bool
		asset.IsUsing = true;
		bool ret = asset.LoadObjectAsync (fileName, typeof(T), 
		                              delegate (AssetBundleRequest req) {
			
			if (req.isDone)
			{
				asset.IsUsing = false;
				bool isNew = asset.Cache == null;
				AddRefAssetCache(asset, isNew, cacheType);
				asset.AddOrgResMap(fileName, req.asset);
				OnLoadObjectAsync(fileName, asset, isNew, req.asset, cacheType);
				if (req.asset == null && cacheType == ResourceCacheType.rctRefAdd)
					AssetCacheManager.Instance.CacheDecRefCount(asset.Cache);
			}
			
			if (onProcess != null)
				onProcess (req.progress, req.isDone, req.asset as T);
		}
		);

		if (!ret)
			asset.IsUsing = false;

		return ret;
	}

	public bool LoadObjectAsync<T>(string fileName, ResourceCacheType cacheType, Action<float, bool, T> onProcess) where T: UnityEngine.Object
	{
		#if USE_LOWERCHAR
		fileName = fileName.ToLower();
		#endif
		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null)
			return false;

		int addCount = 0;
	//	bool isNew = asset.IsNew();
#if UNITY_5_3
		if (asset.CompressType == AssetCompressType.astUnityLzo || 
			asset.CompressType == AssetCompressType.astUnityZip ||
			asset.CompressType == AssetCompressType.astNone
			) {
			return LoadAsyncAssetInfo(asset, null, ref addCount,
				delegate(bool isOk) {
					if (isOk) {
						DoLoadObjectAsync<T>(asset, fileName, cacheType, onProcess);
					}
				});
		}
		else
#endif
		if (asset.CompressType == AssetCompressType.astUnityZip)
		{
			return LoadWWWAsseetInfo(asset, null, ref addCount,
			                  delegate (bool isOk){
								  if (isOk)
							      {
									 DoLoadObjectAsync<T>(asset, fileName, cacheType, onProcess);
				}
			                 });
		} else
		{
			if (!LoadAssetInfo (asset, ref addCount))
				return false;
			return DoLoadObjectAsync<T>(asset, fileName, cacheType, onProcess);
		}
	}

	private void OnLoadObjectAsync(string fileName, AssetInfo asset, bool isNew, UnityEngine.Object obj, ResourceCacheType cacheType)
	{
		if (cacheType == ResourceCacheType.rctNone)
		{
			if (isNew && (obj != null))
			{
				if (asset.Cache == null)
					asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(obj, fileName);
				AssetCacheManager.Instance._AddTempAsset(asset.Cache);
				// asset.UnUsed ();
			}
		}
		else
		if (obj != null) {
			if (asset.Cache == null)
				asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(obj, fileName);
			
			if (asset.Cache != null)
				AssetCacheManager.Instance._OnLoadObject(obj, asset.Cache);
		}
	}

	public override AssetCache CreateCache(UnityEngine.Object orgObj, string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		AssetInfo asset = FindAssetInfo (fileName);
		if (asset == null)
			return null;
		AssetBundleCache cache = AssetBundleCache.Create(asset);
		return cache;
	}

	private string TransFileName(string fileName, string ext)
	{
		if (ext == null)
			return fileName;
#if USE_HAS_EXT
		return fileName;
#else
		return TransFileName + ext;
#endif
	}

	public override Shader LoadShader(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Shader> (TransFileName(fileName, ".shader"), cacheType);
	}

	public override bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess)
	{
		return LoadObjectAsync<Shader> (TransFileName(fileName, ".shader"), cacheType, onProcess);
	}

	public override GameObject LoadPrefab(string fileName, ResourceCacheType cacheType)
	{
#if USE_HAS_EXT
		return LoadObject<GameObject> (fileName, cacheType);
#else
		GameObject ret = LoadObject<GameObject> (TransFileName(fileName, ".prefab"), cacheType);
		if (ret == null)
			ret = LoadObject<GameObject> (TransFileName(fileName, ".fbx"), cacheType);
		return ret;
#endif
	}

	public override bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess)
	{
#if USE_HAS_EXT
		return LoadObjectAsync<GameObject> (fileName, cacheType, onProcess);
#else
		bool ret = LoadObjectAsync<GameObject> (TransFileName(fileName, ".prefab"), cacheType, onProcess);
		if (!ret)
			ret = LoadObjectAsync<GameObject> (TransFileName(fileName, ".fbx"), cacheType, onProcess);
		return ret;
#endif
	}

	public override AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<AudioClip> (TransFileName(fileName, ".audio"), cacheType);
	}

	public override bool LoadAudioClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AudioClip> onProcess)
	{
		return LoadObjectAsync<AudioClip> (TransFileName(fileName, ".audio"), cacheType, onProcess);
	}

	public override string LoadText(string fileName, ResourceCacheType cacheType)
	{
		TextAsset asset = LoadObject<TextAsset> (TransFileName(fileName, ".bytes"), cacheType);
		if (asset == null)
			return null;
		return System.Text.Encoding.UTF8.GetString (asset.bytes);
	}

	public override byte[] LoadBytes(string fileName, ResourceCacheType cacheType)
	{
		TextAsset asset = LoadObject<TextAsset> (TransFileName(fileName, ".bytes"), cacheType);
		if (asset == null)
			return null;
		return asset.bytes;
	}

	public override bool LoadTextAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, TextAsset> onProcess)
	{
		return LoadObjectAsync (TransFileName(fileName, ".bytes"), cacheType, onProcess);
	}

	public override Material LoadMaterial(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Material> (TransFileName(fileName, ".mat"), cacheType);
	}

	public override bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess)
	{
		return LoadObjectAsync<Material> (TransFileName(fileName, ".mat"), cacheType, onProcess);
	}

	public override Texture LoadTexture(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Texture> (TransFileName(fileName, ".tex"), cacheType);
	}

	public override bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess)
	{
		return LoadObjectAsync<Texture> (TransFileName(fileName, ".tex"), cacheType, onProcess);
	}

	public override RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<RuntimeAnimatorController> (TransFileName(fileName, ".controller"), cacheType);
	}

	public override bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess)
	{
		return LoadObjectAsync<RuntimeAnimatorController> (TransFileName(fileName, ".controller"), cacheType, onProcess);
	}

	public override AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<AnimationClip> (TransFileName(fileName, ".anim"), cacheType);
	}

	public override bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess)
	{
		return LoadObjectAsync<AnimationClip> (TransFileName(fileName, ".anim"), cacheType, onProcess);
	}

#if UNITY_5
	public override ShaderVariantCollection LoadShaderVarCollection(string fileName, 
	                                                                ResourceCacheType cacheType)
	{
		return LoadObject<ShaderVariantCollection> (TransFileName(fileName, ".shaderVar"), cacheType);
	}
	
	public override bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType cacheType, 
	                                                  Action<float, bool, ShaderVariantCollection> onProcess)
	{
		return LoadObjectAsync<ShaderVariantCollection> (TransFileName(fileName, ".shaderVar"), cacheType, onProcess);
	}
#endif	

#if UNITY_5_3

	internal bool LoadAsyncAssetInfo(AssetInfo asset, TaskList taskList, ref int addCount, Action<bool> onEnd = null)
	{
		if (asset == null)
			return false;

		if (asset.IsVaild ())
		{
			if (onEnd != null)
				onEnd(true);
			return true;
		}

		if (asset.IsUsing)
		{
			if (taskList == null) {
				var task = asset.AsyncTask;
				if (task != null) {
					taskList = asset.CreateTaskList(onEnd);
					taskList.AddTask(task, false);
				}
			}
			else
				asset.AddNoOwnerToTaskList(taskList, asset.AsyncTask);
			return true;
		}

		if (taskList == null)
			taskList = asset.CreateTaskList(onEnd);

		asset.IsUsing = true;
		for (int i = 0; i < asset.DependFileCount; ++i)
		{
			string fileName = asset.GetDependFileName(i);
			if (!string.IsNullOrEmpty(fileName))
			{
				AssetInfo depend = FindAssetInfo(fileName);
				if (depend == null)
				{
					continue;
				}
				if (!LoadAsyncAssetInfo(depend, taskList, ref addCount))
				{
					asset.IsUsing = false;
					return false;
				}
			}
		}

		addCount += 1;
		AssetCacheManager.Instance._CheckAssetBundleCount (addCount);

		bool ret = asset.LoadAsync(taskList);
		return ret;
	}

#endif

	internal bool LoadWWWAsseetInfo(AssetInfo asset, TaskList taskList, ref int addCount, Action<bool> onEnd = null)
	{
		if (asset == null)
			return false;

		if (asset.IsVaild ())
		{
			if (onEnd != null)
				onEnd(true);
			return true;
		}


		if (asset.IsUsing)
		{
			if (taskList == null)
			{
				var task = asset.WWWTask;
				if (task != null)
				{
					taskList = asset.CreateTaskList(onEnd);
					taskList.AddTask(task, false);
				}
			}
			else {
				asset.AddNoOwnerToTaskList(taskList, asset.WWWTask);
			}
			return true;
		}

		if (taskList == null)
			taskList = asset.CreateTaskList(onEnd);

		asset.IsUsing = true;
		for (int i = 0; i < asset.DependFileCount; ++i)
		{
			string fileName = asset.GetDependFileName(i);
			if (!string.IsNullOrEmpty(fileName))
			{
				AssetInfo depend = FindAssetInfo(fileName);
				if (depend == null)
				{
					#if USE_UNITY5_X_BUILD
					continue;
					#else
					asset.IsUsing = false;
					return false;
					#endif
				}
				if (!LoadWWWAsseetInfo(depend, taskList, ref addCount))
				{
					asset.IsUsing = false;
					return false;
				}
			}
		}

		addCount += 1;
		AssetCacheManager.Instance._CheckAssetBundleCount (addCount);

	//	asset.IsUsing = false;
		bool ret = asset.LoadWWW(taskList);
		return ret;
	}

	internal bool LoadAssetInfo(AssetInfo asset, ref int addCount)
	{
		if (asset == null)
			return false;

		if (asset.IsVaild () || asset.IsUsing)
			return true;

		asset.IsUsing = true;
		// 首先先加载依赖AssetInfo
		for (int i = 0; i < asset.DependFileCount; ++i) {
			string fileName = asset.GetDependFileName(i);
			if (!string.IsNullOrEmpty(fileName))
			{
				AssetInfo depend = FindAssetInfo(fileName);
				if (depend == null)
				{
					#if USE_UNITY5_X_BUILD
					continue;
					#else
					asset.IsUsing = false;
					return false;
					#endif
				}
				if (!LoadAssetInfo(depend, ref addCount))
				{
					asset.IsUsing = false;
					return false;
				}
			}
		}

		addCount += 1;
		AssetCacheManager.Instance._CheckAssetBundleCount (addCount);

		asset.IsUsing = false;
		bool ret = asset.Load ();
		return ret;
	}

	private void AddOrUpdateAssetCache(AssetInfo asset)
	{
		if (asset == null)
			return;
		AddOrUpdateDependAssetCache (asset);
		if (asset.Cache == null)
			asset.Cache = AssetBundleCache.Create(asset);
		AssetCacheManager.Instance._AddOrUpdateUsedList (asset.Cache);
	}

	// DependAssetCache
	private void AddOrUpdateDependAssetCache(AssetInfo asset)
	{
		if (asset == null)
			return;

		if ((!asset.IsVaild ()) || asset.IsUsing)
			return;

		asset.IsUsing = true;

		for (int i = 0; i < asset.DependFileCount; ++i) {
			string fileName = asset.GetDependFileName(i);
			if (!string.IsNullOrEmpty(fileName))
			{
				AssetInfo depend = FindAssetInfo(fileName);
				if ((depend != null) && (!depend.IsUsing))
				{
					if (depend.Cache == null)
					{
						depend.Cache = AssetBundleCache.Create(depend);
						AddOrUpdateDependAssetCache(depend);	
					}
					int refCount = asset.GetDependFileRef(i);
					AssetCacheManager.Instance._AddOrUpdateUsedList (depend.Cache, refCount);
				}
			}
		}

		asset.IsUsing = false;
	}

	private string GetCheckFileName(string fileName, bool isWWW, bool isUseABCreateFromFile)
	{
		if (string.IsNullOrEmpty(fileName))
			return string.Empty;

		string ret = GetCheckWritePathFileName(fileName, isWWW);
		if (!string.IsNullOrEmpty(ret))
			return ret;

		ret = string.Format("{0}/{1}", WWWFileLoadTask.GetStreamingAssetsPath(true, isUseABCreateFromFile), fileName);

		if (isWWW)
		{
			ret = WWWFileLoadTask.ConvertToWWWFileName(ret);
		}

		return ret;
	}

	private string GetCheckWritePathFileName(string fileName, bool isWWW)
	{
		string ret = string.Empty;
		if (string.IsNullOrEmpty(fileName))
			return ret;
		string writePath = Utils.FilePathMgr.Instance.WritePath;
		if (!string.IsNullOrEmpty(writePath))
		{
			string realFileName = string.Format("{0}/{1}", writePath, fileName);
			if (File.Exists(realFileName))
			{
				ret = realFileName;
				if (isWWW)
					ret = "file:///" + ret;
			}
		}
		return ret;
	}

	private string GetXmlFileName()
	{
		string ret = GetCheckFileName("AssetBundles.xml", true, false);
		return ret;
		/*
		string ret = GetCheckWritePathFileName("AssetBundles.xml", isWWW);
		if (!string.IsNullOrEmpty(ret))
			return ret;

		switch (Application.platform) {
		case RuntimePlatform.OSXEditor:
			ret = Path.GetFullPath("Assets/StreamingAssets/Mac");
			if (isWWW)
				ret = "file:///" + ret;
			break;
		case RuntimePlatform.WindowsEditor:
			ret = Path.GetFullPath("Assets/StreamingAssets/Windows");
			if (isWWW)
				ret = "file:///" + ret;
			break;
		case RuntimePlatform.OSXPlayer:
			ret = Application.streamingAssetsPath + "/Mac";
			if (isWWW)
				ret = "file:///" + ret;
			break;
		case RuntimePlatform.WindowsPlayer:
			ret = Application.streamingAssetsPath + "/Windows";
			if (isWWW)
				ret = "file:///" + ret;
			break;
		case RuntimePlatform.Android:
			if (!isWWW)
			{
				//ret = "file:///" + Application.streamingAssetsPath + "/Android";
				//ret = Application.streamingAssetsPath + "/Android";
				// 处理下CreateFromFile的路径
				//ret = ret.Replace("jar:file:", "").Replace(".apk!/", ".apk!");
				ret = Application.dataPath + "!assets/" + "Android";
			} else
			{
				ret = Application.streamingAssetsPath + "/Android";
			}
			break;
		case RuntimePlatform.IPhonePlayer:
			ret = Application.streamingAssetsPath + "/IOS";
			break;
		default:
			ret = Application.streamingAssetsPath;
			break;
		}

		ret += "/AssetBundles.xml";
		return ret;*/
	}

	/*
	private static string _cBundleRootPath = "Assets/";
	private string GetBundlePath(string path)
	{
		if (string.IsNullOrEmpty (path))
			return string.Empty;
		int idx = path.IndexOf (_cBundleRootPath);
		if (idx < 0)
			return path;
		string ret = path.Substring (idx + _cBundleRootPath.Length);
		return ret;
	}*/

	/*
	public void LoadXml()
	{
		string fileName = GetXmlFileName (true);
		if (string.IsNullOrEmpty (fileName)) {
			return;
		}

		WWW www = new WWW (fileName);

		if (www == null) {
			return;
		}

		if (!www.isDone) {
			return;
		}

		byte[] bytes = www.bytes;
		www.Dispose ();
		www = null;

		if ((bytes == null) || (bytes.Length <= 0)) {
			return;
		}

		fileName = GetXmlFileName (false);

		string str = System.Text.Encoding.UTF8.GetString (bytes);
		XMLParser parser = new XMLParser ();
		XMLNode rootNode = parser.Parse (str);
		if (rootNode == null) {
			return;
		}

		XMLNodeList assetBundles = rootNode.GetNodeList("AssetBundles");
		if ((assetBundles == null) || (assetBundles.Count <= 0)) {
			return;
		}

		rootNode = assetBundles [0] as XMLNode;
		if (rootNode == null) {
			return;
		}

		XMLNodeList nodeList = rootNode.GetNodeList ("AssetBundle");
		if (nodeList == null) {
			return;
		}

		 string assetFilePath = Path.GetDirectoryName (fileName);
		//string assetFilePath = GetBundlePath(Path.GetDirectoryName (fileName));

		// AssetBundle
		for (int i = 0; i < nodeList.Count; ++i) {
			XMLNode node = nodeList[i] as XMLNode;
			if (node == null)
				continue;

			XMLNodeList subFiles = node.GetNodeList("SubFiles");
			if ((subFiles == null) || (subFiles.Count <= 0))
				continue;

			XMLNode subFilesNode = subFiles[0] as XMLNode;
			if (subFilesNode == null)
				continue;
			subFiles = subFilesNode.GetNodeList("SubFile");
			if ((subFiles == null) || (subFiles.Count <= 0))
				continue;

			// 1.Attribe
			string localFileName = node.GetValue("@fileName");
			string assetBundleFileName = string.Format("{0}/{1}", assetFilePath, localFileName);
			string compressTypeStr = node.GetValue("@compressType");
			AssetCompressType compressType = AssetCompressType.astNone;
			if (!string.IsNullOrEmpty(compressTypeStr))
			{
				int compressValue;
				if (int.TryParse(compressTypeStr, out compressValue))
					compressType = (AssetCompressType)compressValue;
			}
			AssetInfo asset = new AssetInfo(assetBundleFileName);
			asset._SetCompressType(compressType);
			// 额外添加一个文件名的映射
			AddFileAssetMap(assetBundleFileName, asset);
			// 
			// 2.SubFiles
			for (int j = 0; j < subFiles.Count; ++j)
			{
				XMLNode subFile = subFiles[j] as XMLNode;
				if (subFile == null)
					continue;
				string subFileName = subFile.GetValue("@fileName");
				if (string.IsNullOrEmpty(subFileName))
					continue;

				int hashCode;
				string hashCodeStr = subFile.GetValue("@hashCode");
				if (string.IsNullOrEmpty(hashCodeStr))
					hashCode = Animator.StringToHash(subFileName);
				else
				{
					if (!int.TryParse(hashCodeStr, out hashCode))
						hashCode = Animator.StringToHash(subFileName);
				}

				asset._AddSubFile(hashCode);
				AddFileAssetMap(subFileName, asset);
			}
			// 3.DependFiles
			XMLNodeList dependFiles = node.GetNodeList("DependFiles");
			if ((dependFiles != null) && (dependFiles.Count > 0))
			{
				XMLNode depnedFilesNode = dependFiles[0] as XMLNode;
				if (depnedFilesNode != null)
				{
					dependFiles = depnedFilesNode.GetNodeList("DependFile");
					if (dependFiles != null)
					{
						for (int j = 0; j < dependFiles.Count; ++j)
						{
							XMLNode dependFile = dependFiles[j] as XMLNode;
							if (dependFile == null)
							{
								return;
							}
							string dependFileName = string.Format("{0}/{1}", assetFilePath, dependFile.GetValue("@fileName"));
							asset._AddDependFile(dependFileName);
						}
					}
				}
	
			}
		}

		GC.Collect ();
	}*/

	public AssetLoader()
	{
		/*
		string fileName = GetXmlFileName (true);
		if (string.IsNullOrEmpty (fileName))
			return;
		mXmlLoaderTask = new WWWFileLoadTask (fileName);
		if (mXmlLoaderTask.IsDoing) {
			// 创建时钟
			mLoaderTimer = TimerMgr.Instance.CreateTimer(false, 0, false);
			mLoaderTimer.AddListener(OnLoaderTimer);
		}*/
	}

	private void LoadXml(byte[] bytes)
	{
		if ((bytes == null) || (bytes.Length <= 0)) {
			return;
		}

	//	string fileName = GetXmlFileName (false);
		
		string str = System.Text.Encoding.UTF8.GetString (bytes);
		XMLParser parser = new XMLParser ();
		XMLNode rootNode = parser.Parse (str);
		if (rootNode == null) {
			return;
		}
		
		XMLNodeList assetBundles = rootNode.GetNodeList("AssetBundles");
		if ((assetBundles == null) || (assetBundles.Count <= 0)) {
			return;
		}
		
		rootNode = assetBundles [0] as XMLNode;
		if (rootNode == null) {
			return;
		}
		
		XMLNodeList nodeList = rootNode.GetNodeList ("AssetBundle");
		if (nodeList == null) {
			return;
		}
		
		// string assetFilePath = Path.GetDirectoryName (fileName);

		//string assetFilePath = GetBundlePath(Path.GetDirectoryName (fileName));
		//string assetFilePath = WWWFileLoadTask.GetStreamingAssetsPath(true);

		// AssetBundle
		for (int i = 0; i < nodeList.Count; ++i) {
			XMLNode node = nodeList[i] as XMLNode;
			if (node == null)
				continue;
			
			XMLNodeList subFiles = node.GetNodeList("SubFiles");
			if ((subFiles == null) || (subFiles.Count <= 0))
				continue;
			
			XMLNode subFilesNode = subFiles[0] as XMLNode;
			if (subFilesNode == null)
				continue;
			subFiles = subFilesNode.GetNodeList("SubFile");
			if ((subFiles == null) || (subFiles.Count <= 0))
				continue;
			
			// 1.Attribe
			string localFileName = node.GetValue("@fileName");
		
			//string assetBundleFileName = string.Format("{0}/{1}", assetFilePath, localFileName);

			string compressTypeStr = node.GetValue("@compressType");
			AssetCompressType compressType = AssetCompressType.astNone;
			if (!string.IsNullOrEmpty(compressTypeStr))
			{
				int compressValue;
				if (int.TryParse(compressTypeStr, out compressValue))
					compressType = (AssetCompressType)compressValue;
			}

			bool isUseCreateFromFile = compressType == AssetCompressType.astNone ||
										compressType == AssetCompressType.astUnityLzo;
			string assetBundleFileName = GetCheckFileName(localFileName, false, isUseCreateFromFile);

			AssetInfo asset;
			if (!mAssetFileNameMap.TryGetValue(assetBundleFileName, out asset)) {
				asset = new AssetInfo(assetBundleFileName);
				asset._SetCompressType(compressType);
				// 额外添加一个文件名的映射
				AddFileAssetMap(assetBundleFileName, asset);
			}
			else {
				;
			}
			// 
			// 2.SubFiles
			for (int j = 0; j < subFiles.Count; ++j)
			{
				XMLNode subFile = subFiles[j] as XMLNode;
				if (subFile == null)
					continue;
				string subFileName = subFile.GetValue("@fileName");
				if (string.IsNullOrEmpty(subFileName))
					continue;

				/*
				int hashCode;
				string hashCodeStr = subFile.GetValue("@hashCode");
				if (string.IsNullOrEmpty(hashCodeStr))
					hashCode = Animator.StringToHash(subFileName);
				else
				{
					if (!int.TryParse(hashCodeStr, out hashCode))
						hashCode = Animator.StringToHash(subFileName);
				}
				
				asset._AddSubFile(hashCode);*/
				asset._AddSubFile(subFileName);
				AddFileAssetMap(subFileName, asset);
			}
			// 3.DependFiles
			XMLNodeList dependFiles = node.GetNodeList("DependFiles");
			if ((dependFiles != null) && (dependFiles.Count > 0))
			{
				XMLNode depnedFilesNode = dependFiles[0] as XMLNode;
				if (depnedFilesNode != null)
				{
					dependFiles = depnedFilesNode.GetNodeList("DependFile");
					if (dependFiles != null)
					{
						for (int j = 0; j < dependFiles.Count; ++j)
						{
							XMLNode dependFile = dependFiles[j] as XMLNode;
							if (dependFile == null)
							{
								return;
							}

							//string dependFileName = string.Format("{0}/{1}", assetFilePath, dependFile.GetValue("@fileName"));
							string localDependFileName = dependFile.GetValue("@fileName");
							string dependFileName = GetCheckFileName(localDependFileName, false, isUseCreateFromFile);

							string refStr = dependFile.GetValue("@refCount");
							int refCount = 1;
							if (!string.IsNullOrEmpty(refStr))
							{
								if (!int.TryParse(refStr, out refCount))
									refCount = 1;
							}
							asset._AddDependFile(dependFileName, refCount);
						}
					}
				}
				
			}
		}
		
		GC.Collect ();
	}

	private void OnLoaderTimer(Timer time, float deltTime)
	{
		if (mXmlLoaderTask.IsDoing) {
			mXmlLoaderTask.Process ();
			return;
		}

		if (mXmlLoaderTask.IsFail) {
			if (mConfigLoaderEvent != null)
				mConfigLoaderEvent (false);
		} else
		if (mXmlLoaderTask.IsOk) {
			LoadXml(mXmlLoaderTask.ByteData);
			if (mConfigLoaderEvent != null)
				mConfigLoaderEvent (true);
		}

		// 删除
		mXmlLoaderTask.Release ();
		mXmlLoaderTask = null;
		mConfigLoaderEvent = null;
		mLoaderTimer.Dispose ();
		mLoaderTimer = null;
	}

	// 手动调用读取配置
	public void LoadConfigs(Action<bool> OnFinishEvent)
	{
		mConfigLoaderEvent = OnFinishEvent;
		if (mXmlLoaderTask == null) {
			// 已经在读取状态了不会再调用
			string fileName = GetXmlFileName ();
			if (string.IsNullOrEmpty (fileName))
				return;
			mXmlLoaderTask = new WWWFileLoadTask (fileName);
			if (mXmlLoaderTask.IsDoing) {
				// 创建时钟
				if (mLoaderTimer == null)
				{
					mLoaderTimer = TimerMgr.Instance.CreateTimer (false, 0, true);
					mLoaderTimer.AddListener (OnLoaderTimer);
				}
			}
		}

	}

	public bool _ExistsFileName(string localFileName)
	{
		string fileName;
		if (!localFileName.StartsWith ("Assets"))
			fileName = Path.GetFullPath("Assets/" + localFileName);
		else
			fileName = Path.GetFullPath(localFileName);

		return mAssetFileNameMap.ContainsKey (fileName);
	}

	private void AddFileAssetMap(string fileName, AssetInfo asset)
	{
		if (string.IsNullOrEmpty (fileName) || (asset == null))
			return;

		AssetInfo find;
		if (mAssetFileNameMap.TryGetValue (fileName, out find)) {
			if (find == null)
				mAssetFileNameMap[fileName] = asset;
			else
			{
				if (find != asset)
				{
					string err = string.Format("[AssetBundle: {0}] not compare!!!", fileName);
					LogMgr.Instance.LogError(err);
				}

				return;
			}
		}
		mAssetFileNameMap.Add (fileName, asset);
	}

	// FileHash AssetBundle FileName
	// private Dictionary<int, AssetInfo> mAssetFileNameMap = new Dictionary<int, AssetInfo>();
	private Dictionary<string, AssetInfo> mAssetFileNameMap = new Dictionary<string, AssetInfo>();
	private WWWFileLoadTask mXmlLoaderTask = null;
	private Timer mLoaderTimer = null;
	// 读取配置事件
	private Action<bool> mConfigLoaderEvent = null;
}

internal abstract class AssetInfoBaseTask: ITask
{
	public AssetInfoBaseTask(AssetInfo asset)
	{
		Asset = asset;
	}

	public AssetInfo Asset
	{
		get;
		protected set;
	}

	public bool IsMustDone
	{
		get
		{
			return mIsMustDone;
		}

		set
		{
			mIsMustDone = value;
		}
	}

	private bool mIsMustDone = false;
}

internal class AssetLoadTask: AssetInfoBaseTask
{
	public AssetLoadTask(AssetInfo asset): base(asset)
	{

	}

	public override void Process()
	{
	}
}

internal class AssetInfoTaskMgr
{

	public void AddAssetObjLoadAsyncTask<T> (string fileName, ResourceCacheType cacheType, Action<float, bool, T> onProcess) where T: UnityEngine.Object
	{
	}

	public void AddAssetObjLoadTask<T> (string fileName, ResourceCacheType cacheType, Action<T> onLoaded) where T: UnityEngine.Object
	{

	}

	public void ClearTasks(AssetInfo asset)
	{
		if (asset == null)
			return;
		var listNode = FindTaskListNode (asset);
		if (listNode == null)
			return;

		if (listNode.Value != null) {
			var node = listNode.Value.First;
			while (node != null)
			{
				var next = node.Next;
				if (node.Value != null)
					node.Value.Release();
				node = next;
			}

			listNode.Value.Clear();
		}

		mTaskMap.Remove (asset);
		mTaskList.Remove (listNode);
	}

	private bool Process(LinkedListNode<LinkedList<AssetInfoBaseTask>> node)
	{
		if (node.Value == null)
			return true;

		var n = node.Value.First;

		while (n != null) {
			var next = n.Next;

			if (n.Value != null)
			{
				if (n.Value.IsDoing)
					n.Value.Process();

				if (n.Value.IsDone)
					node.Value.Remove(n);
			} else
			{
				node.Value.Remove(n);
			}

			n = next;
		}

		bool ret = node.Value.Count <= 0;
		return ret;
	}

	public void Process()
	{
		var node = mTaskList.First;
		while (node != null) {
			var next = node.Next;
			AssetInfo asset = null;
			if ((node.Value != null) && (node.Value.First != null) && (node.Value.First.Value != null))
			{
				asset = node.Value.First.Value.Asset;
			}
			if (Process(node))
			{
				if (asset != null)
					mTaskMap.Remove(asset);
				mTaskList.Remove(node);
			}
			node = next;
		}
	}

	private LinkedList<AssetInfoBaseTask> FindTaskList(AssetInfo asset)
	{
		LinkedListNode<LinkedList<AssetInfoBaseTask>> ret = FindTaskListNode (asset);
		if (ret == null)
			return null;
		return ret.Value;
	}

	private LinkedListNode<LinkedList<AssetInfoBaseTask>> FindTaskListNode(AssetInfo asset)
	{
		if (asset == null)
			return null;
		LinkedListNode<LinkedList<AssetInfoBaseTask>> ret;
		if (!mTaskMap.TryGetValue (asset, out ret))
			return null;
		return ret;
	}

	private LinkedList<AssetInfoBaseTask> FindOrCreateTaskList(AssetInfo asset)
	{
		if (asset == null)
			return null;
		LinkedList<AssetInfoBaseTask> ret = FindTaskList (asset);
		if (ret != null)
			return ret;
		LinkedListNode<LinkedList<AssetInfoBaseTask>> node = new LinkedListNode<LinkedList<AssetInfoBaseTask>> (new LinkedList<AssetInfoBaseTask> ());
		mTaskMap.Add (asset, node);
		return node.Value;
	}

	private Dictionary<AssetInfo, LinkedListNode<LinkedList<AssetInfoBaseTask>>> mTaskMap = new Dictionary<AssetInfo, LinkedListNode<LinkedList<AssetInfoBaseTask>>>();
	private LinkedList<LinkedList<AssetInfoBaseTask>> mTaskList = new LinkedList<LinkedList<AssetInfoBaseTask>>();
}
