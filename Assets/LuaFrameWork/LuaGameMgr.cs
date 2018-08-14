using System;
using UnityEngine;
using LuaInterface;

public class LuaGameMgr: MonoBehaviour
{

	private LuaState m_LuaState = null;
	private LuaLooper m_LuaLoop = null;
	private LuaLoader m_LuaLoader = null;

	/// <summary>
	/// 初始化LUA环境
	/// </summary>
	void InitLua()
	{
		m_LuaState = new LuaState();
		OpenLibs();
		if (m_LuaLoader == null)
			m_LuaLoader = new LuaLoader();
		m_LuaState.LuaSetTop(0);
		LuaBinder.Bind(m_LuaState);
		DelegateFactory.Init();
		LuaCoroutine.Register(m_LuaState, this);
	}

	/// <summary>
	/// 注册的C库
	/// </summary>
	void OpenLibs()
	{
		if (m_LuaState != null)
		{
			m_LuaState.OpenLibs(LuaDLL.luaopen_pb);      
			m_LuaState.OpenLibs(LuaDLL.luaopen_sproto_core);
			m_LuaState.OpenLibs(LuaDLL.luaopen_protobuf_c);
			m_LuaState.OpenLibs(LuaDLL.luaopen_lpeg);
			m_LuaState.OpenLibs(LuaDLL.luaopen_bit);
			m_LuaState.OpenLibs(LuaDLL.luaopen_socket_core);
		}
		OpenCJson();
	}

	void Start()
	{
		InitLua();
		//暂时放在这里，后面用更新的
		InitStart();
	}

	/// <summary>
	/// Lua环境注销
	/// </summary>
	private void UnInitLua()
	{
		if (m_LuaLoop != null)
		{
			m_LuaLoop.Destroy();
			m_LuaLoop = null;
		}

		if (m_LuaState != null)
		{
			m_LuaState.Dispose();
			m_LuaState = null;
		}

		m_LuaLoader = null;
	}

	void OnDestroy()
	{
		UnInitLua();
	}

	public void DoFile(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return;
		if (m_LuaState != null)
			m_LuaState.DoFile(fileName);
	}

	public void LuaGC()
	{
		if (m_LuaState != null)
			m_LuaState.LuaGC(LuaGCOptions.LUA_GCCOLLECT);
	}

	// 初始化MAIN
	public void InitStart()
	{
		// InitLuaPath
		if (m_LuaState != null)
		{
			m_LuaState.Start();
			DoStartMain();
		}

		if (m_LuaLoop == null)
		{
			m_LuaLoop = gameObject.AddComponent<LuaLooper>();
			m_LuaLoop.luaState = m_LuaState;
		}
	}

	protected void DoStartMain()
	{
		m_LuaState.DoFile("Main.lua");

		// 调用主函数
		LuaFunction main = m_LuaState.GetFunction("Main");
		main.Call();
		main.Dispose();
		main = null;
	}

	//cjson 比较特殊，只new了一个table，没有注册库，这里注册一下
	protected void OpenCJson()
	{
		if (m_LuaState != null)
		{
			m_LuaState.LuaGetField(LuaIndexes.LUA_REGISTRYINDEX, "_LOADED");
			m_LuaState.OpenLibs(LuaDLL.luaopen_cjson);
			m_LuaState.LuaSetField(-2, "cjson");

			m_LuaState.OpenLibs(LuaDLL.luaopen_cjson_safe);
			m_LuaState.LuaSetField(-2, "cjson.safe");   
		}
	}
}
