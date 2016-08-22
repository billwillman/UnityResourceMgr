using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[CustomEditor(typeof(ResourceMgrMonitor))]
public class ResourceMgrMonitorEditor: Editor
{

	private void DrawCacheMap(Dictionary<string, int> map, string labelName)
	{
		if ((map != null) && (map.Count > 0)) {
			EditorGUILayout.Space();
			EditorGUILayout.LabelField (labelName);
			
			bool hasBundleRes = false;
			// 先绘制Bundle资源
			var node = map.GetEnumerator();
			while (node.MoveNext()) {
				string name = node.Current.Key;
				
				bool isBundleRes = name.StartsWith("Bundle");
				if (isBundleRes)
				{
					EditorGUILayout.LabelField(name);
					EditorGUILayout.IntField(node.Current.Value);
					hasBundleRes = true;
				}
			}
			node.Dispose();
			
			if (hasBundleRes)
			{
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}
			
			// 再绘制非Bundle的
			node = map.GetEnumerator();
			while (node.MoveNext())
			{
				string name = node.Current.Key;
				
				bool isBundleRes = name.StartsWith("Bundle");
				if (!isBundleRes)
				{
					EditorGUILayout.LabelField(name);
					EditorGUILayout.IntField(node.Current.Value);
				}
			}
			node.Dispose();
		}
		
	}

	// changed is return true
	bool UpdateAssetRefMap()
	{
		bool ret = false;
		// used list
		var list = AssetCacheManager.Instance.UsedCacheList;

		if (list.Count != mUsedAssetRefMap.Count)
			ret = true;

		HashSet<string> hash = new HashSet<string> ();
		HashSet<string> removeHash = new HashSet<string> ();

		var node = list.First;
		while (node != null) {
			AssetCache cache = node.Value;
			if (cache != null)
			{
				AssetBundleCache bundleCache = cache as AssetBundleCache;
				if ((bundleCache != null) && (bundleCache.Target != null))
				{
					string key = GetBundleKey(bundleCache.Target.FileName);
					hash.Add(key);
					if (mUsedAssetRefMap.ContainsKey(key))
					{
						if (mUsedAssetRefMap[key] != cache.RefCount)
						{
							mUsedAssetRefMap[key] = cache.RefCount;
							ret = true;
						}
					}
					else
					{
						mUsedAssetRefMap.Add(key, cache.RefCount);
						ret = true;
					}
				} else
				{
					ResourceAssetCache resCache = cache as ResourceAssetCache;
					if ((resCache != null) && (resCache.Target != null))
					{
						string key = string.Format("Res:{0}", resCache.Target.name);
						hash.Add(key);
						if (mUsedAssetRefMap.ContainsKey(key))
						{
							if (mUsedAssetRefMap[key] != cache.RefCount)
							{
								mUsedAssetRefMap[key] = cache.RefCount;
								ret = true;
							}
						}
						else
						{
							mUsedAssetRefMap.Add(key, cache.RefCount);
							ret = true;
						}
					}
				}
			}
			node = node.Next;
		}

		var iter = mUsedAssetRefMap.GetEnumerator ();
		while (iter.MoveNext()) {
			string key = iter.Current.Key;
			if (!hash.Contains(key))
				removeHash.Add(key);
		}
        iter.Dispose();

		var removeIter = removeHash.GetEnumerator ();
		while (removeIter.MoveNext()) {
			mUsedAssetRefMap.Remove(removeIter.Current);
		}
        removeIter.Dispose();
        hash.Clear ();
		removeHash.Clear ();

		// not used list
		list = AssetCacheManager.Instance.NotUsedCacheList;

		if (list.Count != mNotUsedAssetRefMap.Count)
			ret = true;

		node = list.First;
		while (node != null) {
			AssetCache cache = node.Value;
			if (cache != null)
			{
				AssetBundleCache bundleCache = cache as AssetBundleCache;
				if ((bundleCache != null) && (bundleCache.Target != null))
				{
					string key = GetBundleKey(bundleCache.Target.FileName);
					hash.Add(key);
					if (mNotUsedAssetRefMap.ContainsKey(key))
					{
						if (mNotUsedAssetRefMap[key] != cache.RefCount)
						{
							mNotUsedAssetRefMap[key] = cache.RefCount;
							ret = true;
						}
					}
					else
					{
						mNotUsedAssetRefMap.Add(key, cache.RefCount);
						ret = true;
					}
				} else
				{
					ResourceAssetCache resCache = cache as ResourceAssetCache;
					if ((resCache != null) && (resCache.Target != null))
					{
						string key = string.Format("Res:{0}", resCache.Target.name);
						hash.Add(key);
						if (mNotUsedAssetRefMap.ContainsKey(key))
						{
							if (mNotUsedAssetRefMap[key] != cache.RefCount)
							{
								mNotUsedAssetRefMap[key] = cache.RefCount;
								ret = true;
							}
						}
						else
						{
							mNotUsedAssetRefMap.Add(key, cache.RefCount);
							ret = true;
						}
					}
				}
			}
			node = node.Next;
		}

		iter = mNotUsedAssetRefMap.GetEnumerator ();
		while (iter.MoveNext()) {
			string key = iter.Current.Key;
			if (!hash.Contains(key))
				removeHash.Add(key);
		}
        iter.Dispose();

		removeIter = removeHash.GetEnumerator ();
		while (removeIter.MoveNext()) {
			mNotUsedAssetRefMap.Remove(removeIter.Current);
		}
        removeIter.Dispose();

        hash.Clear ();
		removeHash.Clear ();

		return ret;
	}

	void OnEnable()
	{
		EditorApplication.update += RefMapUpdate;
	}

	void OnDisable()
	{
		EditorApplication.update -= RefMapUpdate;
	}

	void RefMapUpdate()
	{
		if (!Application.isPlaying)
			return;

		float curTime = Time.unscaledTime;
		m_IsUPdateData = curTime - m_LastUpdateTime > 0.5f;
		
		if (m_IsUPdateData)
			m_LastUpdateTime = curTime;

			if (m_IsUPdateData) {
				bool isChg = UpdateAssetRefMap ();
				if (isChg)
					this.Repaint ();
			}
	}

	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector ();

		if (Application.isPlaying) {

			DrawSearchTarget();

			// 正在使用列表
			DrawCacheMap(mUsedAssetRefMap, "正在使用列表");

			// 非使用列表
			DrawCacheMap(mNotUsedAssetRefMap, "未使用列表");

			DrawBtnClearNoUsed();
		}
	}

	void DrawBtnClearNoUsed()
	{
		if (GUILayout.Button("清空未使用Cache"))
		{
			AssetCacheManager.Instance.ClearUnUsed();
		}
	}

	string GetBundleKey(string fileName)
	{
		fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
		return string.Format("Bundle:{0}", fileName);
	}

	private void DrawSearchTarget()
	{
		EditorGUILayout.Space();
		UnityEngine.GameObject newTarget = EditorGUILayout.ObjectField("搜索GameObject资源：", mShowTarget, typeof(GameObject), true) as GameObject;
		if (mShowTarget != newTarget)
		{
			mShowTarget = newTarget;
		}
		
		if (mShowTarget != null)
		{
			EditorGUILayout.Space();
			AssetCache cache = AssetCacheManager.Instance.FindInstGameObjectCache(mShowTarget);
			if (cache != null)
			{
				EditorGUILayout.TextField(cache.RefCount.ToString());
				
				AssetBundleCache bundleCache = cache as AssetBundleCache;
				if (bundleCache != null)
				{
					AssetInfo assetInfo = bundleCache.Target;
					if (assetInfo != null)
					{
						for (int i = 0; i < assetInfo.DependFileCount; ++i)
						{
							string dependFileName = assetInfo.GetDependFileName(i);
							if (!string.IsNullOrEmpty(dependFileName))
							{
								AssetLoader loader = ResourceMgr.Instance.AssetLoader as AssetLoader;
								if (loader != null)
								{
									AssetInfo dependAssetInfo = loader.FindAssetInfo(dependFileName);
									if (dependAssetInfo != null && dependAssetInfo.Cache != null)
									{
										string dependKey = GetBundleKey(dependFileName);
										EditorGUILayout.LabelField(dependKey);
										EditorGUILayout.TextField(dependAssetInfo.Cache.RefCount.ToString());
									}
								}
							}
						}
					}
				}
				
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}
		}
	}

	private Dictionary<string, int> mUsedAssetRefMap = new Dictionary<string, int>();
	private Dictionary<string, int> mNotUsedAssetRefMap = new Dictionary<string, int>();
	static private GameObject mShowTarget = null;
	private float m_LastUpdateTime = 0;
	private bool m_IsUPdateData = false;
}