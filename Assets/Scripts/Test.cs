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

		if (m_CubeObj == null)
		{
			if (GUI.Button(new Rect(100, 160, 150, 50), "(同步)创建Prefab物体"))
			{
				m_CubeObj = ResourceMgr.Instance.CreateGameObject("resources/prefabs/flag.prefab");
			}

			if (GUI.Button(new Rect(260, 160, 150, 50), "(异步)创建Prefab物体"))
			{
				ResourceMgr.Instance.CreateGameObjectAsync("resources/prefabs/flag.prefab",
				   delegate (float process, bool isDone, GameObject obj){
						if (isDone)
							m_CubeObj = obj;
					}
				);
			}
		} else
		{
			if (GUI.Button(new Rect(100, 160, 150, 50), "删除Prefab物体"))
			{
				ResourceMgr.Instance.DestroyObject(m_CubeObj);
				m_CubeObj = null;
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

	private GameObject m_CubeObj = null;
	private string m_CurScene = string.Empty;
}
