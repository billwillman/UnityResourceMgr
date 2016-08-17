using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class BaseResLoader: CachedMonoBehaviour
{
	protected static readonly string _cMainTex = "_MainTex";
	protected Dictionary<ResKey, UnityEngine.Object> m_ResMap = new Dictionary<ResKey, UnityEngine.Object>();

	protected struct ResKey
	{
		public int instanceId;
		public System.Type resType;
		public string resName;
	}

	protected static ResKey CreateKey(int instanceId, System.Type resType, string resName = "")
	{
		ResKey ret = new ResKey();
		ret.resName = resName;
		ret.resType = resType;
		ret.instanceId = instanceId;
		return ret;
	}

	protected virtual void InternalDestroyResource(ResKey key, UnityEngine.Object res)
	{
		if (key.resType == typeof(Sprite))
			Resources.UnloadAsset(res);
	}

	protected void DestroyResource(ResKey key)
	{
		UnityEngine.Object res;
		if (m_ResMap.TryGetValue(key, out res))
		{
			InternalDestroyResource(key, res);
			ResourceMgr.Instance.DestroyObject(res);
			m_ResMap.Remove(key);
		}
	}

	protected void DestroyResource(int instanceId, System.Type resType, string resName = "")
	{
		ResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
	}

	protected void SetResources(int instanceId, UnityEngine.Object res, System.Type resType, string resName = "")
	{
		ResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
		if (res == null)
			return;
		m_ResMap.Add(key, res);
	}

	protected void SetResource(UnityEngine.Object target, UnityEngine.Object res, System.Type resType, string resName = "")
	{
		if (target == null)
			return;
		SetResources(target.GetInstanceID(), res, resType, resName);
	}

	protected void ClearAllResources()
	{
		var iter = m_ResMap.GetEnumerator();
		while (iter.MoveNext())
		{
			InternalDestroyResource(iter.Current.Key, iter.Current.Value);
			ResourceMgr.Instance.DestroyObject(iter.Current.Value);
		}
		iter.Dispose();
		m_ResMap.Clear();
	}

	protected virtual void OnDestroy()
	{
		ClearAllResources();
	}
}
