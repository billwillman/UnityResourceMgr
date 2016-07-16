using UnityEngine;
using System.Collections;
using Utils;

public class Main : CachedMonoBehaviour {

	void Awake()
	{
		if (!m_Inited)
		{
			DontDestroyOnLoad(CachedGameObject);
			m_Inited = true;
		}
	}

	void Start()
	{
		GameMgr.Instance.OnAppEnter();
	}

	void OnLevelWasLoaded (int level)
	{
		GameMgr.Instance.OnSceneLoad(level);
	}

	void Update()
	{
		GameMgr.Instance.OnUpdate();
	}

	void OnApplicationQuit()
	{
		GameMgr.Instance.OnAppExit();
	}
	
	private bool m_Inited = false;
}
