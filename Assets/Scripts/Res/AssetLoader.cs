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
#define USE_DEP_BINARY
#define USE_DEP_BINARY_AB
#define USE_DEP_BINARY_HEAD
#define USE_ABFILE_ASYNC
// 是否使用LoadFromFile读取压缩AB
#define USE_LOADFROMFILECOMPRESS
//#define USE_WWWCACHE

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using XmlParser;
using Utils;

public class AsyncLoadKeyComparser: StructComparser<AsyncLoadKey>
{}

public struct AsyncLoadKey: IEquatable<AsyncLoadKey>
{
	public string fileName;
	public System.Type type;

	public bool Equals(AsyncLoadKey other) {
		return this == other;
	}

	public override bool Equals(object obj) {
		if (obj == null)
			return false;

		if (GetType() != obj.GetType())
			return false;

		if (obj is AsyncLoadKey) {
			AsyncLoadKey other = (AsyncLoadKey)obj;
			return Equals(other);
		}
		else
			return false;

	}

	public override int GetHashCode() {
		int ret = FilePathMgr.InitHashValue();
		FilePathMgr.HashCode(ref ret, fileName);
		FilePathMgr.HashCode(ref ret, type);
		return ret;
	}

	public static bool operator ==(AsyncLoadKey a, AsyncLoadKey b) {
		return (a.type == b.type) && (string.Compare(a.fileName, b.fileName) == 0);
	}

	public static bool operator !=(AsyncLoadKey a, AsyncLoadKey b) {
		return !(a == b);
	}
}

public class AssetBundleCache: AssetCache
{
	public AssetBundleCache(AssetInfo target)
	{
		mTarget = target;
		IsloadDecDepend = true;
	}

	public AssetBundleCache()
	{
		IsloadDecDepend = true;
	}

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
			mTarget.UnLoad (IsloadDecDepend);
			mTarget = null;
		}

		if (m_PoolUsed) {
			InPool (this);
		} else
			ClearLinkListNode ();
	}

	protected override void OnUnUsed()
	{
		if (mTarget != null)
		{
			mTarget.UnUsed(IsloadDecDepend);
			mTarget = null;
		}

		if (m_PoolUsed) {
			InPool (this);
		} else
			ClearLinkListNode ();
	}

    protected override void OnUnloadAsset(UnityEngine.Object asset)
    {
        if (mTarget != null)
        {
            mTarget.OnUnloadAsset(asset);
        }
    }

	internal bool IsloadDecDepend
	{
		get;
		set;
	}

    public AssetInfo Target {
		get
		{
			return mTarget;
		}
	}

	public static int GetPoolCount()
	{
		return m_Pool.Count;
	}

	private static AssetBundleCache GetPool(AssetInfo target)
	{
		InitPool();
		AssetBundleCache ret = m_Pool.GetObject();
		ret.mTarget = target;
		ret.IsloadDecDepend = true;
		return ret;
	}

	private static void InPool(AssetBundleCache cache)
	{
		if (cache == null)
			return;
		InitPool();
		cache.Reset();
		m_Pool.Store(cache);
	}

	private void Reset()
	{
		mTarget = null;
		IsloadDecDepend = true;
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
	internal bool IsUsing {
		get {
			return m_UsingCnt > 0;
		}
	}

	internal void AddUsingCnt()
	{
		++m_UsingCnt;
	}

	internal void ClearUsingCnt()
	{
		m_UsingCnt = 0;
	}

	internal void DecUsingCnt()
	{
		--m_UsingCnt;
		if (m_UsingCnt < 0)
			m_UsingCnt = 0;
	}

    internal bool IsLocalUsing {
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

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
	internal BundleCreateAsyncTask AsyncTask
	{
		get
		{
			return m_AsyncTask;
		}
	}
#endif
	
	private WWWFileLoadTask m_WWWTask = null;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
    private BundleCreateAsyncTask m_AsyncTask = null;
#endif
	private TaskList m_TaskList = null;
	private ITimer m_Timer = null;
	private Action<bool> m_EndEvt = null;
	private int m_UsingCnt = 0;
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

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
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

	private bool DoCheckTaskVaild(ITask task)
	{
		if (task == null)
			return false;
		return task.UserData != null;
	}

	private void OnTimerEvt(Timer obj, float timer)
	{
		if (m_TaskList != null)
		{
			//m_TaskList.Process(DoCheckTaskVaild);
			m_TaskList.ProcessDoneContinue(DoCheckTaskVaild);
			if (m_TaskList != null && m_TaskList.IsEmpty)
			{
				try
				{
					if (m_EndEvt != null)
						m_EndEvt(true);
				} finally
				{
					ClearTaskData();
				}
			}
		}
	}

	internal void OnTaskResult(ITask task)
	{
		if (task == null)
			return;
		if (task.IsDone)
		{
            AssetInfo assetInfo = task.UserData as AssetInfo;
            bool isFail = task.IsFail && !assetInfo.IsVaild();
            if (isFail)
			{
				try
				{
					if (m_EndEvt != null)
						m_EndEvt(false);
				} finally
				{
					ClearTaskData();
				}
			} else
			{
				if (m_TaskList != null)
					m_TaskList.RemoveTask(task);
			}
		}
	}

	public string GetOrgAssetFileName(UnityEngine.Object orgAsset)
	{
		if (orgAsset == null)
			return string.Empty;
		string ret = string.Empty;
		var iter = m_OrgResMap.GetEnumerator();
		while (iter.MoveNext ()) {
			if (iter.Current.Value == orgAsset) {
				ret = iter.Current.Key;
				break;
			}
		}
		iter.Dispose ();
		return ret;
	}

	public string GetOrgAssetFileName(int orgId)
	{
		string ret = string.Empty;
		var iter = m_OrgResMap.GetEnumerator();
		while (iter.MoveNext ()) {
			if (iter.Current.Value.GetInstanceID() == orgId) {
				ret = iter.Current.Key;
				break;
			}
		}
		iter.Dispose ();
		return ret;
	}

	public bool ContainsOrgAsset(int orgId)
	{
		if (m_OrgResMap == null)
			return false;
		bool ret = false;
		var iter = m_OrgResMap.GetEnumerator();
		while (iter.MoveNext ()) {
			if (iter.Current.Value.GetInstanceID() == orgId) {
				ret = true;
				break;
			}
		}
		iter.Dispose ();
		return ret;
	}

	public bool ContainsOrgAsset(UnityEngine.Object orgAsset)
	{
		if (m_OrgResMap == null || orgAsset == null)
			return false;
		bool ret = false;
		var iter = m_OrgResMap.GetEnumerator();
		while (iter.MoveNext ()) {
			if (iter.Current.Value == orgAsset) {
				ret = true;
				break;
			}
		}
		iter.Dispose ();
		return ret;
	}

	public UnityEngine.Object GetOrgAsset(string fileName)
	{
		UnityEngine.Object ret = null;
		if (!GetOrgResMap(fileName, out ret))
			return null;
		return ret;
	}

    internal void OnUnloadAsset(UnityEngine.Object asset)
    {
        if (m_OrgResMap != null)
        {
            var iter = m_OrgResMap.GetEnumerator();
            while (iter.MoveNext())
            {
                if (iter.Current.Value == asset)
                {
                    m_OrgResMap.Remove(iter.Current.Key);
                    break;
                }
            }
            iter.Dispose();
        }

        UnityEngine.Object target = null;
        if (asset is Sprite)
            target = ((Sprite)asset).texture;

        AssetCacheManager.Instance._RemoveOrgObj(asset, true);
        if (target != null)
            OnUnloadAsset(target);
    }

	public TaskList CreateTaskList(Action<bool> onEnd)
	{
		if (m_TaskList == null)
		{
			m_TaskList = new TaskList();
			m_TaskList.UserData = this;
			if (m_Timer == null)
			{
				m_Timer = TimerMgr.Instance.CreateTimer(0, true);
				m_Timer.AddListener(OnTimerEvt);
			}
		}

		m_EndEvt += onEnd;
		return m_TaskList;
	}

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
    private static void OnLocalAsyncResult(ITask task)
	{
		BundleCreateAsyncTask asycTask = task as BundleCreateAsyncTask;
		if (asycTask == null)
			return;
		AssetInfo info = asycTask.UserData as AssetInfo;
		if (info == null)
			return;
		if (asycTask.IsDone)
		{
			if (asycTask.IsOk)
				info.mBundle = asycTask.Bundle;

			if (info.m_AsyncTask != null) {
				info.m_AsyncTask.Release ();
				info.m_AsyncTask = null;
			}
		}

		//info.IsUsing = false;
		info.DecUsingCnt();
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
		if (wwwTask.IsDone)
		{
			if (wwwTask.IsOk)
				info.mBundle = wwwTask.Bundle;
			if (info.m_WWWTask != null) {
				info.m_WWWTask.Release ();
				info.m_WWWTask = null;
			}
		}

		//info.IsUsing = false;
		info.DecUsingCnt();
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

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5

    // 5.3新的异步加载方法
    public bool LoadAsync(TaskList taskList, int priority)
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

		m_AsyncTask = BundleCreateAsyncTask.Create(mFileName, priority);
		if (m_AsyncTask != null)
		{
			// 优化AB加载
			m_AsyncTask.StartLoad();
			m_AsyncTask.UserData = this;
			taskList.AddTask(m_AsyncTask, true);
			if (taskList.UserData != null)
			{
				AssetInfo parent = taskList.UserData as AssetInfo;
				if (parent != null)
				{
					m_AsyncTask.AddResultEvent(parent.OnTaskResult);
				}
			}

            m_AsyncTask.AddResultEvent(OnLocalAsyncResult);
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
#if USE_WWWCACHE
            m_WWWTask.IsUsedCached = true;
#endif
            m_WWWTask.UserData = this;
			taskList.AddTask(m_WWWTask, true);
			if (taskList.UserData != null)
			{
				AssetInfo parent = taskList.UserData as AssetInfo;
				if (parent != null)
				{
					m_WWWTask.AddResultEvent(parent.OnTaskResult);
				}
			}
			
			m_WWWTask.AddResultEvent(OnLocalWWWResult);
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
            //	ClearTaskData();
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            mBundle = AssetBundle.LoadFromFile (mFileName);
#else
			mBundle = AssetBundle.CreateFromFile(mFileName);
#endif
			if (mBundle == null)
				return false;
		} else 
		if (mCompressType == AssetCompressType.astUnityLzo

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            || mCompressType == AssetCompressType.astUnityZip
#endif
			) {
            // Lz4 new compressType
            //	ClearTaskData();
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
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
		if (string.IsNullOrEmpty (fileName))
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

		if (!IsVaild ())
			return null;

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
		if (!m_OrgResMap.TryGetValue(fileName, out obj))
			return false;
		return obj != null;
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

	internal bool LoadSubsAsync<T>(string fileName, int priority, Action<AssetBundleRequest> onProcess) where T: UnityEngine.Object {
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

		request.priority = priority;
		return AddAsyncOperation(fileName, objType, request, onProcess);
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

	public bool LoadObjectAsync(string fileName, Type objType, int priority, Action<AssetBundleRequest> onProcess)
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

		request.priority = priority;

		return AddAsyncOperation(fileName, objType, request, onProcess);
	}

	public bool IsVaild()
	{
		return (mBundle != null);
	}

	public bool HasChildFiles()
	{
		return (mChildFileNameHashs != null) && (mChildFileNameHashs.Count > 0);
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

	public void _BundleUnLoadFalse()
	{
		if (IsVaild ()) {
			mBundle.Unload(false);
			mBundle = null;
		}
	}

	public void UnLoad(bool isDecDepend = true)
	{
#if UNITY_EDITOR
		if (IsUsing)
			Debug.LogErrorFormat("{0} is using but unload!", mFileName);
#endif
	//	if (/*IsVaild() &&*/ !IsUsing) {
			bool isVaild = IsVaild ();
			m_OrgResMap.Clear();
			// LogMgr.Instance.Log(string.Format("Bundle unload=>{0}", Path.GetFileNameWithoutExtension(mFileName)));
			m_AsyncLoadDict.Clear ();
			ClearUsingCnt ();
			if (isVaild) {
				mBundle.Unload (true);
				mBundle = null;
			}
			ClearTaskData();
			mCache = null;

			if (isVaild) {
				if (isDecDepend)
					DecDependInfo ();
			}
	//	}
	}

	public void UnUsed(bool isDecDepend = true)
	{
		m_OrgResMap.Clear();
		bool isVaid = IsVaild();
	//	if (IsVaild() /*&& !IsUsing*/) {
			mCache = null;
			_BundleUnLoadFalse ();
			m_AsyncLoadDict.Clear();
			ClearUsingCnt ();
			ClearTaskData();
		// 处理依赖关系
		if (isVaid) {
			if (isDecDepend)
				DecDependInfo ();
		}
	//	}
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

		asset.IsLocalUsing = true;

		AssetLoader loader = Loader;
		for (int i = 0; i < asset.DependFileCount; ++i) {
			string fileName = asset.GetDependFileName(i);
			AssetInfo dependInfo = loader.FindAssetInfo(fileName);
			if ((dependInfo != null) && (!dependInfo.IsLocalUsing))
			{
				if (dependInfo.Cache != null)
				{
					int refCount = asset.GetDependFileRef(i);
					AssetCacheManager.Instance.CacheDecRefCount(dependInfo.Cache, refCount);
				}
				//RemoveDepend(dependInfo);
			}
		}

		asset.IsLocalUsing = false;
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

	public HashSet<string>.Enumerator GetSubFilesIter()
	{
		if (mChildFileNameHashs == null)
			return new HashSet<string>.Enumerator ();
		return mChildFileNameHashs.GetEnumerator ();
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
	private Dictionary<AsyncLoadKey, AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey>> m_AsyncLoadDict = new Dictionary<AsyncLoadKey, AsyncOperationMgr.AsyncOperationItem<AssetBundleRequest, AsyncLoadKey>>(AsyncLoadKeyComparser.Default);
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
		if (!LoadAssetInfo (asset, ref addCount, sceneName))
			return false;
		
		AddOrUpdateAssetCache (asset);

		return true;
	}

	public override bool OnSceneLoadAsync(string sceneName, Action onEnd, int priority = 0)
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
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        if (
#if USE_LOADFROMFILECOMPRESS
            asset.CompressType == AssetCompressType.astUnityLzo ||
			asset.CompressType == AssetCompressType.astUnityZip ||
#endif
            asset.CompressType == AssetCompressType.astNone
		   )
		{
			return LoadAsyncAssetInfo(asset, null, ref addCount, priority, sceneName,
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
		if (
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            asset.CompressType == AssetCompressType.astUnityLzo ||
#endif
            asset.CompressType == AssetCompressType.astUnityZip
            )
		{
				return LoadWWWAsseetInfo(asset, null, ref addCount, sceneName,
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
			if (!LoadAssetInfo (asset, ref addCount, sceneName))
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

    public string GetShaderFileNameByName(string shaderName) {
        string ret;
        if (mShaderNameMap.TryGetValue(shaderName, out ret))
            return ret;
        return string.Empty;
    }

	public bool PreloadAllType(string abFileName, System.Type type, Action onEnd = null, int priority = 0)
	{
		if (type == null || string.IsNullOrEmpty(abFileName))
			return false;

		AssetInfo asset = FindAssetInfo(abFileName);
		if (asset == null)
			return false;

		int addCount = 0;
        //bool isNew = asset.IsNew();
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        if (asset.CompressType == AssetCompressType.astUnityLzo || 
			asset.CompressType == AssetCompressType.astUnityZip ||
			asset.CompressType == AssetCompressType.astNone
		   ) {
			return LoadAsyncAssetInfo(asset, null, ref addCount, priority, string.Empty,
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
				return LoadWWWAsseetInfo(asset, null, ref addCount, string.Empty, 
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

			if (!LoadAssetInfo (asset, ref addCount, string.Empty))
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
        if (!mAssetFileNameMap.TryGetValue (fileName, out asset)) {
            asset = LoadABFileInfo (fileName);
        }

		return asset;
	}

    public override bool LoadSpritesAsync(string fileName, Action<float, bool, UnityEngine.Object[]> onProcess, int priority) {
		fileName = TransFileName (fileName, ".tex");
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		return LoadObjectAsync<Texture>(fileName, ResourceCacheType.rctRefAdd, priority,
			delegate(float process, bool isDone, Texture obj) {
				if (isDone) {
					if (obj != null) {

						if (onProcess != null)
							onProcess(process * 0.9f, false, null);

						bool b = _LoadSpritesAsync(fileName, obj, ResourceCacheType.rctRefAdd, priority, onProcess);
						if (!b)
							return;
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
		
	private bool _LoadSpritesAsync(string fileName, Texture texture, ResourceCacheType cacheType, int priority, Action<float, bool, UnityEngine.Object[]> onProcess)
	{
		if (texture == null)
			return false;

		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null || asset.Cache == null)
		{
			if (onProcess != null)
				onProcess(1.0f, true, null);
			return false;
		}

		bool ret = asset.LoadSubsAsync<Sprite>(fileName, priority, 
		   delegate (AssetBundleRequest req) {

			if (req.isDone)
			{
				ResourceMgr.Instance.DestroyObject(texture);
				_OnLoadSprites(asset, req.allAssets, cacheType);
			}

			if (onProcess != null)
				onProcess (req.progress/10.0f + 0.9f, req.isDone, req.allAssets);

		  }
		);

		if (!ret)
		{
			ResourceMgr.Instance.DestroyObject(texture);
			if (onProcess != null)
				onProcess(1.0f, true, null);
			return false;
		}

		return true;
	}

	private void _OnLoadSprites(AssetInfo asset, UnityEngine.Object[] ret, ResourceCacheType cacheType)
	{
		if (asset == null || ret == null || ret.Length <= 0)
			return;

		if (ret != null && ret.Length > 0 && cacheType != ResourceCacheType.rctNone) {
			if (cacheType == ResourceCacheType.rctRefAdd)
				AssetCacheManager.Instance.CacheAddRefCount(asset.Cache, ret.Length);
			for (int i = 0; i < ret.Length; ++i) {
				AssetCacheManager.Instance._OnLoadObject(ret[i], asset.Cache);
			}
		}
	}

	private Sprite[] _LoadSprites(string fileName, ResourceCacheType cacheType) {
		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null || asset.Cache == null)
			return null;
		
         Sprite[]  ret = asset.LoadSubs<Sprite>(fileName);

		_OnLoadSprites (asset, ret, cacheType);

		return ret;
	}

	// 加载Sprite
	public override Sprite[] LoadSprites(string fileName) {
		fileName = TransFileName (fileName, ".tex");
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		Texture tex = LoadObject<Texture>(fileName, ResourceCacheType.rctTemp);
		if (tex == null)
			return null;

		Sprite[] ret = _LoadSprites(fileName, ResourceCacheType.rctRefAdd);

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

		bool isNew = (asset.Cache == null) || IsAssetInfoUnloadDep(asset, fileName);
		int addCount = 0;
		if (!LoadAssetInfo (asset, ref addCount, fileName))
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
					asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(ret, fileName, null);
				AssetCacheManager.Instance._AddTempAsset(asset.Cache);
				// asset.UnUsed ();
			}
		}
		else
		if (ret != null) {
			if (asset.Cache == null)
				asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(ret, fileName, null);
	
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

	private bool DoLoadObjectAsync<T>(AssetInfo asset, string fileName, ResourceCacheType cacheType, int priority,
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
		//asset.IsUsing = true;
		asset.AddUsingCnt();
		bool ret = asset.LoadObjectAsync (fileName, typeof(T), priority, 
		                              delegate (AssetBundleRequest req) {
			
			if (req.isDone)
			{
				//asset.IsUsing = false;
				asset.DecUsingCnt();
				bool isNew = (asset.Cache == null) || IsAssetInfoUnloadDep(asset, fileName);
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
			asset.DecUsingCnt ();
			//asset.IsUsing = false;

		return ret;
	}

	public bool LoadObjectAsync<T>(string fileName, ResourceCacheType cacheType, int priority, Action<float, bool, T> onProcess) where T: UnityEngine.Object
	{
#if USE_LOWERCHAR
		fileName = fileName.ToLower();
#endif
		AssetInfo asset = FindAssetInfo(fileName);
		if (asset == null)
			return false;

		int addCount = 0;
        //	bool isNew = asset.IsNew();
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        if (
#if USE_LOADFROMFILECOMPRESS
            asset.CompressType == AssetCompressType.astUnityLzo ||
			asset.CompressType == AssetCompressType.astUnityZip ||
#endif
            asset.CompressType == AssetCompressType.astNone
			) {
#if USE_ABFILE_ASYNC
			return LoadAsyncAssetInfo(asset, null, ref addCount, priority, fileName,
				delegate(bool isOk) {
					if (isOk) {
						DoLoadObjectAsync<T>(asset, fileName, cacheType, priority, onProcess);
					}
				});
#else
		if (!LoadAssetInfo (asset, ref addCount, fileName))
			return false;
		return DoLoadObjectAsync<T>(asset, fileName, cacheType, priority, onProcess);
#endif
		}
		else
#endif
		if (
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            asset.CompressType == AssetCompressType.astUnityLzo ||
#endif
            asset.CompressType == AssetCompressType.astUnityZip      
            )
		{
				return LoadWWWAsseetInfo(asset, null, ref addCount, fileName,
			                  delegate (bool isOk){
								  if (isOk)
							      {
										DoLoadObjectAsync<T>(asset, fileName, cacheType, priority, onProcess);
								  }
					});
		} else
		{
			if (!LoadAssetInfo (asset, ref addCount, fileName))
				return false;
			return DoLoadObjectAsync<T>(asset, fileName, cacheType, priority, onProcess);
		}
	}

	private void OnLoadObjectAsync(string fileName, AssetInfo asset, bool isNew, UnityEngine.Object obj, ResourceCacheType cacheType)
	{
		if (cacheType == ResourceCacheType.rctNone)
		{
			if (isNew && (obj != null))
			{
				if (asset.Cache == null)
					asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(obj, fileName, null);
				AssetCacheManager.Instance._AddTempAsset(asset.Cache);
				// asset.UnUsed ();
			}
		}
		else
		if (obj != null) {
			if (asset.Cache == null)
				asset.Cache = ResourceMgr.Instance.AssetLoader.CreateCache(obj, fileName, null);
			
			if (asset.Cache != null)
				AssetCacheManager.Instance._OnLoadObject(obj, asset.Cache);
		}
	}

	public override AssetCache CreateCache(UnityEngine.Object orgObj, string fileName, System.Type orgType)
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

	public override bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Shader> (TransFileName(fileName, ".shader"), cacheType, priority, onProcess);
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

	public override bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess, int priority = 0)
	{
#if USE_HAS_EXT
		return LoadObjectAsync<GameObject> (fileName, cacheType, priority, onProcess);
#else
		bool ret = LoadObjectAsync<GameObject> (TransFileName(fileName, ".prefab"), cacheType, priority, onProcess);
		if (!ret)
			ret = LoadObjectAsync<GameObject> (TransFileName(fileName, ".fbx"), cacheType, priority, onProcess);
		return ret;
#endif
	}

	public override AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<AudioClip> (TransFileName(fileName, ".audio"), cacheType);
	}

	public override bool LoadAudioClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AudioClip> onProcess, int priority = 0)
	{
		return LoadObjectAsync<AudioClip> (TransFileName(fileName, ".audio"), cacheType, priority, onProcess);
	}

	public override string LoadText(string fileName, ResourceCacheType cacheType)
	{
		TextAsset asset = LoadObject<TextAsset> (TransFileName(fileName, ".bytes"), cacheType);
		if (asset == null)
			return null;
        return asset.text;
	}

	public override byte[] LoadBytes(string fileName, ResourceCacheType cacheType)
	{
		TextAsset asset = LoadObject<TextAsset> (TransFileName(fileName, ".bytes"), cacheType);
		if (asset == null)
			return null;
		return asset.bytes;
	}

	public override bool LoadTextAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, TextAsset> onProcess, int priority = 0)
	{
		return LoadObjectAsync (TransFileName(fileName, ".bytes"), cacheType, priority, onProcess);
	}

	public override Material LoadMaterial(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Material> (TransFileName(fileName, ".mat"), cacheType);
	}

	public override bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Material> (TransFileName(fileName, ".mat"), cacheType, priority, onProcess);
	}

	public override Texture LoadTexture(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Texture> (TransFileName(fileName, ".tex"), cacheType);
	}

	public override bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Texture> (TransFileName(fileName, ".tex"), cacheType, priority, onProcess);
	}

	public override Font LoadFont (string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<Font>(TransFileName(fileName, ".ttf"), cacheType);
	}

	public override bool LoadFontAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, Font> onProcess, int priority = 0)
	{
		return LoadObjectAsync<Font> (TransFileName (fileName, ".ttf"), cacheType, priority, onProcess);
	}

	public override RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<RuntimeAnimatorController> (TransFileName(fileName, ".controller"), cacheType);
	}

	public override bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess, int priority = 0)
	{
		return LoadObjectAsync<RuntimeAnimatorController> (TransFileName(fileName, ".controller"), cacheType, priority, onProcess);
	}

	public override AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<AnimationClip> (TransFileName(fileName, ".anim"), cacheType);
	}

	public override bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess, int priority = 0)
	{
		return LoadObjectAsync<AnimationClip> (TransFileName(fileName, ".anim"), cacheType, priority, onProcess);
	}

	public override ScriptableObject LoadScriptableObject (string fileName, ResourceCacheType cacheType)
	{
		return LoadObject<ScriptableObject> (TransFileName(fileName, ".asset"), cacheType);
	}

	public override bool LoadScriptableObjectAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.ScriptableObject> onProcess, int priority = 0)
	{
		return LoadObjectAsync<ScriptableObject> (TransFileName (fileName, ".asset"), cacheType, priority, onProcess);
	}

#if UNITY_5
	public override ShaderVariantCollection LoadShaderVarCollection(string fileName, 
	                                                                ResourceCacheType cacheType)
	{
		return LoadObject<ShaderVariantCollection> (TransFileName(fileName, ".shaderVar"), cacheType);
	}
	
	public override bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType cacheType, 
		Action<float, bool, ShaderVariantCollection> onProcess, int priority = 0)
	{
		return LoadObjectAsync<ShaderVariantCollection> (TransFileName(fileName, ".shaderVar"), cacheType, priority, onProcess);
	}
#endif

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5

    internal bool LoadAsyncAssetInfo(AssetInfo asset, TaskList taskList, ref int addCount, int priority, 
									 string resFileName, Action<bool> onEnd = null)
	{
		if (asset == null)
			return false;

		if (asset.IsVaild () || asset.GetOrgAsset(resFileName) != null)
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

		//asset.IsUsing = true;
		asset.AddUsingCnt();
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
				if (!LoadAsyncAssetInfo(depend, taskList, ref addCount, priority, string.Empty))
				{
					//asset.IsUsing = false;
					asset.DecUsingCnt();
					return false;
				}
			}
		}
			
		addCount += 1;
		AssetCacheManager.Instance._CheckAssetBundleCount (addCount);

		bool ret = asset.LoadAsync (taskList, priority);

		//Debug.LogFormat ("==>ab Load: {0}", asset.FileName);
		return ret;
	}

#endif

	internal bool LoadWWWAsseetInfo(AssetInfo asset, TaskList taskList, ref int addCount, string resFileName, Action<bool> onEnd = null)
	{
		if (asset == null)
			return false;

		if (asset.IsVaild () || asset.GetOrgAsset(resFileName) != null)
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

		//asset.IsUsing = true;
		asset.AddUsingCnt();
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
					//asset.IsUsing = false;
					asset.DecUsingCnt ();
					return false;
#endif
				}
				if (!LoadWWWAsseetInfo(depend, taskList, ref addCount, string.Empty))
				{
					//asset.IsUsing = false;
					asset.DecUsingCnt();
					return false;
				}
			}
		}

		addCount += 1;
		AssetCacheManager.Instance._CheckAssetBundleCount (addCount);

		bool ret = asset.LoadWWW (taskList);
		return ret;
	}

	private static bool IsAssetInfoUnloadDep(AssetInfo asset, string resFileName)
	{
		bool ret = false;
		if (asset == null)
			return ret;
		AssetBundleCache cache = asset.Cache as AssetBundleCache;
		if (cache != null) {
			ret = !cache.IsloadDecDepend;
			// 只有当不存在这个资源的时候才需要把依赖再读一次
			if (ret && !string.IsNullOrEmpty(resFileName))
				ret = asset.GetOrgAsset (resFileName) == null;
		}
		return ret;
	}

	internal bool LoadAssetInfo(AssetInfo asset, ref int addCount, string resFileName)
	{
		if (asset == null)
			return false;

		if ((asset.IsVaild () || asset.IsLocalUsing) || asset.GetOrgAsset(resFileName) != null)
			return true;

		asset.IsLocalUsing = true;
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
					asset.IsLocalUsing = false;
					return false;
#endif
                }
				if (!LoadAssetInfo(depend, ref addCount, string.Empty))
				{
					asset.IsLocalUsing = false;
					return false;
				}
			}
		}

		asset.IsLocalUsing = false;
        addCount += 1;
        AssetCacheManager.Instance._CheckAssetBundleCount (addCount);
		bool ret = asset.Load ();
		//	Debug.LogFormat ("-->ab load: {0}", asset.FileName);
		return ret;
	}

	private void AddOrUpdateAssetCache(AssetInfo asset)
	{
		if (asset == null)
			return;
		AddOrUpdateDependAssetCache (asset);
		if (asset.Cache == null)
			asset.Cache = AssetBundleCache.Create(asset);
		AssetBundleCache abCache = asset.Cache as AssetBundleCache;
		if (abCache != null)
			abCache.IsloadDecDepend = true;
		AssetCacheManager.Instance._AddOrUpdateUsedList (asset.Cache);
	}

	// DependAssetCache
	private void AddOrUpdateDependAssetCache(AssetInfo asset)
	{
		if (asset == null)
			return;

		if (/*!asset.IsUsing &&*/ (!asset.IsVaild ()) || asset.IsLocalUsing)
			return;

		asset.IsLocalUsing = true;

		for (int i = 0; i < asset.DependFileCount; ++i) {
			string fileName = asset.GetDependFileName(i);
			if (!string.IsNullOrEmpty(fileName))
			{
				AssetInfo depend = FindAssetInfo(fileName);
				if ((depend != null) && (!depend.IsLocalUsing))
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


		asset.IsLocalUsing = false;
		AssetBundleCache abCache = asset.Cache as AssetBundleCache;
		if (abCache != null)
			abCache.IsloadDecDepend = true;
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
#if USE_DEP_BINARY_AB
		string ret = GetCheckFileName("AssetBundles.xml", false, true);
#else
		string ret = GetCheckFileName("AssetBundles.xml", true, false);
#endif
		return ret;
	}

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

	// 资源更新清理
	public void AutoUpdateClear()
	{
		mConfigLoaderEvent = null;

		if (mLoaderTimer != null)
		{
			mLoaderTimer.Dispose();
			mLoaderTimer = null;
		}

		if (mXmlLoaderTask != null)
		{
			mXmlLoaderTask.Release();
			mXmlLoaderTask = null;
		}

		mAssetFileNameMap.Clear();
        ClearBinaryStream ();
        mShaderNameMap.Clear();

    }

	// fileName为资源文件名
	public string GetAssetBundleFileName(string fileName)
	{
		#if USE_LOWERCHAR
		fileName = fileName.ToLower();
		#endif
		AssetInfo info = FindAssetInfo(fileName);
		if (info == null)
			return string.Empty;
		return info.FileName;
	}

    private void LoadFlatBuffer(byte[] bytes) {
        if ((bytes == null) || (bytes.Length <= 0)) {
            return;
        }

        FlatBuffers.ByteBuffer buffer = new FlatBuffers.ByteBuffer(bytes);
        var tree = AssetBundleFlatBuffer.AssetBundleTree.GetRootAsAssetBundleTree(buffer);
        var header = tree.FileHeader.GetValueOrDefault();

        Dictionary<string, string> fileRealMap = null;

        for (int i = 0; i < header.AbFileCount; ++i) {
            var assetBundleInfo = tree.AssetBundles(i).GetValueOrDefault();
            var abHeader = assetBundleInfo.FileHeader.GetValueOrDefault();
            AssetCompressType compressType = (AssetCompressType)abHeader.CompressType;
            bool isUseCreateFromFile = compressType == AssetCompressType.astNone
#if USE_LOADFROMFILECOMPRESS
                                                        || compressType == AssetCompressType.astUnityLzo

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                                                        || compressType == AssetCompressType.astUnityZip
#endif

#endif
                                                        ;

            string assetBundleFileName = GetCheckFileName(ref fileRealMap, abHeader.AbFileName,
                                                            false, isUseCreateFromFile);

            AssetInfo asset;
            if (!mAssetFileNameMap.TryGetValue(assetBundleFileName, out asset)) {
                asset = new AssetInfo(assetBundleFileName);
                asset._SetCompressType(compressType);
                // 额外添加一个文件名的映射
                AddFileAssetMap(assetBundleFileName, asset);
            } else {
                ;
            }

            // 子文件
            for (int j = 0; j < abHeader.SubFileCount; ++j) {
                var subInfo = assetBundleInfo.SubFiles(j).GetValueOrDefault();
                string subFileName = subInfo.FileName;
                if (string.IsNullOrEmpty(subFileName))
                    continue;
                asset._AddSubFile(subFileName);
                AddFileAssetMap(subFileName, asset);
                if (!string.IsNullOrEmpty(subInfo.ShaderName)) {
                    if (!mShaderNameMap.ContainsKey(subInfo.ShaderName))
                        mShaderNameMap.Add(subInfo.ShaderName, subInfo.FileName);
                    else {
                        Debug.LogWarningFormat("ShaderName: {0} has exists!!!", subInfo.ShaderName);
                    }
                }
            }

            // 依赖
            for (int j = 0; j < abHeader.DependFileCount; ++j) {
                var depInfo = assetBundleInfo.DependFiles(j).GetValueOrDefault();
                string dependFileName = GetCheckFileName(ref fileRealMap, depInfo.AbFileName,
                                                            false, isUseCreateFromFile);
                asset._AddDependFile(dependFileName, depInfo.RefCount);
            }
        }

    }

    //-----分块加载AssetBundles.xml
    private MemoryStream m_BinaryStream = null;
    private Dictionary<string, string> m_FileRealMap = null;
    private void ClearBinaryStream()
    {
        if (m_BinaryStream != null) {
            m_BinaryStream.Close ();
            m_BinaryStream.Dispose ();
            m_BinaryStream = null;
        }    

        m_AssetMapOffsetMap.Clear ();
        m_OffsetAssetInfoMap.Clear ();

        m_FileRealMap = null;
    }

    private Dictionary<string, long> m_AssetMapOffsetMap = new Dictionary<string, long> ();
    private Dictionary<long, AssetInfo> m_OffsetAssetInfoMap = new Dictionary<long, AssetInfo> ();
    private void LoadAssetOffsetMap(Stream stream, DependBinaryFile.FileHeader header)
    {
        stream.Seek (header.fileMapOffset, SeekOrigin.Begin);

        for (int i = 0; i < header.fileMapCount; ++i) {
            string abFileName = FilePathMgr.Instance.ReadString (stream);
            long offset = FilePathMgr.Instance.ReadLong (stream);
            m_AssetMapOffsetMap [abFileName] = offset;
        }
    }

    private AssetInfo LoadABFileInfo(string resFileName)
    {
        if (m_BinaryStream == null)
            return null;

        long offset;
        if (!m_AssetMapOffsetMap.TryGetValue (resFileName, out offset))
            return null;
        AssetInfo ret;
        if (m_OffsetAssetInfoMap.TryGetValue (offset, out ret))
            return ret;
        
        m_BinaryStream.Seek (offset, SeekOrigin.Begin);
        DependBinaryFile.ABFileHeader abHeader = DependBinaryFile.LoadABFileHeader (m_BinaryStream);
        AssetCompressType compressType = (AssetCompressType)abHeader.compressType;
        bool isUseCreateFromFile = compressType == AssetCompressType.astNone
            #if USE_LOADFROMFILECOMPRESS
            || compressType == AssetCompressType.astUnityLzo

            #if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            || compressType == AssetCompressType.astUnityZip
            #endif

            #endif
            ;

        string assetBundleFileName = GetCheckFileName(ref m_FileRealMap, abHeader.abFileName, 
            false, isUseCreateFromFile);

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

        // 子文件
        for (int j = 0; j < abHeader.subFileCount; ++j) {
            DependBinaryFile.SubFileInfo subInfo = DependBinaryFile.LoadSubInfo (m_BinaryStream);
            string subFileName = subInfo.fileName;
            if (string.IsNullOrEmpty (subFileName))
                continue;
            asset._AddSubFile(subFileName);
            AddFileAssetMap(subFileName, asset);
            if (!string.IsNullOrEmpty(subInfo.shaderName)) {
                if (!mShaderNameMap.ContainsKey(subInfo.shaderName))
                    mShaderNameMap.Add(subInfo.shaderName, subInfo.fileName);
                else {
                    Debug.LogWarningFormat("ShaderName: {0} has exists!!!", subInfo.shaderName);
                }
            }
        }

        // 依赖
        for (int j = 0; j < abHeader.dependFileCount; ++j) {
            DependBinaryFile.DependInfo depInfo = DependBinaryFile.LoadDependInfo (m_BinaryStream);
            string dependFileName = GetCheckFileName(ref m_FileRealMap, depInfo.abFileName,
                false, isUseCreateFromFile);
            asset._AddDependFile(dependFileName, depInfo.refCount);
        }

        if (asset != null) {
            m_OffsetAssetInfoMap.Add (offset, asset);
        }

        return asset;
    }

    private void LoadBinaryHeader(byte[] bytes)
    {
        if ((bytes == null) || (bytes.Length <= 0)) {
            return;
        }

        ClearBinaryStream ();

        m_BinaryStream = new MemoryStream (bytes);
        DependBinaryFile.FileHeader header = DependBinaryFile.LoadFileHeader (m_BinaryStream);
        if (!DependBinaryFile.CheckFileHeader(header)) {
                // 兼容
                if (DependBinaryFile.CheckFileHeaderD01(header))
                    LoadBinary(bytes);
                return;
            }
        long offset = header.fileMapOffset;
        if (offset <= 0) {
            LoadBinary (bytes);
            return;
        }
        
        LoadAssetOffsetMap (m_BinaryStream, header);
    }

    //------------------------

	//二进制
	private void LoadBinary(byte[] bytes)
	{
		if ((bytes == null) || (bytes.Length <= 0)) {
			return;
		}

        ClearBinaryStream ();

		MemoryStream stream = new MemoryStream (bytes);

		DependBinaryFile.FileHeader header = DependBinaryFile.LoadFileHeader (stream);
		/*
		if (!DependBinaryFile.CheckFileHeaderD01(header)) {
                // 兼容
                if (DependBinaryFile.CheckFileHeader(header))
                    LoadBinaryHeader(bytes);
                return;
            }
		*/
		if (!DependBinaryFile.CheckFileHeader(header) && 
                !DependBinaryFile.CheckFileHeaderD01(header))
                return;
				
		Dictionary<string, string> fileRealMap = null;
		
		for (int i = 0; i < header.abFileCount; ++i) {
			DependBinaryFile.ABFileHeader abHeader = DependBinaryFile.LoadABFileHeader (stream);
			AssetCompressType compressType = (AssetCompressType)abHeader.compressType;
			bool isUseCreateFromFile = compressType == AssetCompressType.astNone
#if USE_LOADFROMFILECOMPRESS
                                                        || compressType == AssetCompressType.astUnityLzo

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                                                        || compressType == AssetCompressType.astUnityZip
#endif

#endif
                                                        ;

			string assetBundleFileName = GetCheckFileName(ref fileRealMap, abHeader.abFileName, 
                                                            false, isUseCreateFromFile);

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

			// 子文件
			for (int j = 0; j < abHeader.subFileCount; ++j) {
				DependBinaryFile.SubFileInfo subInfo = DependBinaryFile.LoadSubInfo (stream);
				string subFileName = subInfo.fileName;
				if (string.IsNullOrEmpty (subFileName))
					continue;
				asset._AddSubFile(subFileName);
				AddFileAssetMap(subFileName, asset);
                if (!string.IsNullOrEmpty(subInfo.shaderName)) {
                    if (!mShaderNameMap.ContainsKey(subInfo.shaderName))
                        mShaderNameMap.Add(subInfo.shaderName, subInfo.fileName);
                    else {
                        Debug.LogWarningFormat("ShaderName: {0} has exists!!!", subInfo.shaderName);
                    }
                }
			}

			// 依赖
			for (int j = 0; j < abHeader.dependFileCount; ++j) {
				DependBinaryFile.DependInfo depInfo = DependBinaryFile.LoadDependInfo (stream);
				string dependFileName = GetCheckFileName(ref fileRealMap, depInfo.abFileName,
                                                            false, isUseCreateFromFile);
				asset._AddDependFile(dependFileName, depInfo.refCount);
			}
		}

		stream.Close ();
		stream.Dispose ();

	//	GC.Collect ();
	}

	private void LoadXml(string bytes)
	{
		if (string.IsNullOrEmpty(bytes)) {
			return;
		}

	//	string fileName = GetXmlFileName (false);
		
		string str = bytes;
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

        Dictionary<string, string> fileRealMap = null;

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

			bool isUseCreateFromFile = compressType == AssetCompressType.astNone
#if USE_LOADFROMFILECOMPRESS
                                        || compressType == AssetCompressType.astUnityLzo

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                                        || compressType == AssetCompressType.astUnityZip
#endif

#endif
                                        ;
			string assetBundleFileName = GetCheckFileName(ref fileRealMap, localFileName, false, isUseCreateFromFile);

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
                string shaderName = node.GetValue("@shaderName");
                if (!string.IsNullOrEmpty(shaderName)) {
                    if (!mShaderNameMap.ContainsKey(shaderName))
                        mShaderNameMap.Add(shaderName, subFileName);
                    else
                        Debug.LogWarningFormat("ShaderName: {0} has exists!!!", shaderName);
                }
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
							string dependFileName = GetCheckFileName(ref fileRealMap, localDependFileName, false, isUseCreateFromFile);

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
		
	//	GC.Collect ();
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

#if !USE_DEP_BINARY_AB
			float curTime = Time.realtimeSinceStartup;
			float usedTime = curTime - m_LastUsedTime;
			Debug.LogFormat("WWW加载XML：{0}", usedTime.ToString());
			m_LastUsedTime = curTime;
#endif

#if USE_DEP_BINARY
#if USE_FLATBUFFER
            LoadFlatBuffer(mXmlLoaderTask.ByteData);
#else
                #if USE_DEP_BINARY_HEAD
                LoadBinaryHeader(mXmlLoaderTask.ByteData);
                #else
            LoadBinary(mXmlLoaderTask.ByteData);
                #endif
#endif
#else
			LoadXml(mXmlLoaderTask.Text);
#endif

#if !USE_DEP_BINARY_AB
			usedTime = Time.realtimeSinceStartup - m_LastUsedTime;
			Debug.LogFormat("解析XML时间：{0}", usedTime.ToString());
#endif

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

#if !USE_DEP_BINARY_AB
	private float m_LastUsedTime = 0;
#endif

	 private string GetCheckFileName(ref Dictionary<string, string> fileRealMap, string abFileName, bool isWWW,
                bool isUseABCreateFromFile) {
            if (string.IsNullOrEmpty(abFileName))
                return string.Empty;

            if (fileRealMap == null)
                fileRealMap = new Dictionary<string, string>();
            string assetBundleFileName;
            if (!fileRealMap.TryGetValue(abFileName, out assetBundleFileName)) {
                assetBundleFileName = GetCheckFileName(abFileName, isWWW, isUseABCreateFromFile);
                fileRealMap.Add(abFileName, assetBundleFileName);
            }

            return assetBundleFileName;
    }

	// 手动调用读取配置
	public void LoadConfigs(Action<bool> OnFinishEvent)
	{

#if !USE_DEP_BINARY_AB
		m_LastUsedTime = Time.realtimeSinceStartup;
#endif

#if USE_DEP_BINARY && USE_DEP_BINARY_AB

		float startTime = Time.realtimeSinceStartup;

		AssetBundle bundle;
		string fileName = GetXmlFileName();
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        bundle = AssetBundle.LoadFromFile(fileName);
#else
		bundle = AssetBundle.CreateFromFile(fileName);
#endif
		if (bundle != null)
		{
			float curTime = Time.realtimeSinceStartup;
			float usedTime = curTime - startTime;
			Debug.LogFormat("加载XML AB时间：{0}", usedTime.ToString());
			startTime = curTime;

			string name = System.IO.Path.GetFileNameWithoutExtension(fileName);
			TextAsset asset = bundle.LoadAsset<TextAsset>(name);
			if (asset != null)
			{
#if USE_FLATBUFFER
                LoadFlatBuffer(asset.bytes);
#else
        #if USE_DEP_BINARY_HEAD
                LoadBinaryHeader(asset.bytes);
        #else
                LoadBinary(asset.bytes);
        #endif
#endif
                usedTime = Time.realtimeSinceStartup - startTime;
				Debug.LogFormat("解析XML时间：{0}", usedTime.ToString());

				bundle.Unload(true);
				if (OnFinishEvent != null)
					OnFinishEvent(true);
			} else
			{
				Debug.LogErrorFormat("[LoadConfig]读取TextAsset {0} 失敗", name);
				bundle.Unload(true);
				if (OnFinishEvent != null)
					OnFinishEvent(false);
			}
		} else
		{
			Debug.LogErrorFormat("[LoadConfig]加載 {0} bundle失敗", fileName);
			if (OnFinishEvent != null)
				OnFinishEvent(false);
		}
#else
		mConfigLoaderEvent = OnFinishEvent;
		if (mXmlLoaderTask == null) {
			// 已经在读取状态了不会再调用
			string fileName = GetXmlFileName ();
			if (string.IsNullOrEmpty (fileName))
				return;
			mXmlLoaderTask = WWWFileLoadTask.Create(fileName);
			if (mXmlLoaderTask.IsDoing) {
				// 创建时钟
				if (mLoaderTimer == null)
				{
					mLoaderTimer = TimerMgr.Instance.CreateTimer (false, 0, true);
					mLoaderTimer.AddListener (OnLoaderTimer);
				}
			}
		}
#endif

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
    private Dictionary<string, string> mShaderNameMap = new Dictionary<string, string>();
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
