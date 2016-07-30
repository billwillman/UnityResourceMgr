using UnityEngine;
using System.Collections;
using Utils;

public class Test : CachedMonoBehaviour {

	void OnGUI()
	{
		if (GameMgr.Instance.IsConfigLoaded)
		{
			DrawButtons();
			DrawSpritesButton();
			DrawTextureButton();
		}
	}

	void DrawTextureButton()
	{
		if (!m_IsLoadTextureLoading)
		{
			if (m_Texture == null)
			{
				if (GUI.Button(new Rect(100, 220, 150, 50), "加载测试Texture"))
				{
					m_IsLoadTextureLoading = true;

					if (!ResourceMgr.Instance.LoadTextureAsync("resources/models/@flag/m_items_01.psd",
					delegate (float process, bool isDone, Texture obj)
					{
						if (isDone)
						{
							if (obj != null)
								m_Texture = obj;
							m_IsLoadTextureLoading = false;
						}
					}, ResourceCacheType.rctRefAdd))
						m_IsLoadTextureLoading = false;
				}
			} else
			{
				if (GUI.Button(new Rect(100, 220, 150, 50), "删除测试Texture"))
				{
					ResourceMgr.Instance.DestroyObject(m_Texture);
					m_Texture = null;
					m_IsLoadTextureLoading = false;
				}
			}
		}
	}

	void DrawSpritesButton()
	{
		if (!m_IsSpritesLoading)
		{
			if (m_Sprites == null)
			{
				if (GUI.Button(new Rect(260, 220, 150, 50), "加载测试Sprites"))
				{
					m_IsSpritesLoading = true;
					if (!ResourceMgr.Instance.LoadSpritesAsync("resources/models/@flag/m_hadry_01.psd", 
					                                           delegate (float process, bool isDone, Sprite[] objs) {
						if (isDone)
						{
							if (objs != null)
								m_Sprites = objs;
							m_IsSpritesLoading = false;
						}
						
					}, ResourceCacheType.rctRefAdd))
						m_IsSpritesLoading = false;
				}
			} else
			{
				if (GUI.Button(new Rect(260, 220, 150, 50), "删除测试Sprites"))
				{
					ResourceMgr.Instance.DestroySprites(m_Sprites);
					m_Sprites = null;
					m_IsSpritesLoading = false;
				}
			}
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

	private System.Collections.Generic.List<GameObject> m_ObjList = new System.Collections.Generic.List<GameObject>();
	private string m_CurScene = string.Empty;
	private Sprite[] m_Sprites = null;
	private bool m_IsSpritesLoading = false;
	private bool m_IsLoadTextureLoading = false;
	private Texture m_Texture = null;
}
