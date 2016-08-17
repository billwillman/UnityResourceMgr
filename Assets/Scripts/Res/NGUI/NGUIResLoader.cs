#define _USE_NGUI

#if _USE_NGUI

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class NGUIResLoader: CachedMonoBehaviour  {

	private static readonly string _cMainTex = "_MainTex";
	private Dictionary<NGUIResKey, UnityEngine.Object> m_ResMap = new Dictionary<NGUIResKey, Object>();

	private struct NGUIResKey
	{
		public int instanceId;
		public System.Type resType;
		public string resName;
	}

	private static NGUIResKey CreateKey(int instanceId, System.Type resType, string resName = "")
	{
		NGUIResKey ret = new NGUIResKey();
		ret.resName = resName;
		ret.resType = resType;
		ret.instanceId = instanceId;
		return ret;
	}

	private void DestroyResource(NGUIResKey key)
	{
		UnityEngine.Object res;
		if (m_ResMap.TryGetValue(key, out res))
		{
			ResourceMgr.Instance.DestroyObject(res);
			m_ResMap.Remove(key);
		}
	}

	private void DestroyResource(int instanceId, System.Type resType, string resName = "")
	{
		NGUIResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
	}

	private void SetResources(int instanceId, UnityEngine.Object res, System.Type resType, string resName = "")
	{
		NGUIResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
		if (res == null)
			return;
		m_ResMap.Add(key, res);
	}

	private void SetResource(UnityEngine.Object target, UnityEngine.Object res, System.Type resType, string resName = "")
	{
		if (target == null)
			return;
		SetResources(target.GetInstanceID(), res, resType, resName);
	}

	private void ClearAllResources()
	{
		var iter = m_ResMap.GetEnumerator();
		while (iter.MoveNext())
		{
			ResourceMgr.Instance.DestroyObject(iter.Current.Value);
		}
		iter.Dispose();
		m_ResMap.Clear();
	}

	public bool LoadMainTexture(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, tex, typeof(Texture), _cMainTex);
		uiTexture.mainTexture = tex;

		return tex != null;
	}

	public bool LoadTexture(UITexture uiTexture, string fileName, string matName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
			return false;

		Material mat = uiTexture.material;
		if (mat == null)
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, tex, typeof(Texture), matName);
		mat.SetTexture(matName, tex);

		return tex != null;
	}

	public bool LoadMaterial(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, mat, typeof(Material));
		if (mat != null)
			//uiTexture.material = mat;
			uiTexture.material = GameObject.Instantiate(mat);
		else
			uiTexture.material = null;

		return mat != null;
	}

	public bool LoadMaterial(UISprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, mat, typeof(Material));

		if (mat != null)
			//uiSprite.material = mat;
			uiSprite.material = GameObject.Instantiate(mat);
		else
			uiSprite.material = null;

		return mat != null;
	}

	public bool LoadTexture(UISprite uiSprite, string fileName, string matName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
			return false;

		Material mat = uiSprite.material;
		if (mat == null)
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), matName);
		mat.SetTexture(matName, tex);

		return tex != null;
	}

	public bool LoadMainTexture(UISprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		
		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), _cMainTex);
		uiSprite.mainTexture = tex;

		return tex != null;
	}

	public bool LoadMainTexture(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), _cMainTex);
		uiSprite.mainTexture = tex;

		return tex != null;
	}

	public bool LoadTexture(UI2DSprite uiSprite, string fileName, string matName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
			return false;

		Material mat = uiSprite.material;
		if (mat == null)
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, tex, typeof(Texture), matName);
		mat.SetTexture(matName, tex);

		return tex != null;
	}

	public bool LoadMaterial(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, mat, typeof(Material));

		if (mat != null)
			//uiSprite.material = mat;
			uiSprite.material = GameObject.Instantiate(mat);
		else
			uiSprite.material = null;

		return mat != null;
	}

	protected virtual void OnDestroy()
	{
		ClearAllResources();
	}
}

#endif
