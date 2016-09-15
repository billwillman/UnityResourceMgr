using System;
using System.Collections;
using System.Collections.Generic;
using Utils;
using UnityEngine;

public class LoaderNode
{

	public LoaderNode()
	{
		m_Node = new LinkedListNode<LoaderNode>(this);
	}

	public LinkedListNode<LoaderNode> LinkNode
	{
		get
		{
			return m_Node;
		}
	}

	private void Reset()
	{
		m_ResFileName = string.Empty;
		m_ResCacheType = ResourceCacheType.rctNone;	
		m_CallBack = null;
		m_ResType = null;
		m_IsAsync = false;
	}

	private static void InitPool()
	{
		if (m_InitPool)
			return;
		m_InitPool = true;
		m_Pool.Init(0);
	}

	private static void InPool(LoaderNode node)
	{
		if (node == null)
			return;
		node.Reset();
		InitPool();
		m_Pool.Store(node);
	}

	public string ResFileName
	{
		get
		{
			return m_ResFileName;
		}
	}

	public ResourceCacheType CacheType
	{
		get
		{
			return m_ResCacheType;
		}
	}

	public bool IsAsync
	{
		get
		{
			return m_IsAsync;
		}
	}

	public static LoaderNode Create(string fileName, ResourceCacheType cacheType, System.Type resType, bool isAsync, System.Delegate callBack = null)
	{
		InitPool();
		LoaderNode ret = m_Pool.GetObject();
		ret.m_ResFileName = fileName;
		ret.m_ResCacheType = cacheType;
		ret.m_CallBack = callBack;
		ret.m_ResType = resType;
		ret.m_IsAsync = isAsync;
		return ret;
	}

	public void Destroy()
	{
		InPool(this);
	}

	public System.Delegate CallBack
	{
		get
		{
			return m_CallBack;
		}
	}

	public System.Type ResType
	{
		get
		{
			return m_ResType;
		}
	}

	internal void DoLoad()
	{
		System.Type resType = ResType;

		if (resType == typeof(GameObject))
		{
			Action<float, bool, GameObject> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, GameObject>;

			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadPrefabAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				GameObject ret = ResourceMgr.Instance.LoadPrefab(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}

		} else if (resType == typeof(Texture))
		{
			Action<float, bool, Texture> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, Texture>;

			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadTextureAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				Texture ret = ResourceMgr.Instance.LoadTexture(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(Material))
		{
			Action<float, bool, Material> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, Material>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadMaterialAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				Material ret = ResourceMgr.Instance.LoadMaterial(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(Shader))
		{
			Action<float, bool, Shader> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, Shader>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadShaderAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				Shader ret = ResourceMgr.Instance.LoadShader(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(AnimationClip))
		{
			Action<float, bool, AnimationClip> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, AnimationClip>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadAnimationClipAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				AnimationClip ret = ResourceMgr.Instance.LoadAnimationClip(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(AudioClip))
		{
			Action<float, bool, AudioClip> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, AudioClip>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadAudioClipAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				AudioClip ret = ResourceMgr.Instance.LoadAudioClip(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(RuntimeAnimatorController))
		{
			Action<float, bool, RuntimeAnimatorController> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, RuntimeAnimatorController>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadAniControllerAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				RuntimeAnimatorController ret = ResourceMgr.Instance.LoadAniController(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(ScriptableObject))
		{
			Action<float, bool, ScriptableObject> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, ScriptableObject>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadScriptableObjectAsync(ResFileName, CacheType, func))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				ScriptableObject ret = ResourceMgr.Instance.LoadScriptableObject(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}
		} else if (resType == typeof(ShaderVariantCollection))
		{
			Action<float, bool, ShaderVariantCollection> func = null;
			if (CallBack != null)
				func = CallBack as Action<float, bool, ShaderVariantCollection>;
			if (IsAsync)
			{
				if (!ResourceMgr.Instance.LoadShaderVarCollectionAsync(ResFileName, func, CacheType))
				{
					if (func != null)
						func(1.0f, true, null);
				}
			} else
			{
				ShaderVariantCollection ret = ResourceMgr.Instance.LoadShaderVarCollection(ResFileName, CacheType);
				if (func != null)
					func(1.0f, true, ret);
			}

		}

	}

	private LinkedListNode<LoaderNode> m_Node = null;
	private string m_ResFileName = string.Empty;
	private ResourceCacheType m_ResCacheType = ResourceCacheType.rctNone;
	private System.Delegate m_CallBack = null;
	private System.Type m_ResType = null;
	private bool m_IsAsync = false;

	private static ObjectPool<LoaderNode> m_Pool = new ObjectPool<LoaderNode>();
	private static bool m_InitPool = false;
}

public class LoaderNodeMgr
{
	public LoaderNodeMgr(float loadDelayTime = 1.0f)
	{
		m_LoadDelayTime = loadDelayTime;
		m_Timer = TimerMgr.Instance.CreateTimer(false, 0, true, true);
		m_Timer.AddListener(OnLoaderTime);
	}

	public void AddNode(string fileName, ResourceCacheType cacheType, System.Type resType, 
						bool isAsync, System.Delegate callBack = null)
	{
		AsyncLoadKey key =  new AsyncLoadKey();
		key.fileName = fileName;
		key.type = resType;
		if (m_LoadingHash.Contains(key))
			return;
		
		LoaderNode node = LoaderNode.Create(fileName, cacheType, resType, isAsync, callBack);
		if (node == null)
			return;

		m_LoadingHash.Add(key);
		m_LoadList.AddLast(node.LinkNode);
	}

	private void OnLoaderTime(Timer time, float delta)
	{
		if (m_LoadList.Count <= 0)
		{
			m_LastDelayTime = 0;
			return;
		}

		bool isUseLoad = false;
		if (m_LastDelayTime < float.Epsilon)
		{
			m_LastDelayTime = m_LoadDelayTime;
			isUseLoad = true;
		} else		
		{
			m_LastDelayTime -= delta;
			if (m_LastDelayTime <= 0)
			{
				isUseLoad = true;
				m_LastDelayTime = m_LoadDelayTime;
			}
		}

		if (!isUseLoad)
			return;

		var node = m_LoadList.First;
		m_LoadList.RemoveFirst();

		AsyncLoadKey key = new AsyncLoadKey();
		key.fileName = node.Value.ResFileName;
		key.type = node.Value.ResType;
		m_LoadingHash.Remove(key);

		node.Value.DoLoad();
		node.Value.Destroy();
	}

	// 小心使用
	public void Clear()
	{
		m_LoadingHash.Clear();
		var node = m_LoadList.First;
		while (node != null)
		{
			if (node.Value != null)
				node.Value.Destroy();
			node = node.Next;
		}
		m_LoadList.Clear();
	}

	private LinkedList<LoaderNode> m_LoadList = new LinkedList<LoaderNode>();
	private HashSet<AsyncLoadKey> m_LoadingHash = new HashSet<AsyncLoadKey>();
	private Timer m_Timer = null;
	private float m_LoadDelayTime = 1.0f;
	private float m_LastDelayTime = 0;
}
