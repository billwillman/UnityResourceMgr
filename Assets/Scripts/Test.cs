using UnityEngine;
using System.Collections;
using Utils;

public class Test : CachedMonoBehaviour {

	void OnGUI()
	{
		if (GameMgr.Instance.IsConfigLoaded)
		{
			DrawButtons();
		}
	}

	void DrawButtons()
	{
		if (GUI.Button(new Rect(100, 100, 150, 50), "(同步)切换场景"))
		{
			ChangeScene(false);
		}

		if (GUI.Button(new Rect(260, 100, 150, 50), "(异步)切换场景"))
		{
			ChangeScene(true);
		}

		/*
			if (GUI.Button(new Rect(100, 160, 150, 50), "(同步)创建Prefab物体"))
			{
				GameObject obj = ResourceMgr.Instance.CreateGameObject("resources/prefabs/flag.prefab");
				if (obj != null)
					m_ObjList.Add(obj);
			}
			*/

			if (GUI.Button(new Rect(260, 160, 150, 50), "(异步)创建Prefab物体"))
			{
				ResourceMgr.Instance.CreateGameObjectAsync("resources/prefabs/flag.prefab",
				   delegate (float process, bool isDone, GameObject obj){
						if (isDone && obj != null)
							m_ObjList.Add(obj);
					}
				);
			}

		if (m_ObjList.Count > 0)
		{
			if (GUI.Button(new Rect(100, 160, 150, 50), "删除Prefab物体"))
			{
				var obj = m_ObjList[m_ObjList.Count - 1];
				ResourceMgr.Instance.DestroyObject(obj);
				m_ObjList.RemoveAt(m_ObjList.Count - 1);
			}
		}
	}

	void ChangeScene(bool isAsync)
	{
		if (!string.IsNullOrEmpty(m_CurScene))
			ResourceMgr.Instance.CloseScene(m_CurScene);
		m_CurScene = "1";
		if (isAsync)
			ResourceMgr.Instance.LoadSceneAsync(m_CurScene, false, null);
		else
			ResourceMgr.Instance.LoadScene(m_CurScene, false);

	}

	void OnLevelWasLoaded (int level)
	{
		AssetCacheManager.Instance.ClearUnUsed();
		ResourceMgr.Instance.UnloadUnUsed();
	}

	private System.Collections.Generic.List<GameObject> m_ObjList = new System.Collections.Generic.List<GameObject>();
	private string m_CurScene = string.Empty;
}
