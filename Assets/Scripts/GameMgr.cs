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

	void OnTestStart()
	{
		m_IsConfigLoaded = true;
	}

	void OnResConfigLoad(bool isOk)
	{
		// 读取 配置完毕
	//	if (isOk)
		{
			// 预先加载 Shader的AB, 只需要提供其中一个的文件名即可，自动找到AB，并加载
			if (!ResourceMgr.Instance.PreLoadAndBuildAssetBundleShaders("resources/@shaders/lightmap-unlit-wind.shader",
																   OnTestStart))
				OnTestStart();
		}
	}

	public void OnAppExit()
	{
        NsHttpClient.HttpHelper.OnAppExit();
        ResourceMgr.Instance.OnAppExit();
	}

	public void OnSceneLoad(int level)
	{
		AssetCacheManager.Instance.ClearUnUsed();
		ResourceMgr.Instance.UnloadUnUsed();
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

