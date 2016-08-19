using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class BaseResLoader: CachedMonoBehaviour
{
	protected static readonly string _cMainTex = "_MainTex";
	protected static readonly string _cMainMat = "_Mat_0";
	private Dictionary<ResKey, ResValue> m_ResMap = null;

	private bool m_IsCheckedVisible = false;

	private void CheckVisible()
	{
		if (m_IsCheckedVisible)
			return;
		m_IsCheckedVisible = true;
		GameObject obj = this.CachedGameObject;
		if (obj == null)
			return;
		if (!obj.activeSelf)
		{
			obj.SetActive(true);
			obj.SetActive(false);
		}
	}

	private void CheckResMap()
	{
		if (m_ResMap == null)
			m_ResMap = new Dictionary<ResKey, ResValue> ();
	}

	protected bool FindResValue(ResKey key, out ResValue value)
	{
		value = new ResValue ();
		if (m_ResMap == null)
			return false;
		return m_ResMap.TryGetValue (key, out value);
	}


	protected bool FindResValue(int instanceId, System.Type resType, out ResValue value, string resName = "")
	{
		ResKey key = CreateKey (instanceId, resType, resName);
		return FindResValue (key, out value);
	}

	protected bool FindResValue(UnityEngine.Object target, System.Type resType, out ResValue value, string resName = "")
	{
		value = new ResValue ();
		if (target == null)
			return false;
		return FindResValue (target.GetInstanceID (), resType, out value, resName);
	}

	protected string GetMatResName(int matIdx)
	{
		if (matIdx < 0)
			return string.Empty;
		string ret = string.Format("_Mat_{0:D}", matIdx);
		return ret;
	}

	protected struct ResKey
	{
		public int instanceId;
		public System.Type resType;
		public string resName;
	}

	protected struct ResValue
	{
		public UnityEngine.Object obj;
		public UnityEngine.Object[] objs;
		public string tag;
	}

	protected static ResKey CreateKey(int instanceId, System.Type resType, string resName = "")
	{
		ResKey ret = new ResKey();
		ret.resName = resName;
		ret.resType = resType;
		ret.instanceId = instanceId;
		return ret;
	}

	protected ResValue CreateValue(UnityEngine.Object obj, string tag = "")
	{
		ResValue ret = new ResValue();
		ret.obj = obj;
		ret.objs = null;
		ret.tag = tag;
		return ret;
	}

	protected ResValue CreateValue(UnityEngine.Object[] objs, string tag = "")
	{
		ResValue ret = new ResValue();
		ret.obj = null;
		ret.objs = objs;
		ret.tag = tag;
		return ret;
	}

	protected virtual bool InternalDestroyResource(ResKey key, ResValue res)
	{
		if (key.resType == typeof(Sprite))
		{
			if (res.obj != null)
				Resources.UnloadAsset(res.obj);
		} else if (key.resType == typeof(Sprite[]))
		{
			if (res.objs != null)
			{
				for (int i = 0; i < res.objs.Length; ++i)
				{
					UnityEngine.Object obj = res.objs[i];
					if (obj != null)
						Resources.UnloadAsset(obj);
				}
			}
		}

		return true;
	}

	protected void DestroyResource(ResKey key)
	{
		if (m_ResMap == null)
			return;
		ResValue res;
		if (m_ResMap.TryGetValue(key, out res))
		{
			if (InternalDestroyResource(key, res))
			{
				if (res.obj != null)
					ResourceMgr.Instance.DestroyObject(res.obj);

				if (res.objs != null)
					ResourceMgr.Instance.DestroyObjects(res.objs);
			}
			m_ResMap.Remove(key);
		}
	}

	protected void DestroyResource(int instanceId, System.Type resType, string resName = "")
	{
		ResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
	}

	protected void SetResource(int instanceId, UnityEngine.Object res, System.Type resType, string resName = "", string tag = "")
	{
		ResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
		if (res == null)
			return;
		CheckResMap ();
		CheckVisible();
		ResValue value = CreateValue(res, tag);
		m_ResMap.Add(key, value);
	}

	protected void SetResource(UnityEngine.Object target, UnityEngine.Object res, System.Type resType, string resName = "", string tag = "")
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), res, resType, resName, tag);
	}

	protected void ClearResource<T>(UnityEngine.Object target, string resName = "") where T: UnityEngine.Object
	{
		System.Type resType = typeof(T);
		SetResource<T>(target, null, resName);
	}

	protected void ClearResource(UnityEngine.Object target, System.Type resType, string resName = "")
	{
		SetResource(target, null, resType, resName);
	}

	protected void SetResource<T>(UnityEngine.Object target, T res, string resName = "", string tag = "") where T: UnityEngine.Object
	{
		System.Type resType = typeof(T);
		SetResource(target, res, resType, resName, tag);
	}

	protected void SetResources(int instanceId, UnityEngine.Object[] res, System.Type resType, string resName = "", string tag = "")
	{
		ResKey key = CreateKey(instanceId, resType, resName);
		DestroyResource(key);
		if (res == null)
			return;
		CheckResMap ();
		CheckVisible();
		ResValue value = CreateValue(res, tag);
		m_ResMap.Add(key, value);
	}

	protected void SetResources(UnityEngine.Object target, UnityEngine.Object[] res, System.Type resType, string resName = "", string tag = "")
	{
		if (target == null)
			return;
		SetResources(target.GetInstanceID(), res, resType, resName, tag);
	}

	protected void ClearAllResources()
	{
		if (m_ResMap == null)
			return;
		
		var iter = m_ResMap.GetEnumerator();
		while (iter.MoveNext())
		{
			if (InternalDestroyResource(iter.Current.Key, iter.Current.Value))
			{
				if (iter.Current.Value.obj != null)
					ResourceMgr.Instance.DestroyObject(iter.Current.Value.obj);

				if (iter.Current.Value.objs != null)
					ResourceMgr.Instance.DestroyObjects(iter.Current.Value.objs);
			}
		}
		iter.Dispose();
		m_ResMap.Clear();
	}

	protected virtual void OnDestroy()
	{
		ClearAllResources();
	}

	protected void DestroySprite(Sprite sp)
	{
		if (sp == null)
			return;
		Resources.UnloadAsset(sp);
		ResourceMgr.Instance.DestroyObject(sp);
	}

	public bool LoadMaterial(MeshRenderer renderer, string fileName)
	{
		if (renderer == null)
			return false;

		Material mat = null;
		if (!string.IsNullOrEmpty(fileName))
			mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResources(renderer, null, typeof(Material[]));
		SetResource(renderer, mat, typeof(Material));
		renderer.sharedMaterial = mat;
		return true;
	}

	public void ClearMaterials(MeshRenderer renderer)
	{
		SetResources(renderer, null, typeof(Material[]));
		SetResource(renderer, null, typeof(Material));
		renderer.sharedMaterial = null;
		renderer.material = null;
		renderer.sharedMaterials = null;
		renderer.materials = null;
	}

	public bool LoadMaterial(SpriteRenderer sprite, string fileName)
	{
		if (sprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(sprite, mat, typeof(Material));

		if (mat != null)
			sprite.sharedMaterial = mat;
		else
			sprite.sharedMaterial = null;

		return mat != null;
	}

	public void ClearMaterial(SpriteRenderer sprite)
	{
		if (sprite == null)
			return;
		ClearResource<Material>(sprite);
		sprite.sharedMaterial = null;
		sprite.material = null;
	}

	public bool LoadSprite(SpriteRenderer sprite, string fileName, string spriteName)
	{
		if (sprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
			return false;

		ResValue resValue;
		if (FindResValue (sprite, typeof(Sprite[]), out resValue)) {
			if (string.Compare (resValue.tag, fileName) == 0) {
				if (resValue.objs != null) {
					for (int i = 0; i < resValue.objs.Length; ++i) {
						Sprite sp = resValue.objs [i] as Sprite;
						if (sp == null)
							continue;
						if (string.Compare (sp.name, spriteName) == 0) {
							sprite.sprite = sp;
							return true;
						}
					}
				}
			}

			return false;
		};

		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName, ResourceCacheType.rctRefAdd);
		bool isFound = false;
		for (int i = 0; i < sps.Length; ++i)
		{
			Sprite sp = sps[i];
			if (sp == null)
				continue;
			if (!isFound && string.Compare(sp.name, spriteName) == 0)
			{
				sprite.sprite = sp;
				isFound = true;
				SetResources(sprite, sps, typeof(Sprite[]), "", fileName);
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
			SetResources(sprite, null, typeof(Sprite[]));
		}

		return isFound;
	}

    public bool LoadGameObject(ref GameObject target, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        ClearGameObject(ref target);
        target = ResourceMgr.Instance.LoadPrefab(fileName, ResourceCacheType.rctRefAdd);
        if (target != null)
            SetResource(target.GetInstanceID(), target, typeof(GameObject));
        return target != null;
    }

    public void ClearGameObject(ref GameObject target)
    {
        if (target == null)
            return;
        SetResource(target.GetInstanceID(), null, typeof(GameObject));
        target = null;
    }

	public void ClearMaterail(ref Material target)
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), null, typeof(Material));
		target = null;
	}

	public bool LoadMaterial(ref Material target, string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		ClearMaterail(ref target);
		target = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		if (target != null)
			SetResource(target.GetInstanceID(), target, typeof(Material));
		return target != null;
	}

	public void ClearTexture(ref Texture target)
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), null, typeof(Texture));
		target = null;
	}

	public bool LoadTexture(ref Texture target, string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		ClearTexture(ref target);
		target = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
		if (target != null)
			SetResource(target.GetInstanceID(), target, typeof(Texture));
		return target != null;
	}

	public void ClearShader(ref Shader target)
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), null, typeof(Shader));
		target = null;
	}

	public bool LoadShader(ref Shader target, string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		ClearShader(ref target);
		target = ResourceMgr.Instance.LoadShader(fileName, ResourceCacheType.rctRefAdd);
		if (target != null)
			SetResource(target.GetInstanceID(), target, typeof(Shader));
		return target != null;
	}

	public void ClearAudioClip(ref AudioClip target)
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), null, typeof(AudioClip));
		target = null;
	}

	public bool LoadAudioClip(ref AudioClip target, string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		ClearAudioClip(ref target);
		target = ResourceMgr.Instance.LoadAudioClip(fileName, ResourceCacheType.rctRefAdd);
		if (target != null)
			SetResource(target.GetInstanceID(), target, typeof(AudioClip));
		return target != null;
	}

	public void ClearAnimationClip(ref AnimationClip target)
	{
		if (target == null)
			return;
		SetResource(target.GetInstanceID(), null, typeof(AnimationClip));
		target = null;
	}

	public bool LoadAnimationClip(ref AnimationClip target, string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		ClearAnimationClip(ref target);
		target = ResourceMgr.Instance.LoadAnimationClip(fileName, ResourceCacheType.rctRefAdd);
		if (target != null)
			SetResource(target.GetInstanceID(), target, typeof(AnimationClip));
		return target != null;
	}

    public void ClearAniController(Animator target)
    {
        if (target == null)
            return;
        SetResource(target.GetInstanceID(), null, typeof(RuntimeAnimatorController));
    }

    public bool LoadAniController(Animator target, string fileName)
    {
        if (target == null || string.IsNullOrEmpty(fileName))
            return false;
        ClearAniController(target);
        var ctl = ResourceMgr.Instance.LoadAniController(fileName, ResourceCacheType.rctRefAdd);
        if (ctl != null)
            SetResource(target.GetInstanceID(), ctl, typeof(RuntimeAnimatorController));
        target.runtimeAnimatorController = ctl;
        return ctl != null;
    }
}
