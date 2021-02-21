#if UNITY_2017_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ABRuntimeShow : EditorWindow
{

	private struct ABInfo
	{
		public string name;
		public string[] assetNames;
		public string[] scenePaths;
		public bool isNoDepend;

		public string editorTitle {
			get {
				if (isNoDepend)
					return "【缓存】" + name;
				return name;
			}
		}

		private bool IsContainsScenePath (string path)
		{
			if (string.IsNullOrEmpty (path) || scenePaths == null || scenePaths.Length <= 0)
				return false;
			for (int i = 0; i < scenePaths.Length; ++i) {
				if (string.Compare (path, scenePaths [i]) == 0)
					return true;
			}
			return false;
		}

		private bool IsContainsAssetNames (string name)
		{
			if (string.IsNullOrEmpty (name) || assetNames == null || assetNames.Length <= 0)
				return false;
			for (int i = 0; i < assetNames.Length; ++i) {
				if (string.Compare (name, assetNames [i]) == 0)
					return true;
			}
			return false;
		}

		private void AddDepPathList (HashSet<string> lst, string inputPath)
		{
			if (lst == null || string.IsNullOrEmpty (inputPath))
				return;
			var lowerPath = inputPath.ToLower ();
			if (lst.Contains (lowerPath))
				return;

			string[] deps = AssetDatabase.GetDependencies (inputPath, false);
			if (deps != null && deps.Length > 0) {
				for (int i = 0; i < deps.Length; ++i) {
					var dep = deps [i];
					if (string.IsNullOrEmpty (dep) || AssetBundleBuild.FileIsScript(dep))
						continue;
					if (string.Compare(inputPath, dep) != 0)
						AddDepPathList (lst, dep);
				}
			}

			lst.Add (lowerPath);
		}

		private void AddDepDynCreate(HashSet<string> lst, UnityEngine.Object dynObj)
		{
			if (lst == null || dynObj == null)
				return;
			
		}

		private void AddDepPathList (HashSet<string> lst, UnityEngine.Object obj)
		{
			if (lst == null || obj == null)
				return;
			string path = AssetDatabase.GetAssetPath (obj);
			if (string.IsNullOrEmpty (path)) {
				// 处理动态new出来obj的情况
				AddDepDynCreate(lst, obj);
				return;
			}
			AddDepPathList (lst, path);
		}

		private void AddDepPathList (HashSet<string> lst, UnityEngine.GameObject obj)
		{
			if (lst == null || obj == null)
				return;
			
			var trans = obj.transform;
			for (int i = 0; i < trans.childCount; ++i) {
				var childTrans = trans.GetChild (i);
				AddDepPathList (lst, childTrans.gameObject);
			}

			UnityEngine.Object o = (UnityEngine.Object)obj;
			AddDepPathList (lst, o);
		}

		private void AddDepPathList (HashSet<string> lst, UnityEngine.GameObject[] objs)
		{
			if (lst == null || objs == null || objs.Length <= 0)
				return;
			for (int i = 0; i < objs.Length; ++i) {
				var obj = objs [i];
				if (obj == null)
					continue;
				AddDepPathList (lst, obj);
			}
		}

		public static List<Scene> GetCurrentScenes ()
		{
			List<Scene> ret = new List<Scene> ();
			for (int i = 0; i < SceneManager.sceneCount; ++i) {
				Scene scene = SceneManager.GetSceneAt (i);
				if (scene.isLoaded && scene.IsValid ())
					ret.Add (scene);
			}
			return ret;
		}

		public void CheckDep (List<Scene> scenes)
		{
			isNoDepend = true;
			HashSet<string> rets = new HashSet<string> ();
			for (int i = 0; i < scenes.Count; ++i) {
				Scene scene = scenes [i];
				string scenePath = scene.path.ToLower ();
				if (IsContainsScenePath (scenePath)) {
					isNoDepend = false;
					return;
				}

				GameObject[] gameObjs = scene.GetRootGameObjects ();
				AddDepPathList (rets, gameObjs);


				var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset> (scene.path);
				AddDepPathList (rets, sceneAsset);

			}

			var iter = rets.GetEnumerator ();
			while (iter.MoveNext ()) {
				if (IsContainsAssetNames (iter.Current)) {
					isNoDepend = false;
					return;
				}
			}
			iter.Dispose ();
		}
	}

	private Vector2 scrollPos = Vector2.zero;

	private List<ABInfo> abList = new List<ABInfo> ();

	[MenuItem ("Tools/显示运行时AssetBundle")]
	public static void ShowWindow ()
	{
		const float w = 600;
		const float h = 600;
		Rect r = new Rect ((Screen.width - w) / 2.0f, (Screen.height - h) / 2.0f, w, h);
		ABRuntimeShow wnd = EditorWindow.GetWindowWithRect<ABRuntimeShow> (r, true);
		wnd.Init ();
	}

	void RefreshAssetBundleList ()
	{
		abList.Clear ();
		var bundles = AssetBundle.GetAllLoadedAssetBundles ();
		if (bundles == null)
			return;
		EditorUtility.ClearProgressBar ();
		try {
			float idx = 0;
			float cnt = 0;

			var iter = bundles.GetEnumerator ();
			while (iter.MoveNext ()) {
				cnt += 1f;
			}
			iter.Dispose ();

			var scenes = ABInfo.GetCurrentScenes ();

			iter = bundles.GetEnumerator ();
			while (iter.MoveNext ()) {
				AssetBundle ab = iter.Current;
				if (ab == null)
					continue;
				++idx;
				EditorUtility.DisplayProgressBar (string.Format("运行时检查AB冗余加载（{0:D}/{1:D}）", Mathf.RoundToInt(idx), Mathf.RoundToInt(cnt)), ab.name, idx / cnt);
				ABInfo info = new ABInfo ();
				info.name = ab.name;
				info.isNoDepend = true;
				info.assetNames = ab.GetAllAssetNames ();
				if (info.assetNames != null) {
					for (int i = 0; i < info.assetNames.Length; ++i) {
						string s = info.assetNames [i];
						info.assetNames [i] = s.ToLower ();
					}
				}
				info.scenePaths = ab.GetAllScenePaths ();
				if (info.scenePaths != null) {
					for (int i = 0; i < info.scenePaths.Length; ++i) {
						string s = info.scenePaths [i];
						info.scenePaths [i] = s.ToLower ();
					}
				}

				info.CheckDep (scenes);
				abList.Add (info);
			}
			iter.Dispose ();
		} finally {
			EditorUtility.ClearProgressBar ();
		}

		// 排序
		abList.Sort ((ABInfo a, ABInfo b) => {
			if (a.isNoDepend == b.isNoDepend)
				return string.Compare (a.name, b.name);
			if (!a.isNoDepend)
				return 1;
			return -1;
		}
		);
	}

	private void Init ()
	{
		//RefreshAssetBundleList ();
	}

	void OnGUI ()
	{
		string btnTile = string.Format ("刷新AssetBundle（{0:D}）", abList.Count);
		if (GUILayout.Button (btnTile)) {
			RefreshAssetBundleList ();
		}
		var oldColor = GUI.color;
		scrollPos = EditorGUILayout.BeginScrollView (scrollPos, true, true);
		for (int i = 0; i < abList.Count; ++i) {
			var info = abList [i];
			EditorGUILayout.BeginHorizontal ();
	
			if (info.isNoDepend) {
				GUI.color = Color.red;
			} else {
				GUI.color = Color.green;
			}
			EditorGUILayout.LabelField (info.editorTitle);
			EditorGUILayout.EndHorizontal ();
		}
		EditorGUILayout.EndScrollView ();
		GUI.color = oldColor;
	}
}

#endif
