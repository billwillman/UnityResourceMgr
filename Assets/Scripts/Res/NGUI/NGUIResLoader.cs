//#define _USE_NGUI

#if _USE_NGUI

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;
using NsLib.ResMgr;

public class NGUIResLoader: BaseResLoaderAsyncMono {

    private void ClearInstanceMaterialMap(UITexture target) {
        if (target == null)
            return;
        ClearInstanceMaterialMap(target.GetInstanceID());
    }

    private void ClearInstanceMaterialMap(UISprite target) {
        if (target == null)
            return;
        ClearInstanceMaterialMap(target.GetInstanceID());
    }

    private void ClearInstanceMaterialMap(UI2DSprite target) {
        if (target == null)
            return;
        ClearInstanceMaterialMap(target.GetInstanceID());
    }

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

    protected override bool OnTextureLoaded(Texture target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst, string resName, string tag) {
        bool ret = base.OnTextureLoaded(target, obj, asyncType, isMatInst, resName, tag);
        if (!ret) {
            switch (asyncType) {
                case BaseResLoaderAsyncType.UITextureMainTexture:
                    UITexture ui1 = obj as UITexture;
                    ui1.mainTexture = target;
                    var m1 = ui1.material;
                    if (m1 != null)
                        m1.mainTexture = target;
                    break;
                case BaseResLoaderAsyncType.UISpriteMainTexture:
                    UISprite ui2 = obj as UISprite;
                    ui2.mainTexture = target;
                    break;
                case BaseResLoaderAsyncType.UI2DSpriteMainTexture:
                    UI2DSprite ui3 = obj as UI2DSprite;
                    ui3.mainTexture = target;
                    break;
                default:
                    return false;
            }

            SetResource<Texture>(obj, target, resName, tag);
            return true;
        }

        return false;
    }

	protected override bool OnShaderLoaded(Shader target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst, string resName, string tag) {
		bool ret = base.OnShaderLoaded(target, obj, asyncType, isMatInst, resName, tag);
        if (!ret) {
            switch (asyncType) {
                case BaseResLoaderAsyncType.UITextureShader:
                    UITexture ui1 = obj as UITexture;
                    ui1.shader = target;
                    break;
                case BaseResLoaderAsyncType.UI2DSpriteShader:
                    UI2DSprite ui2 = obj as UI2DSprite;
                    ui2.shader = target;
                    break;
                default:
                    return false;
            }

            SetResource<Shader>(obj, target, resName, tag);
            return true;
        }
        return false;
    }

    public bool LoadMainTextureAsync(UITexture uiTexture, string fileName, int loadPriority = 0) {
        if (uiTexture == null || string.IsNullOrEmpty(fileName))
            return false;
        var mgr = BaseResLoaderAsyncMgr.GetInstance();
        if (mgr != null) {
            ulong id;
            int rk = ReMake(fileName, uiTexture, BaseResLoaderAsyncType.UITextureMainTexture, false, out id, _cMainTex);
            if (rk < 0)
                return false;
            if (rk == 0)
                return true;

            return mgr.LoadTextureAsync(fileName, this, id, loadPriority);
        }
        return false;
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

    public bool LoadShaderAsync(UITexture uiTexture, string fileName, int loadPriority = 0) {
        if (uiTexture == null || string.IsNullOrEmpty(fileName))
            return false;

        var mgr = BaseResLoaderAsyncMgr.GetInstance();
        if (mgr != null) {
            ulong id;
            int rk = ReMake(fileName, uiTexture, BaseResLoaderAsyncType.UITextureShader, false, out id);
            if (rk < 0)
                return false;
            if (rk == 0)
                return true;

            return mgr.LoadShaderAsync(fileName, this, id, loadPriority);
        }
        return false;
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
        ClearInstanceMaterialMap(uiTexture);
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
        if (result == 2) {
            uiTexture.material = GameObject.Instantiate(mat);
            AddOrSetInstanceMaterialMap(uiTexture.GetInstanceID(), uiTexture.material);
        } else if (result == 1) {
            if (uiTexture.material == null) {
                mat = GetInstanceMaterialMap(uiTexture.GetInstanceID());
                uiTexture.material = mat;
            }
        }

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

        if (result == 2) {
            uiSprite.material = GameObject.Instantiate(mat);
            AddOrSetInstanceMaterialMap(uiSprite.GetInstanceID(), uiSprite.material);
        } else if (result == 1) {
            if (uiSprite.material == null) {
                mat = GetInstanceMaterialMap(uiSprite.GetInstanceID());
                uiSprite.material = mat;
            }
        }

        return mat != null;
	}

	public void ClearMaterial(UISprite uiSprite)
	{
		if (uiSprite == null)
			return;
		ClearResource<Material>(uiSprite);
		uiSprite.material = null;
        ClearInstanceMaterialMap(uiSprite);
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

    public bool LoadMainTextureAsync(UISprite uiSprite, string fileName, int loadPriority = 0) {
        if (uiSprite == null || string.IsNullOrEmpty(fileName))
            return false;
        var mgr = BaseResLoaderAsyncMgr.GetInstance();
        if (mgr != null) {
            ulong id;
            int rk = ReMake(fileName, uiSprite, BaseResLoaderAsyncType.UISpriteMainTexture, false, out id, _cMainTex);
            if (rk < 0)
                return false;
            if (rk == 0)
                return true;

            return mgr.LoadTextureAsync(fileName, this, id, loadPriority);
        }
        return false;
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

    public bool LoadMainTextureAsync(UI2DSprite uiSprite, string fileName, int loadPriority = 0) {
        if (uiSprite == null || string.IsNullOrEmpty(fileName))
            return false;
        var mgr = BaseResLoaderAsyncMgr.GetInstance();
        if (mgr != null) {
            ulong id;
            int rk = ReMake(fileName, uiSprite, BaseResLoaderAsyncType.UI2DSpriteMainTexture, false, out id, _cMainTex);
            if (rk < 0)
                return false;
            if (rk == 0)
                return true;

            return mgr.LoadTextureAsync(fileName, this, id, loadPriority);
        }
        return false;
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

    public bool LoadShaderAsync(UI2DSprite uiSprite, string fileName, int loadPriority = 0) {
        if (uiSprite == null || string.IsNullOrEmpty(fileName))
            return false;
        var mgr = BaseResLoaderAsyncMgr.GetInstance();
        if (mgr != null) {
            ulong id;
            int rk = ReMake(fileName, uiSprite, BaseResLoaderAsyncType.UI2DSpriteShader, false, out id);
            if (rk < 0)
                return false;
            if (rk == 0)
                return true;

            return mgr.LoadShaderAsync(fileName, this, id, loadPriority);
        }
        return false;
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
        ClearInstanceMaterialMap(uiSprite);
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

        if (result == 2) {
            uiSprite.material = GameObject.Instantiate(mat);
            AddOrSetInstanceMaterialMap(uiSprite.GetInstanceID(), uiSprite.material);
        } else if (result == 1) {
            if (uiSprite.material == null) {
                mat = GetInstanceMaterialMap(uiSprite.GetInstanceID());
                uiSprite.material = mat;
            }
        }

        return mat != null;
	}

	protected override bool OnPrefabLoaded(GameObject target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, string resName, string tag)
	{
		bool ret = base.OnPrefabLoaded (target, obj, asyncType, resName, tag);
		if (!ret) {
			switch (asyncType) {
			case BaseResLoaderAsyncType.NGUIUISpriteAtlas:
				UISprite o1 = obj as UISprite;
				UIAtlas altas = target.GetComponent<UIAtlas> ();
				if (altas == null) {
					ClearResource<UIAtlas> (o1);
					o1.atlas = null;
					return false;
				}

				o1.atlas = altas;
				SetResource (obj, target, typeof(UIAtlas));
				break;
			default:
				return false;
			}


			return true;
		}
		return ret;
	}

	public bool LoadAtlasAsync(UISprite obj, string fileName, int loadPriority = 0)
	{
		if (obj == null || string.IsNullOrEmpty (fileName))
			return false;
		var mgr = BaseResLoaderAsyncMgr.GetInstance ();
		if (mgr != null) {
			ulong id;
			int rk = ReMake(fileName, obj, BaseResLoaderAsyncType.NGUIUISpriteAtlas, false, out id);
			if (rk < 0)
				return false;
			if (rk == 0)
				return true;

			return mgr.LoadPrefabAsync(fileName, this, id, loadPriority);
		}
		return false;
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

	protected override bool OnFontLoaded(Font target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, string resName, string tag)
	{
		bool ret = base.OnFontLoaded (target, obj, asyncType, resName, tag);
		if (!ret) {

			switch (asyncType) {
			case BaseResLoaderAsyncType.NGUIUIFontFont:
				UIFont o1 = obj as UIFont;
				o1.dynamicFont = target;
				break;
			default:
				return false;	
			}

			SetResource<Font>(obj, target, resName, tag);
			return true;
		}
		return ret;
	}

	public bool LoadFontAsync(UIFont obj, string fileName, int loadPriority = 0)
	{
		if (obj == null || string.IsNullOrEmpty (fileName))
			return false;
		var mgr = BaseResLoaderAsyncMgr.GetInstance ();
		if (mgr != null) {
			ulong id;
			int rk = ReMake(fileName, obj, BaseResLoaderAsyncType.NGUIUIFontFont, false, out id);
			if (rk < 0)
				return false;
			if (rk == 0)
				return true;

			return mgr.LoadFontAsync(fileName, this, id, loadPriority);
		}
		return false;
	}

	/*------------------------------------------ 异步方法 ---------------------------------------------*/
    /*
	public void LoadMainTextureAsync(int start, int end, OnGetItem<UITexture> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadMainTexture, delayTime));
	}

	public void LoadShaderAsync(int start, int end, OnGetItem<UITexture> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadShader, delayTime));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<UITexture> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadMaterial, delayTime));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<UISprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadMaterial, delayTime));
	}

	public void LoadMainTextureAsync(int start, int end, OnGetItem<UISprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadMainTexture, delayTime));
	}

	public void LoadMainTextureAsync(int start, int end, OnGetItem<UI2DSprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadMainTexture, delayTime));
	}

	public void LoadShaderAsync(int start, int end, OnGetItem<UI2DSprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadShader, delayTime));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<UI2DSprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadMaterial, delayTime));
	}

	public void LoadAltasAsync(int start, int end, OnGetItem<UISprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadAltas, delayTime));
	}

	public void LoadTextureAsync(int start, int end, OnGetItem1<UITexture> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UITexture>(start, end, onGetItem, LoadTexture, delayTime));
	}

	public void LoadTextureAsync(int start, int end, OnGetItem1<UISprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UISprite>(start, end, onGetItem, LoadTexture, delayTime));
	}

	public void LoadTextureAsync(int start, int end, OnGetItem1<UI2DSprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadTexture, delayTime));
	}

	public void LoadSpriteAsync(int start, int end, OnGetItem1<UI2DSprite> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<UI2DSprite>(start, end, onGetItem, LoadSprite, delayTime));
	}*/
}

#endif
