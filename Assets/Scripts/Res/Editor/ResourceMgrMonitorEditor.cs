using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NsHttpClient;

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
				
				bool isBundleRes = name.StartsWith("Bundle:");
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
		m_IsUPdateData = curTime - m_LastUpdateTime > 0.25f;
		
		if (m_IsUPdateData)
			m_LastUpdateTime = curTime;

			if (m_IsUPdateData) {
				bool isChg = UpdateAssetRefMap ();
				
			int cnt = TimerMgr.Instance.TimerPoolCount;
			if (cnt != m_LastTimeCnt)
			{
				m_LastTimeCnt = cnt;
				isChg = true;
			}

			cnt = ResourceAssetCache.GetPoolCount();
			if (cnt != m_LastResCacheCnt)
			{
				m_LastResCacheCnt = cnt;
				isChg = true;
			}

			cnt = AssetBundleCache.GetPoolCount();
			if (cnt != m_LastBundleCacheCnt)
			{
				m_LastBundleCacheCnt = cnt;
				isChg = true;
			}

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            cnt = BundleCreateAsyncTask.GetPoolCount();
			if (cnt != m_LastBundleCreateCnt)
			{
				m_LastBundleCreateCnt = cnt;
				isChg = true;
			}
			#endif

			cnt = WWWFileLoadTask.GetPoolCount();
			if (cnt != m_LastWWWCreateCnt)
			{
				m_LastWWWCreateCnt = cnt;
				isChg = true;
			}

			cnt = HttpHelper.RunCount;
			if (cnt != m_LastRunHttpCnt)
			{
				m_LastRunHttpCnt = cnt;
				isChg = true;
			}

			cnt = HttpHelper.PoolCount;
			if (cnt != m_LastHttpPoolCnt)
			{
				m_LastHttpPoolCnt = cnt;
				isChg = true;
			}

				if (isChg)
					this.Repaint ();
			}
	}

	void DrawObjectPoolInfos()
	{
		EditorGUILayout.Space();
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("對象池列表");

		EditorGUILayout.LabelField("TimerMgr Pool");
		EditorGUILayout.IntField(m_LastTimeCnt);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("ResourcesCache Pool");
		EditorGUILayout.IntField(m_LastResCacheCnt);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("AssetBundleCache Pool");
		EditorGUILayout.IntField(m_LastBundleCacheCnt);

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        EditorGUILayout.Space();
		EditorGUILayout.LabelField("BundleCreateAsyncTask Pool");
		EditorGUILayout.IntField(m_LastBundleCreateCnt);
		#endif

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("WWWFileLoadTask Pool");
		EditorGUILayout.IntField(m_LastWWWCreateCnt);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("当前HTTP连接数量");
		EditorGUILayout.IntField(m_LastRunHttpCnt);
		EditorGUILayout.LabelField("Http Pool数量");
		EditorGUILayout.IntField(m_LastHttpPoolCnt);

		EditorGUILayout.Space();
		EditorGUILayout.Space();
	}

    void DrawBundleCnt() {
        EditorGUILayout.Space();
        EditorGUILayout.IntField("资源Assets总数量", AssetCacheManager.Instance.AllBunldeCnt);
    }

    public override void OnInspectorGUI ()
	{
		DrawDefaultInspector ();

		if (Application.isPlaying) {

            DrawSearchTarget();

			DrawObjectPoolInfos();

            DrawBundleCnt();

            // 正在使用列表
            DrawCacheMap(mUsedAssetRefMap, "正在使用列表");

			// 非使用列表
			DrawCacheMap(mNotUsedAssetRefMap, "未使用列表");

			DrawBtnClearNoUsed();
		} else {
            m_LoadMd5FindFile = false;
        }
	}

	void DrawBtnClearNoUsed()
	{
		if (GUILayout.Button("清空未使用Cache"))
		{
			AssetCacheManager.Instance.ClearUnUsed();
		}

		if (GUILayout.Button("UnloadUsed"))
		{
			ResourceMgr.Instance.UnloadUnUsed();
		}
	}

	string GetBundleKey(string fileName)
	{
		fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);

        LoadMd5FindFile();
        if (m_Md5FindMap.Count > 0) {
            string realFileName;
            if (m_Md5FindMap.TryGetValue(fileName, out realFileName))
                fileName = realFileName;
        }

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

    private static void LoadMd5FindFile() {
        if (m_LoadMd5FindFile)
            return;
        m_LoadMd5FindFile = true;
        string fileName = "Assets/md5Find.txt";
        if (System.IO.File.Exists(fileName)) {
            System.IO.FileStream stream = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            if (stream.Length > 0) {
                byte[] buf = new byte[stream.Length];
                stream.Read(buf, 0, buf.Length);
                string s = System.Text.Encoding.ASCII.GetString(buf);
                List<string> lines = new List<string>(s.Split('\r'));
                for (int i = 0; i < lines.Count; ++i) {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    int idx = line.IndexOf('=');
                    if (idx > 0) {
                        string left = line.Substring(0, idx).Trim();
                        if (string.IsNullOrEmpty(left))
                            continue;
                        string right = line.Substring(idx + 1).Trim();
                        if (string.IsNullOrEmpty(right))
                            continue;
                        m_Md5FindMap.Add(left, right);
                    }
                }
            }
            stream.Dispose();
            stream.Close();
        }
    }

    private static bool m_LoadMd5FindFile = false;
    private static Dictionary<string, string> m_Md5FindMap = new Dictionary<string, string>();

    private Dictionary<string, int> mUsedAssetRefMap = new Dictionary<string, int>();
	private Dictionary<string, int> mNotUsedAssetRefMap = new Dictionary<string, int>();
	static private GameObject mShowTarget = null;
	private float m_LastUpdateTime = 0;
	private bool m_IsUPdateData = false;

	private int m_LastTimeCnt = 0;
	private int m_LastResCacheCnt = 0;
	private int m_LastBundleCacheCnt = 0;
	private int m_LastBundleCreateCnt = 0;
	private int m_LastWWWCreateCnt = 0;
	private int m_LastRunHttpCnt = 0;
	private int m_LastHttpPoolCnt = 0;
}