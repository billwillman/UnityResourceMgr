using System;
using Utils;
using AutoUpdate;
using UnityEngine;

public class GameMgr: Singleton<GameMgr>
{
	public void OnAppEnter()
	{
		m_IsConfigLoaded = false;
		ResourceMgr.Instance.LoadConfigs(OnResConfigLoad);
	}

	void OnResConfigLoad(bool isOk)
	{
		// 读取 配置完毕
		m_IsConfigLoaded = true;
		if (isOk)
		{
			// 预先加载 Shader的AB, 只需要提供其中一个的文件名即可，自动找到AB，并加载
			ResourceMgr.Instance.PreLoadAndBuildAssetBundleShaders("resources/@shaders/lightmap-unlit-wind.shader");
		}
	}

	public void OnAppExit()
	{
	}

	public void OnSceneLoad(int level)
	{
	}

	public bool IsConfigLoaded
	{
		get
		{
			return m_IsConfigLoaded;
		}
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
	private bool m_IsConfigLoaded = false;
}

