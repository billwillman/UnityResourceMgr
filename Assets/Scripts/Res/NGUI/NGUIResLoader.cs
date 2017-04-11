#define _USE_NGUI

#if _USE_NGUI

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class NGUIResLoader: BaseResLoader  {

	public bool LoadMainTexture(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, tex, typeof(Texture), _cMainTex);
		uiTexture.mainTexture = tex;
		Material mat = uiTexture.material;
		if (mat != null) {
			mat.mainTexture = tex;
		}

		return tex != null;
	}

	public void ClearMainTexture(UITexture uiTexture)
	{
		if (uiTexture == null)
			return;
		ClearTexture(uiTexture, _cMainTex);
		uiTexture.mainTexture = null;
	}

	public void ClearTexture(UITexture uiTexture, string matName)
	{
		if (uiTexture == null)
			return;
		
		ClearResource<Texture>(uiTexture, matName);
		Material mat = uiTexture.material;
		if (mat != null)
			mat.SetTexture(matName, null);
	}

	public bool LoadShader(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Shader shader = ResourceMgr.Instance.LoadShader(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiTexture, shader, typeof(Shader));
		uiTexture.shader = shader;

		return shader != null;
	}

	public void ClearShader(UITexture uiTexture)
	{
		if (uiTexture == null)
			return;
		ClearResource<Shader>(uiTexture);
		uiTexture.shader = null;
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

	public void ClearMaterial(UITexture uiTexture)
	{
		if (uiTexture == null)
			return;
		ClearResource<Material>(uiTexture);
		uiTexture.material = null;
	}

	public bool LoadMaterial(UITexture uiTexture, string fileName)
	{
		if (uiTexture == null || string.IsNullOrEmpty(fileName))
			return false;

		Material mat;
		int result = SetMaterialResource (uiTexture, fileName, out mat);
		if (result == 0) {
			uiTexture.material = null;
			return false;
		}
		if (result == 2)
			uiTexture.material = GameObject.Instantiate(mat);

		return mat != null;
	}

	public bool LoadMaterial(UISprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat;
		int result = SetMaterialResource (uiSprite, fileName, out mat);
		if (result == 0) {
			uiSprite.material = null;
			return false;
		}

		if (result == 2)
			uiSprite.material = GameObject.Instantiate(mat);

		return mat != null;
	}

	public void ClearMaterial(UISprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearResource<Material>(uiSprite);
		uiSprite.material = null;
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

	public void ClearMainTexture(UISprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearTexture(uiSprite, _cMainTex);
		uiSprite.mainTexture = null;
	}

	public void ClearTexture(UISprite uiSprite, string matName)
	{
		if (uiSprite == null)
			return;
		
		ClearResource<Texture>(uiSprite, matName);

		Material mat = uiSprite.material;
		if (mat != null)
			mat.SetTexture(matName, null);
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

	public bool LoadShader(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;

		Shader shader = ResourceMgr.Instance.LoadShader(fileName, ResourceCacheType.rctRefAdd);
		SetResource(uiSprite, shader, typeof(Shader));
		uiSprite.shader = shader;

		return shader != null;
	}

	public void ClearShader(UI2DSprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearResource<Shader>(uiSprite);
		uiSprite.shader = null;
	}

	public void ClearTexture(UI2DSprite uiSprite, string matName)
	{
		if (uiSprite == null)
			return;
		
		ClearResource<Texture>(uiSprite, matName);

		Material mat = uiSprite.material;
		if (mat != null)
			mat.SetTexture(matName, null);
	}

	public void ClearMainTexture(UI2DSprite uiSprite)
	{
		if (uiSprite == null)
			return;
		
		ClearTexture(uiSprite, _cMainTex);
		uiSprite.mainTexture = null;
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

	public bool LoadSprite(UI2DSprite uiSprite, string fileName, string spriteName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
			return false;
		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName);
		bool isFound = false;
		for (int i = 0; i < sps.Length; ++i)
		{
			Sprite sp = sps[i];
			if (sp == null)
				continue;
			if (!isFound && string.Compare(sp.name, spriteName) == 0)
			{
				uiSprite.sprite2D = sp;
				isFound = true;
				SetResources(uiSprite, sps, typeof(Sprite[]));
				break;
			}
		}

		if (!isFound)
		{
			for (int i = 0; i < sps.Length; ++i)
			{
				Sprite sp = sps[i];
				DestroySprite(sp);
			}
			SetResources(uiSprite, null, typeof(Sprite[]));
		}

		return isFound;
	}

	public void ClearMaterial(UI2DSprite uiSprite)
	{
		if (uiSprite == null)
			return;
		uiSprite.material = null;
		ClearResource<Material>(uiSprite);
	}

	public bool LoadMaterial(UI2DSprite uiSprite, string fileName)
	{
		if (uiSprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat;
		int result = SetMaterialResource (uiSprite, fileName, out mat);

		if (result == 0) {
			uiSprite.material = null;
			return false;
		}

		if (result == 2)
			uiSprite.material = GameObject.Instantiate(mat);

		return mat != null;
	}

	public bool LoadAltas(UISprite uiSprite, string fileName)
	{
			if (uiSprite == null || string.IsNullOrEmpty (fileName))
				return false;

			GameObject obj = ResourceMgr.Instance.LoadPrefab (fileName, ResourceCacheType.rctRefAdd);
			if (obj == null) {
				ClearResource<UIAtlas> (uiSprite);
				uiSprite.atlas = null;
				return false;
			}

			UIAtlas altas = obj.GetComponent<UIAtlas> ();
			if (altas == null) {
				ResourceMgr.Instance.DestroyObject (obj);
				ClearResource<UIAtlas> (uiSprite);
				uiSprite.atlas = null;
				return false;
			}

			SetResource (uiSprite, obj, typeof(UIAtlas));
			uiSprite.atlas = altas;
			return altas != null;
	}

	public void ClearAtlas(ref UIAtlas target)
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), null, typeof(UIAtlas));
		target = null;
	}

	public bool LoadAtlas(ref UIAtlas target, string fileName)
	{
			if (string.IsNullOrEmpty(fileName))
				return false;
			ClearAtlas(ref target);
			GameObject obj = ResourceMgr.Instance.LoadPrefab (fileName, ResourceCacheType.rctRefAdd);
			if (obj == null)
				return false;
			target = obj.GetComponent<UIAtlas> ();
			if (target == null) {
				ResourceMgr.Instance.DestroyObject (obj);
				return false;
			}
			SetResource(target, obj, typeof(UIAtlas));
			return true;
	}

	/*------------------------------------------ 异步方法 ---------------------------------------------*/

	public void LoadMainTextureAsync(int start, int end, OnGetItem<UITexture> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadMainTexture));
	}

	public void LoadShaderAsync(int start, int end, OnGetItem<UITexture> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadShader));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<UITexture> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadMaterial));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<UISprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadMaterial));
	}

	public void LoadMainTextureAsync(int start, int end, OnGetItem<UISprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadMainTexture));
	}

	public void LoadMainTextureAsync(int start, int end, OnGetItem<UI2DSprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadMainTexture));
	}

	public void LoadShaderAsync(int start, int end, OnGetItem<UI2DSprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadShader));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<UI2DSprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadMaterial));
	}

	public void LoadAltasAsync(int start, int end, OnGetItem<UISprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadAltas));
	}

	public void LoadTextureAsync(int start, int end, OnGetItem1<UITexture> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadTexture));
	}

	public void LoadTextureAsync(int start, int end, OnGetItem1<UISprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadTexture));
	}

	public void LoadTextureAsync(int start, int end, OnGetItem1<UI2DSprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadTexture));
	}

	public void LoadSpriteAsync(int start, int end, OnGetItem1<UI2DSprite> onGetItem)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadSprite));
	}
}

#endif
