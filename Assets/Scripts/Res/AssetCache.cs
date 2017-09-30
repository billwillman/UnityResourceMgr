/*----------------------------------------------------------------
// 模块名：AssetCache
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年9月7日
// 模块描述：
// 			针对需要Cache的资源进行管理（主要是GameObject）--用于GameObject的AssetBundle
//----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using UnityEngine;

// 一个Cache对象, 对于AssetBundle很可能一对多, 对于Resource目录的资源是一对一关系
public abstract class AssetCache {
    // 释放掉
    protected abstract void OnUnLoad();
    protected virtual void OnUnUsed() { }
    protected abstract void OnUnloadAsset(UnityEngine.Object asset);

    public void UnLoad() {
        RemoveAllObj();
        OnUnLoad();
        mRefCount = 0;
    }

    public void UnUsed() {
        RemoveAllObj();
        OnUnUsed();
        mRefCount = 0;

    }

    public void UnloadAsset(UnityEngine.Object asset) {
        if (asset == null)
            return;
        int id = asset.GetInstanceID();
        if (mObjSet.Contains(id)) {
            mObjSet.Remove(id);
            OnUnloadAsset(asset);
        }
    }

    public int RefCount {
        get {
            return mRefCount;
        }
    }

    /*
	public int PrefabRefCount
	{
		get
		{
			return mPrefabRefCnt;
		}
	}*/

    public virtual bool IsNotUsed() {
        return (mRefCount <= 0);//&& (mPrefabRefCnt <= 0);
    }

    /*
	public int _AddPrefabRefCnt(int refCount = 1)
	{
		mPrefabRefCnt += refCount;
		return mPrefabRefCnt;
	}

	public bool HasPrefabRef
	{
		get
		{
			return mPrefabRefCnt > 0;
		}
	}

	public int _DecPrefabRefCnt(int refCount = 1)
	{
		if (mPrefabRefCnt <= 0)
		{
			mPrefabRefCnt = 0;
			return mPrefabRefCnt;
		}

		mPrefabRefCnt -= refCount;
		return mPrefabRefCnt;
	}*/

    public int _AddRefCount(int refCount = 1) {
        mRefCount += refCount;
        return mRefCount;
    }

    public int _DecRefCount(int refCount = 1) {
        if (mRefCount <= 0) {
            mRefCount = 0;
            return mRefCount;
        }

        mRefCount -= refCount;
        if (mRefCount < 0)
            mRefCount = 0;
        return mRefCount;
    }

    // 最后使用时间
    public float LastUsedTime {
        get;
        set;
    }

    internal bool HasLinkListNode {
        get {
            return m_LinkListNode != null;
        }
    }

    internal LinkedListNode<AssetCache> LinkListNode {
        get {
            if (m_LinkListNode == null)
                m_LinkListNode = new LinkedListNode<AssetCache>(this);
            return m_LinkListNode;
        }
    }

    public void AddObj(int id) {
        if (mObjSet == null)
            mObjSet = new HashSet<int>();
        if (mObjSet.Count > 0) {
            if (mObjSet.Contains(id))
                return;
        }
        mObjSet.Add(id);
    }

    public bool existsObj(int id) {
        if (mObjSet == null)
            return false;
        return mObjSet.Contains(id);
    }

    public void RemoveAllObj() {
        if (mObjSet != null) {
            mObjSet.Clear();
            mObjSet = null;
        }
    }

    public void RemoveObj(UnityEngine.Object obj) {
        if (obj == null)
            return;
        int instId = obj.GetInstanceID();
        RemoveObj(instId);
    }

    public void RemoveObj(int id) {
        if (mObjSet == null)
            return;
        if (mObjSet.Contains(id))
            mObjSet.Remove(id);
    }

    public bool GetObjEnumerator(out HashSet<int>.Enumerator iter) {
        if (mObjSet == null) {
            iter = new HashSet<int>.Enumerator();
            return false;
        }
        iter = mObjSet.GetEnumerator();
        return true;
    }

    protected void ClearLinkListNode() {
        if (m_LinkListNode == null)
            return;
        m_LinkListNode.Value = null;
        m_LinkListNode = null;
    }

    private int mRefCount = 0;
    //	private int mPrefabRefCnt = 0;
    private HashSet<int> mObjSet = null;
    protected LinkedListNode<AssetCache> m_LinkListNode = null;
}

public class AssetCacheManager : Singleton<AssetCacheManager> {
    // 未使用时间后清除时间（单位：秒）
    public static readonly float cCacheUnUsedTime = 10f; // 10秒卸载，后面可调
                                                         // 每次从列表判断次数
    public static readonly int cCacheTickCount = 5;
    // 内存警告上限
    public static readonly bool cIsUseCacheMemoryClear = true;
    public static readonly uint cCacheMemoryLimit = 1024 * 1024 * 200;
    public static readonly int cAssetBundleMaxCount = 200;
    public static readonly bool cIsCheckAssetBundleCount = false;

    public AssetCacheManager() {
        // Cache判断Timer, 每一帧都会去判断
        mRunTime = TimerMgr.Instance.CreateTimer(0, true);
        mRunTime.AddListener(OnTimerEvent);
    }

    public uint GetCheckMemorySize() {
        /*
		if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsWebPlayer) ||
			(Application.platform == RuntimePlatform.OSXPlayer) || (Application.platform == RuntimePlatform.OSXWebPlayer)) {
			int memSize = SystemInfo.graphicsMemorySize;
			if (SystemInfo.systemMemorySize > memSize)
				memSize = SystemInfo.systemMemorySize;

			return memSize;
		} else
		if ((Application.platform == RuntimePlatform.WindowsEditor) || (Application.platform == RuntimePlatform.OSXEditor)) {
			int memSize = SystemInfo.graphicsMemorySize;
			if (SystemInfo.systemMemorySize > memSize)
				memSize = SystemInfo.systemMemorySize;

			return memSize / 2;
		} else {
			return SystemInfo.systemMemorySize;
		}*/

        if (Application.isEditor)
            return Profiler.GetTotalReservedMemory() / 2;
        else
            return Profiler.GetTotalReservedMemory();
        //return Profiler.usedHeapSize;
    }

    public void ClearUnUsed(bool isStep = false) {
        float t = GetCurrentTime();

        UpdateUsedList(t, false);
        UpdateNotUsedList(t, false);
        /*ClearAllNotUsedList (true);


		// 因为有依赖关系，需要调两次
		UpdateUsedList (t, false);
		UpdateNotUsedList (t, false);
		ClearAllNotUsedList (true);*/
        while (true) {
            if (mNotUsedCacheList.Count <= 0)
                break;
            if (!ClearAllNotUsedList(true))
                break;
            if (isStep)
                break;
            UpdateUsedList(t, false);
            UpdateNotUsedList(t, false);
        }
    }

    // instObj 为实例化的对象
    public bool _OnCreateGameObject(GameObject instObj, GameObject orgObj) {
        if ((instObj == null) || (orgObj == null))
            return false;

        // 已经包含了（不应该出现）
        int instId = instObj.GetInstanceID();
        if (mInstObjToObjMap.ContainsKey(instId)) {
            LogMgr.Instance.LogError("_OnCreateGameObject: instObj's Id is exist");
            return false;
        }

        AssetCache cache;
        int orgId = orgObj.GetInstanceID();
        if (!mObjCacheMap.TryGetValue(orgId, out cache))
            cache = null;
        if (cache == null) {
            LogMgr.Instance.LogError("_OnCreateGameObject: not call Load GameObject");
            return false;
        }

        _AddOrUpdateUsedList(cache);

        mInstObjToObjMap.Add(instId, orgId);
        return true;
    }

    public bool GetInstGameObjectOrgId(int instId, out int orgId) {
        if (!mInstObjToObjMap.TryGetValue(instId, out orgId))
            return false;
        return true;
    }

    public AssetCache FindInstGameObjectCache(GameObject target) {
        AssetCache ret = null;

        if (target == null)
            return ret;
        int orgObjId;
        if (mInstObjToObjMap.TryGetValue(target.GetInstanceID(), out orgObjId)) {
            if (!mObjCacheMap.TryGetValue(orgObjId, out ret))
                ret = null;
        }

        return ret;
    }

    public void _AddOrUpdateUsedList(AssetCache newCache, int refCount = 1) {
        if ((newCache == null) || (mUsedCacheList == null))
            return;

        if (CacheAddRefCount(newCache, refCount))
            return;

        newCache._AddRefCount(refCount);
        newCache.LastUsedTime = GetCurrentTime();
        mCacheSet.Add(newCache);
        LinkedListNode<AssetCache> node = newCache.LinkListNode;
        mUsedCacheList.AddLast(node);
    }

    public void _OnLoadObject(UnityEngine.Object orgObj, AssetCache cache) {
        if ((orgObj == null) || (cache == null))
            return;

        AssetCache ca;
        if (!mObjCacheMap.TryGetValue(orgObj.GetInstanceID(), out ca))
            ca = null;

        if (ca != null) {
            if (ca.RefCount == 0) {
                ca.LastUsedTime = GetCurrentTime();
            }
            return;
        }

        // 从Temp列表清除
        RemoveTempAsset(cache);

        // 设置一次时间(防止短时间删除删除)
        cache.LastUsedTime = GetCurrentTime();
        cache.AddObj(orgObj.GetInstanceID());

        if (!mCacheSet.Contains(cache)) {
            mCacheSet.Add(cache);
            LinkedListNode<AssetCache> node = cache.LinkListNode;
            mNotUsedCacheList.AddLast(node);
        }

        mObjCacheMap.Add(orgObj.GetInstanceID(), cache);
    }

    internal void _Unload(AssetCache cache, bool isUnloadTrue = true) {
        if (cache == null)
            return;

        if (cache.HasLinkListNode) {
            LinkedListNode<AssetCache> node = cache.LinkListNode;
            if (node != null) {
                if (node.List == mUsedCacheList)
                    mUsedCacheList.Remove(node);
                else if (node.List == mNotUsedCacheList)
                    mNotUsedCacheList.Remove(node);
                else if (node.List == mTempAssetList)
                    RemoveTempAsset(cache);
            }
        }

        RemoveCache(cache, isUnloadTrue);
    }

    internal void _RemoveOrgObj(UnityEngine.Object orgObj, bool isUnloadAsset) {
        if (orgObj == null)
            return;

        int orgId = orgObj.GetInstanceID();
        AssetCache cache;
        if (mObjCacheMap.TryGetValue(orgId, out cache)) {
            cache.RemoveObj(orgId);
            mObjCacheMap.Remove(orgId);
        }

        if (isUnloadAsset)
            Resources.UnloadAsset(orgObj);
    }

    internal void _OnUnloadAsset(AssetCache cache, UnityEngine.Object orgObj) {
        if (cache == null || orgObj == null || orgObj is GameObject)
            return;

        if (mCacheSet.Contains(cache)) {
            cache.UnloadAsset(orgObj);
        }
    }

    public bool CacheDecRefCount(AssetCache cache, int refCount = 1, bool isUseTime = true) {
        if (cache == null)
            return false;
        if (mCacheSet.Contains(cache)) {
            bool isUsed = cache.RefCount > 0;
            cache._DecRefCount(refCount);
            if (isUsed)
                cache.LastUsedTime = GetCurrentTime();
            return true;
        }

        return false;
    }

    public bool CacheAddRefCount(AssetCache cache, int refCount = 1, bool isUseTime = true) {
        if (cache == null)
            return false;

        // 判断一下Temp, 如果有，直接删除掉
        bool hasInTemp = mTempDict.Contains(cache);
        if (hasInTemp) {
            RemoveTempAsset(cache);
        }

        if (mCacheSet.Contains(cache)) {
            cache._AddRefCount(refCount);
            if (isUseTime)
                cache.LastUsedTime = GetCurrentTime();
            return true;
        } else if (hasInTemp) {
            // 在Temp队列里
            cache._AddRefCount(refCount);
            cache.LastUsedTime = GetCurrentTime();
            mCacheSet.Add(cache);
            LinkedListNode<AssetCache> node = cache.LinkListNode;
            mUsedCacheList.AddLast(node);
            return true;
        }

        return false;
    }

    // instObj 为实例化的对象
    public void _OnDestroyGameObject(int instObjId) {
        int orgObjId;
        if (!mInstObjToObjMap.TryGetValue(instObjId, out orgObjId)) {
            //LogMgr.Instance.LogError("_OnDestroyGameObject: instObj not exists");
            return;
        }

        mInstObjToObjMap.Remove(instObjId);
        AssetCache cache;
        if (!mObjCacheMap.TryGetValue(orgObjId, out cache))
            cache = null;
        if (cache == null) {
            //LogMgr.Instance.LogError("_OnDestroyGameObject: OrgObj not exists");
            return;
        }

        CacheDecRefCount(cache);
    }

    // bu bao han list del
    private void RemoveCache(AssetCache cache, bool isUnloadTrue = true) {
        if (cache == null)
            return;

        // 通知Cache被删除
        EventDispatch.Instance.TriggerEvent<AssetCache>(_Event_CACHEDESTROY_START, cache);

        HashSet<int>.Enumerator iter;
        if (cache.GetObjEnumerator(out iter)) {
            while (iter.MoveNext()) {
                int id = iter.Current;
                mObjCacheMap.Remove(id);
            }
        }
        iter.Dispose();
        mCacheSet.Remove(cache);
        if (isUnloadTrue)
            cache.UnLoad();
        else
            cache.UnUsed();

        // 通知Cache被删除
        EventDispatch.Instance.TriggerEvent<AssetCache>(_EVENT_CACHEDESTROY_END, cache);
    }

    private bool ClearAllNotUsedList(bool checkRef) {
        bool ret = false;
        if (mNotUsedCacheList != null) {
            LinkedListNode<AssetCache> node = mNotUsedCacheList.First;
            while (node != null) {
                LinkedListNode<AssetCache> usedNode = null;
                LinkedListNode<AssetCache> delNode = null;
                if (node.Value != null) {
                    if ((!checkRef) || (node.Value.IsNotUsed())) {
                        RemoveCache(node.Value);
                        //node.Value = null;
                        delNode = node;
                    } else
                        usedNode = node;
                }
                node = node.Next;
                if (usedNode != null) {
                    mNotUsedCacheList.Remove(usedNode);
                    mUsedCacheList.AddLast(usedNode);
                }

                if (delNode != null) {
                    mNotUsedCacheList.Remove(delNode);
                    ret = true;
                }
            }

            // mNotUsedCacheList.Clear();
        }

        return ret;
    }

    // 这样操作比较危险
    private void ClearAllUsedList() {
        if (mUsedCacheList != null) {
            LinkedListNode<AssetCache> node = mUsedCacheList.First;
            while (node != null) {
                if (node.Value != null) {
                    RemoveCache(node.Value);
                    //node.Value = null;
                }
                node = node.Next;
            }

            mUsedCacheList.Clear();
        }
    }

    // 这样操作比较危险
    private void ClearAllLists() {
        ClearAllNotUsedList(false);
        ClearAllUsedList();
    }

    private void UpdateUsedList(float timer, bool limitTick = true) {
        if (mUsedCacheList != null) {
            // FirstNode 才是最久没有使用的节点
            LinkedListNode<AssetCache> node = mUsedCacheList.First;
            int cnt = mUsedCacheList.Count;
            int tick = 0;
            while (node != null) {
                if (tick >= cnt)
                    break;

                if (limitTick && (tick >= cCacheTickCount))
                    break;
                /*
				else
				if ((!limitTick) && (tick >= cnt))
					break;*/

                LinkedListNode<AssetCache> delNode = null;
                LinkedListNode<AssetCache> lastNode = null;
                // node.value不会为null
                if (node.Value != null) {
                    if (node.Value.RefCount == 0) {
                        delNode = node;
                    } else {
                        lastNode = node;
                    }
                }

                node = node.Next;

                if (delNode != null) {
                    mUsedCacheList.Remove(delNode);
                    mNotUsedCacheList.AddLast(delNode);
                } else
                if (lastNode != null) {
                    // 更新使用时间
                    lastNode.Value.LastUsedTime = timer;
                    mUsedCacheList.Remove(lastNode);
                    mUsedCacheList.AddLast(lastNode);
                }

                ++tick;
            }
        }
    }

    private void UpdateNotUsedList(float timer, bool limitTick = true) {
        if (mNotUsedCacheList != null) {
            // FirstNode 才是最久没有使用的节点
            LinkedListNode<AssetCache> node = mNotUsedCacheList.First;
            int cnt = mNotUsedCacheList.Count;
            int tick = 0;
            while (node != null) {
                if (tick >= cnt)
                    break;

                if (limitTick && (tick >= cCacheTickCount))
                    break;
                /*
				else
				if ((!limitTick) && (tick >= cnt))
					break;
					*/

                LinkedListNode<AssetCache> delNode = null;
                LinkedListNode<AssetCache> lastNode = null;
                LinkedListNode<AssetCache> usedNode = null;
                // node.value不会为null
                if (node.Value != null) {
                    if (timer - node.Value.LastUsedTime >= cCacheUnUsedTime && node.Value.IsNotUsed()) {
                        // 删除资源
                        delNode = node;
                    } else
                    if (node.Value.RefCount > 0) {
                        usedNode = node;
                    } else {
                        lastNode = node;
                    }
                }
                node = node.Next;

                if (delNode != null) {
                    RemoveCache(delNode.Value);
                    //delNode.Value = null;
                    mNotUsedCacheList.Remove(delNode);
                } else
                if (usedNode != null) {
                    mNotUsedCacheList.Remove(usedNode);
                    mUsedCacheList.AddLast(usedNode);
                } else
                if (lastNode != null) {
                    mNotUsedCacheList.Remove(lastNode);
                    mNotUsedCacheList.AddLast(lastNode);
                }

                ++tick;
            }
        }
    }

    public AssetCache FindOrgObjCache(UnityEngine.Object orgObj) {
        if (orgObj == null)
            return null;

        AssetCache ret;
        if (!mObjCacheMap.TryGetValue(orgObj.GetInstanceID(), out ret))
            ret = null;
        return ret;
    }

    public void _CheckAssetBundleCount(int addCount) {
        if (cIsCheckAssetBundleCount) {
            if (mCacheSet.Count + addCount >= cAssetBundleMaxCount) {
                ClearUnUsed();
            }
        }
    }

    private float m_MemClearTime = 0;
    private static float m_MemClearMaxTime = 10.0f;
    private bool CanClearMem() {
        return m_MemClearTime <= float.Epsilon;
    }

    private void ResetMemMaxClearTime() {
        m_MemClearTime = m_MemClearMaxTime;
    }

    private void UpdateMemClearTime(float deltTime) {
        if (m_MemClearTime - deltTime < 0)
            m_MemClearTime = 0;
        else
            m_MemClearTime -= deltTime;
    }

    private void OnTimerEvent(Timer obj, float deltTime) {
        float t = GetCurrentTime();
        UnLoadTempAssetInfo();
        UpdateNotUsedList(t);
        UpdateUsedList(t);

        // 判断当前内存使用情况，如果超过内存限制，直接清除掉 mNotUsedCacheList
        if (cIsUseCacheMemoryClear) {
            UpdateMemClearTime(deltTime);
            if (CanClearMem()) {
#if (!UNITY_EDITOR) && (!ENABLE_PROFILER)
                    uint checkMemorySize = GetCheckMemorySize ();
                    if (checkMemorySize >= cCacheMemoryLimit)
                    {
                        ResetMemMaxClearTime();
                        ClearAllNotUsedList(true);
                        // 再做一次Unload清理, 减少UnloadUnUsed坑，UnloadUnUsed+异步加载，可能会CRASH
                    //    ResourceMgr.Instance.UnloadUnUsed();
                    }
#endif
            }
        }
    }

    public float GetCurrentTime() {
        return Time.realtimeSinceStartup;
    }

    public LinkedList<AssetCache> UsedCacheList {
        get {
            return mUsedCacheList;
        }
    }

    public int UsedCacheCount {
        get {
            return mUsedCacheList.Count;
        }
    }

    public int NotUsedCacheCount {
        get {
            return mNotUsedCacheList.Count;
        }
    }

    public LinkedList<AssetCache> NotUsedCacheList {
        get {
            return mNotUsedCacheList;
        }
    }

    public void _AddTempAsset(AssetCache cache) {
        if (cache == null)
            return;
        if (!mTempDict.Contains(cache)) {
            mTempDict.Add(cache);
            mTempAssetList.AddLast(cache.LinkListNode);
        }
    }

    private void RemoveTempAsset(AssetCache cache) {
        if (cache == null)
            return;
        if (mTempDict.Contains(cache)) {
            mTempDict.Remove(cache);
            mTempAssetList.Remove(cache.LinkListNode);
        }
    }

    // 资源更新清理
    public void AutoUpdateClear(bool isUnloadTrue = false) {
        LinkedListNode<AssetCache> first = mTempAssetList.First;
        while (first != null) {
            if (first.Value != null) {
                if (isUnloadTrue)
                    first.Value.UnLoad();
                else
                    first.Value.UnUsed();
            }
            first = first.Next;
        }
        mTempAssetList.Clear();
        mTempDict.Clear();

        first = mNotUsedCacheList.First;
        while (first != null) {
            if (first.Value != null) {
                if (isUnloadTrue)
                    first.Value.UnLoad();
                else
                    first.Value.UnUsed();
            }
            first = first.Next;
        }
        mNotUsedCacheList.Clear();

        first = mUsedCacheList.First;
        while (first != null) {
            if (first.Value != null) {
                if (isUnloadTrue)
                    first.Value.UnLoad();
                else
                    first.Value.UnUsed();
            }
            first = first.Next;
        }
        mUsedCacheList.Clear();

        mObjCacheMap.Clear();
        mInstObjToObjMap.Clear();
        mCacheSet.Clear();
    }

    // 总的Bundle数量
    public int AllBunldeCnt {
        get {
            return mUsedCacheList.Count + mNotUsedCacheList.Count;
        }
    }

    private void UnLoadTempAssetInfo() {
        var node = mTempAssetList.First;
        while (node != null) {
            bool isRemove = (node.Value != null) && node.Value.IsNotUsed();
            if (isRemove)
                RemoveCache(node.Value);

            //    isRemove = isRemove || node.Value == null;
            var delNode = node;
            node = node.Next;
            //  if (isRemove)
            RemoveTempAsset(delNode.Value);
        }
    }

    private static readonly string _Event_CACHEDESTROY_START = "OnCacheStartDestroy";
    private static readonly string _EVENT_CACHEDESTROY_END = "OnCacheEndDestroy";

    // 未使用列表
    private LinkedList<AssetCache> mNotUsedCacheList = new LinkedList<AssetCache>();
    // 使用列表
    private LinkedList<AssetCache> mUsedCacheList = new LinkedList<AssetCache>();
    // Cache查找（包括Used和NotUsed）
    private HashSet<AssetCache> mCacheSet = new HashSet<AssetCache>();
    // Prefab Object
    // GameObject对应的AssetCache，在Resources里的资源是一对一，而AssetBundle里的资源是一对多(Key: GetInstanceID)
    private Dictionary<int, AssetCache> mObjCacheMap = new Dictionary<int, AssetCache>();
    // Instance Obj to PrefabObj
    private Dictionary<int, int> mInstObjToObjMap = new Dictionary<int, int>();
    private ITimer mRunTime = null;
    // ResourceCacheType == none
    private LinkedList<AssetCache> mTempAssetList = new LinkedList<AssetCache>();
    private HashSet<AssetCache> mTempDict = new HashSet<AssetCache>();
}