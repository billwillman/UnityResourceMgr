using UnityEngine;
using System.Collections;

// Lua Start
public class LuaMain : MonoBehaviour {

	private static LuaMain m_Main = null;
	public static LuaMain Main
	{
		get
		{
			return m_Main;
		}
	}

	void Awake()
	{
		m_Main = this;
		InitResources();
	}

	void Update()
	{
		UpdateTimerMgr();
	}

	/// <summary>
	/// 更新定时器 
	/// </summary>
	void UpdateTimerMgr()
	{
		// 定时器更新
		TimerMgr.Instance.ScaleTick(Time.deltaTime);
		TimerMgr.Instance.UnScaleTick(Time.unscaledTime);
	}

	public void EnterLuaGame()
	{
		var gameMgr = GetComponent<LuaGameMgr>();
		if (gameMgr == null)
		{
			gameMgr = gameObject.AddComponent<LuaGameMgr>();
		}
		m_GameMgr = gameMgr;
	}

	void OnResourceConfigFinish(bool isOk)
	{
		DownloadCheck.ShowDownload();	
	}

	public LuaGameMgr GameMgr
	{
		get
		{
			return m_GameMgr;
		}
	}

	// 初始化资源AssetBundle
	void InitResources()
	{
		ResourceMgr.Instance.LoadConfigs(OnResourceConfigFinish, this, true);
	}

	private static LuaGameMgr m_GameMgr = null;
}
