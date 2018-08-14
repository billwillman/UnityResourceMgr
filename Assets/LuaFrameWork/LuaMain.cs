using UnityEngine;
using System.Collections;

// Lua Start
public class LuaMain : MonoBehaviour {
	void Awake()
	{
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

	void OnResourceConfigFinish(bool isOk)
	{
		var gameMgr = GetComponent<LuaGameMgr>();
		if (gameMgr == null)
		{
			gameMgr = gameObject.AddComponent<LuaGameMgr>();
		}
		m_GameMgr = gameMgr;
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
