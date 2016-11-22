// 任务队列

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 任务接口
public abstract class ITask
{
	// 是否已经好了
	public bool IsDone {
		get
		{
			return (mResult != 0);
		}
	}

	// 结果
	public int Result {
		get
		{
			return mResult;
		}
		set
		{
			mResult = value;
		}
	}

	public bool IsDoing
	{
		get {
			return mResult == 0;
		}
	}

	public bool IsOk
	{
		get {
			return mResult > 0;
		}
	}

	public bool IsFail
	{
		get {
			return mResult < 0;
		}
	}

	public void AddResultEvent(Action<ITask> evt)
	{
		if (OnResult == null)
			OnResult = evt;
		else
			OnResult += evt;
	}

	// 执行回调
	public Action<ITask> OnResult {
		get;
		protected set;
	}

	// 用户数据
	public System.Object UserData {
		get;
		set;
	}

	// 设置拥有者
	public TaskList _Owner
	{
		get {
			return mOwner;
		}

		set {
			mOwner = value;
		}
	}

	// 处理
	public abstract void Process();
	public virtual void Release()
	{
	}

	protected void TaskOk()
	{
		mResult = 1;
	}

	protected void TaskFail()
	{
		mResult = -1;
	}

	protected int mResult = 0;
	private TaskList mOwner = null;
}

#if UNITY_5_3 || UNITY_5_4

// LoadFromFileAsync
public class BundleCreateAsyncTask: ITask
{
	public BundleCreateAsyncTask(string createFileName)
	{
		if (string.IsNullOrEmpty(createFileName))
		{
			TaskFail();
			return;
		}

		m_FileName = createFileName;
	}

	public BundleCreateAsyncTask()
	{}

	public static int GetPoolCount()
	{
		return m_Pool.Count;
	}

	public static BundleCreateAsyncTask Create(string createFileName)
	{
		if (string.IsNullOrEmpty(createFileName))
			return null;
		BundleCreateAsyncTask ret = GetNewTask();
		ret.m_FileName = createFileName;
		return ret;
	}

	public static BundleCreateAsyncTask LoadFileAtStreamingAssetsPath(string fileName, bool usePlatform)
	{
		fileName = WWWFileLoadTask.GetStreamingAssetsPath(usePlatform, true) + "/" + fileName;
		BundleCreateAsyncTask ret = new BundleCreateAsyncTask(fileName);
		return ret;
	}

	public void StartLoad()
	{
		if (m_Req == null)
		{
			m_Req = AssetBundle.LoadFromFileAsync(m_FileName);
		}
	}

	public override void Release()
	{
		base.Release();
		ItemPoolReset();
		InPool(this);
	}

	public override void Process()
	{
		// 可以加载后面的LOAD
		//StartLoad();

		if (m_Req == null)
		{
			TaskFail();
			return;
		}

		if (m_Req.isDone) 
		{
			if (m_Req.assetBundle != null) {
				m_Progress = 1.0f;
				TaskOk ();
				m_Bundle = m_Req.assetBundle;
			} else
				TaskFail ();

			m_Req = null;
		} else {
			m_Progress = m_Req.progress;
		}

		if (OnProcess != null)
			OnProcess (this);

	}

	public AssetBundle Bundle
	{
		get {
			return m_Bundle;
		}
	}

	public float Progress
	{
		get {
			return m_Progress;
		}
	}

	public Action<BundleCreateAsyncTask> OnProcess {
		get;
		set;
	}

    public string FileName
    {
        get
        {
            return m_FileName;
        }
    }

	private static BundleCreateAsyncTask GetNewTask()
	{
		if (m_UsePool)
		{
			InitPool();
			BundleCreateAsyncTask ret = m_Pool.GetObject();
			if (ret != null)
				ret.m_IsInPool = false;
			return ret;
		}

		return new BundleCreateAsyncTask();
	}

	private static void InPool(BundleCreateAsyncTask task)
	{
		if (!m_UsePool || task == null || task.m_IsInPool)
			return;
		InitPool();
		m_Pool.Store(task);	
		task.m_IsInPool = true;
	}

	private void ItemPoolReset()
	{
		if (m_Req != null)
		{
			m_Req = null;
		}

		OnResult = null;
		OnProcess = null;
		m_Bundle = null;
		m_Progress = 0;
		m_FileName = string.Empty;
		mResult = 0;
		UserData = null;
		_Owner = null;
	}

	private static void PoolReset(BundleCreateAsyncTask task)
	{
		if (task == null)
			return;
		task.ItemPoolReset();
	}

	private static void InitPool()
	{
		if (m_PoolInited)
			return;
		m_PoolInited = true;
		m_Pool.Init(0, null, PoolReset);
	}

	private string m_FileName = string.Empty;
	private AssetBundleCreateRequest m_Req = null;
	private float m_Progress = 0;
	private AssetBundle m_Bundle = null;

	private bool m_IsInPool = false;

	private static bool m_UsePool = true;
	private static bool m_PoolInited = false;
	private static Utils.ObjectPool<BundleCreateAsyncTask> m_Pool = new Utils.ObjectPool<BundleCreateAsyncTask>();
}

#endif

// WWW 文件读取任务
public class WWWFileLoadTask: ITask
{
	// 注意：必须是WWW支持的文件名 PC上需要加 file:///
	public WWWFileLoadTask(string wwwFileName)
	{
		if (string.IsNullOrEmpty(wwwFileName)) {
			TaskFail();
			return;
		}

		mWWWFileName = wwwFileName;
	}

	public WWWFileLoadTask()
	{}

    // 是否使用LoadFromCacheOrDownload
    public bool IsUsedCached {
        get;
        set;
    }

	public static WWWFileLoadTask Create(string wwwFileName)
	{
		if (string.IsNullOrEmpty(wwwFileName))
			return null;
		WWWFileLoadTask ret = GetNewTask();
		ret.mWWWFileName = wwwFileName;
		return ret;
	}

	
	// 传入为普通文件名(推荐使用这个函数)
	public static WWWFileLoadTask LoadFileName(string fileName)
	{
		string wwwFileName = ConvertToWWWFileName(fileName);
		WWWFileLoadTask ret = Create(wwwFileName);
		return ret;
	}
	
	// 读取StreamingAssets目录下的文件，只需要相对于StreamingAssets的路径即可(推荐使用这个函数)
	public static WWWFileLoadTask LoadFileAtStreamingAssetsPath(string fileName, bool usePlatform)
	{
		fileName = GetStreamingAssetsPath(usePlatform) + "/" + fileName;
		WWWFileLoadTask ret = LoadFileName(fileName);
		return ret;
	}

	public static string GetStreamingAssetsPath(bool usePlatform, bool isUseABCreateFromFile = false)
	{
		string ret = string.Empty;
		switch (Application.platform)
		{
			case RuntimePlatform.OSXPlayer:
			{
				ret = Application.streamingAssetsPath;
				if (usePlatform)
					ret += "/Mac";
				break;
			}

			case RuntimePlatform.OSXEditor:
			{
				ret = "Assets/StreamingAssets";
				if (usePlatform)
                {
#if UNITY_EDITOR
                    var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
                        if (target == UnityEditor.BuildTarget.StandaloneOSXIntel ||
                            target == UnityEditor.BuildTarget.StandaloneOSXIntel64 ||
                            target == UnityEditor.BuildTarget.StandaloneOSXUniversal)
                            ret += "/Mac";
                        else if (target == UnityEditor.BuildTarget.Android)
                            ret += "/Android";
                        else if (target == UnityEditor.BuildTarget.iOS)
                            ret += "/IOS";
                        else if (target == UnityEditor.BuildTarget.StandaloneWindows || target == UnityEditor.BuildTarget.StandaloneWindows64)
                            ret += "/Windows";
#else
					ret += "/Mac";
#endif
                }
				break;
			}

			case RuntimePlatform.WindowsPlayer:
			{
				ret = Application.streamingAssetsPath;
				if (usePlatform)
					ret += "/Windows";
				break;
			}

			case RuntimePlatform.WindowsEditor:
			{
				ret = "Assets/StreamingAssets";
				if (usePlatform)
                {
#if UNITY_EDITOR
                    var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
                    if (target == UnityEditor.BuildTarget.StandaloneWindows || target == UnityEditor.BuildTarget.StandaloneWindows64)
                        ret += "/Windows";
                    else if (target == UnityEditor.BuildTarget.Android)
                        ret += "/Android";
#else
					ret += "/Windows";
#endif
                }
				break;
			}
			case RuntimePlatform.Android:
			{
				if (isUseABCreateFromFile)
					ret = Application.dataPath + "!assets";
				else
					ret = Application.streamingAssetsPath;
				if (usePlatform)
					ret += "/Android";
				break;
			}
			case RuntimePlatform.IPhonePlayer:
			{
				ret = Application.streamingAssetsPath;
				if (usePlatform)
					ret += "/IOS";
				break;
			}
			default:
				ret = Application.streamingAssetsPath;
				break;
		}
		
		return ret;
	}
	
	// 普通文件名转WWW文件名
	public static string ConvertToWWWFileName(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return string.Empty;
		string ret = System.IO.Path.GetFullPath(fileName);
		if (string.IsNullOrEmpty(ret))
			return string.Empty;
		switch (Application.platform)
		{
			case RuntimePlatform.OSXEditor:
				ret = "file:///" + ret;
				break;
			case RuntimePlatform.WindowsEditor:
				ret = "file:///" + ret; 
				break;
			case RuntimePlatform.OSXPlayer:
				ret = "file:///" + ret; 
				break;
			case RuntimePlatform.WindowsPlayer:
				ret = "file:///" + ret; 
				break;
			case RuntimePlatform.Android:
				ret = ret.Replace("/jar:file:/", "jar:file:///");
				break;
		}
		return ret;
	}

	public override void Release()
	{
		base.Release ();
		ItemPoolReset();
		InPool(this);
	}

	public override void Process()
	{
		if (mLoader == null) {
            if (IsUsedCached)
                mLoader = WWW.LoadFromCacheOrDownload(mWWWFileName, 0);
            else
			    mLoader = new WWW (mWWWFileName);
		}

		if (mLoader == null) {
			TaskFail();
			return;
		}

		if (mLoader.isDone) {
			if (mLoader.assetBundle != null) {
				mProgress = 1.0f;
				TaskOk ();
				mBundle = mLoader.assetBundle;
			} else
			if ((mLoader.bytes != null) && (mLoader.bytes.Length > 0)) {
				mProgress = 1.0f;
				TaskOk ();
				mByteData = mLoader.bytes;
			} else
				TaskFail ();

			mLoader.Dispose ();
			mLoader = null;
		} else {
			mProgress = mLoader.progress;
		}

		if (OnProcess != null)
			OnProcess (this);
	}

	public byte[] ByteData
	{
		get 
		{
			return mByteData;
		}
	}

	public AssetBundle Bundle
	{
		get {
			return mBundle;
		}
	}

	public float Progress
	{
		get {
			return mProgress;
		}
	}

	public Action<WWWFileLoadTask> OnProcess {
		get;
		set;
	}

	public static int GetPoolCount()
	{
		return m_Pool.Count;
	}

	private void ItemPoolReset()
	{
		if (mLoader != null)
		{
			mLoader.Dispose();
			mLoader = null;
		}
		OnResult = null;
		OnProcess = null;
		mProgress = 0;
		mWWWFileName = string.Empty;
		mResult = 0;
		UserData = null;
		_Owner = null;
		mByteData = null;
		mBundle = null;
	}

	private static WWWFileLoadTask GetNewTask()
	{
		if (m_UsePool)
		{
			InitPool();
			WWWFileLoadTask ret = m_Pool.GetObject();
			if (ret != null)
				ret.m_IsInPool = false;
			return ret;
		}

		return new WWWFileLoadTask();
	}

	private static void InPool(WWWFileLoadTask task)
	{
		if (!m_UsePool || task == null || task.m_IsInPool)
			return;
		InitPool();
		m_Pool.Store(task);	
		task.m_IsInPool = true;
	}

	private static void PoolReset(WWWFileLoadTask task)
	{
		if (task == null)
			return;
		task.ItemPoolReset();
	}

	private static void InitPool()
	{
		if (m_PoolInited)
			return;
		m_PoolInited = true;
		m_Pool.Init(0, null, PoolReset);
	}

	private WWW mLoader = null;
	private byte[] mByteData = null;
	private AssetBundle mBundle = null;
	private string mWWWFileName = string.Empty;
	private float mProgress = 0;


	private bool m_IsInPool = false;

	private static bool m_UsePool = true;
	private static bool m_PoolInited = false;
	private static Utils.ObjectPool<WWWFileLoadTask> m_Pool = new Utils.ObjectPool<WWWFileLoadTask>();
}

// 加载场景任务
public class LevelLoadTask: ITask
{
	// 场景名 是否增加方式 是否是异步模式  onProgress(float progress, int result)
	// result: 0 表示进行中 1 表示加载完成 -1表示加载失败
	public LevelLoadTask(string sceneName, bool isAdd, bool isAsync, Action<float, int> onProcess)
	{
		if (string.IsNullOrEmpty (sceneName)) {
			TaskFail();
			return;
		}

		mSceneName = sceneName;
		mIsAdd = isAdd;
		mIsAsync = isAsync;
		mOnProgress = onProcess;
	}

	public override void Process()
	{
		// 同步
		if (!mIsAsync) {
            bool isResult = Application.CanStreamedLevelBeLoaded(mSceneName);

            if (isResult)
            {
                if (mIsAdd)
                    Application.LoadLevelAdditive(mSceneName);
                else
                    Application.LoadLevel(mSceneName);
            }

			
			if (isResult)
			{
				TaskOk();
			}
			else
				TaskFail();

			if (mOnProgress != null)
			{
				if (isResult)
					mOnProgress(1.0f, 1);
				else
					mOnProgress(0, -1);
			}
			
			return;
		}

		if (mOpr == null) {
			// 异步
			if (mIsAdd)
				mOpr = Application.LoadLevelAdditiveAsync (mSceneName);
			else
				mOpr = Application.LoadLevelAsync (mSceneName);
		}

		if (mOpr == null) {
			TaskFail();
			if (mOnProgress != null)
				mOnProgress(0, -1);
			return;
		}

		if (mOpr.isDone) {
			TaskOk();
			if (mOnProgress != null)
				mOnProgress(1.0f, 1);
		} else {
			if (mOnProgress != null)
				mOnProgress(mOpr.progress, 0);
		}

	}

	public string SceneName
	{
		get {
			return mSceneName;
		}
	}

	private string mSceneName = string.Empty;
	private bool mIsAdd = false;
	private bool mIsAsync = false;
	private AsyncOperation mOpr = null;
	private Action<float, int> mOnProgress = null;
}

// 任务列表(为了保证顺序执行)
public class TaskList
{
	public bool Contains(ITask task)
	{
		if (task == null)
			return false;
		int hashCode = task.GetHashCode();
		return mTaskIDs.Contains(hashCode);
	}

	// 保证不要加重复的
	public void AddTask(ITask task, bool isOwner)
	{
		if (task == null)
			return;
		int hashCode = task.GetHashCode();
		if (!mTaskIDs.Contains(hashCode))
		{
			mTaskIDs.Add(hashCode);
			mTaskList.AddLast (task);
			if (isOwner)
				task._Owner = this;
		}
	}

	// 保证不要加重复的
	public void AddTask(LinkedListNode<ITask> node, bool isOwner)
	{
		if ((node == null) || (node.Value == null))
			return;
		int hashCode = node.Value.GetHashCode();
		if (!mTaskIDs.Contains(hashCode))
		{
			mTaskIDs.Add(hashCode);
			mTaskList.AddLast (node);
			if (isOwner)
				node.Value._Owner = this;
		}
	}

	public void Process(Func<ITask, bool> onCheckTaskVaild = null)
	{
		LinkedListNode<ITask> node = mTaskList.First;
		if ((node != null) && (node.Value != null)) {

			if (onCheckTaskVaild != null)
			{
				if (!onCheckTaskVaild(node.Value))
				{
					RemoveTask(node);
					return;
				}
			}

			if (node.Value.IsDone)
			{
				TaskEnd(node.Value);
				RemoveTask(node);
				return;
			}

			TaskProcess(node.Value);

			if (node.Value.IsDone)
			{
				TaskEnd(node.Value);
				RemoveTask(node);
			}
		}
	}

	public bool IsEmpty
	{
		get{
			return mTaskList.Count <= 0;
		}
	}

	public System.Object UserData
	{
		get;
		set;
	}
		
	public void Clear()
	{
		var node = mTaskList.First;
		while (node != null) {
			var next = node.Next;
			if (node.Value != null && node.Value._Owner == this)
				node.Value.Release();
			node = next;
		}

		mTaskList.Clear ();
		mTaskIDs.Clear();
	}

	private void RemoveTask(LinkedListNode<ITask> node)
	{
		if (node == null || node.Value == null)
			return;
		int hashCode = node.Value.GetHashCode();
		if (mTaskIDs.Contains(hashCode))
		{
			mTaskIDs.Remove(hashCode);
			mTaskList.Remove(node);
		}
	}

	public void RemoveTask(ITask task)
	{
		if (task == null)
			return;
		int hashCode = task.GetHashCode();
		if (mTaskIDs.Contains(hashCode))
		{
			mTaskIDs.Remove(hashCode);
			mTaskList.Remove(task);
		}
	}

	public int Count
	{
		get
		{
			return mTaskList.Count;
		}
	}

	private void TaskEnd(ITask task)
	{
		if ((task == null) || (!task.IsDone))
			return;
		if ((task._Owner == this) && (task.OnResult != null))
		{
			task.OnResult (task);
			task.Release();
		}
	}

	private void TaskProcess(ITask task)
	{
		if (task == null)
			return;

		if (task._Owner == this)
			task.Process ();
	}
	
	// 任务必须是顺序执行
	private LinkedList<ITask> mTaskList = new LinkedList<ITask>();
	private HashSet<int> mTaskIDs = new HashSet<int>();
}