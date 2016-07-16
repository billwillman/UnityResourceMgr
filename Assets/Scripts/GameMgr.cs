using System;
using Utils;
using AutoUpdate;
using UnityEngine;

public class GameMgr: Singleton<GameMgr>
{
	public void OnAppEnter()
	{
		ResourceMgr.Instance.LoadConfigs(OnResConfigLoad);
	}

	void OnResConfigLoad(bool isOk)
	{
		// 读取 配置完毕
	}

	public void OnAppExit()
	{
	}

	public void OnSceneLoad(int level)
	{
	}

	public void OnUpdate()
	{
		TimerMgr.Instance.ScaleTick(Time.deltaTime);
		TimerMgr.Instance.UnScaleTick(Time.unscaledDeltaTime);
	}

	public Main Main
	{
		get
		{
			if (m_Main == null)
			{
				m_Main = GameObject.FindObjectOfType<Main>();
			}
			
			return m_Main;
		}
	}

	private Main m_Main = null;
}

