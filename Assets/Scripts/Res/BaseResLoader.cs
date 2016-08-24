#define USE_CHECK_VISIBLE
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

	#if USE_CHECK_VISIBLE
	private bool m_IsCheckedVisible = false;
	#endif

	private void CheckVisible()
	{
		#if USE_CHECK_VISIBLE
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
		#endif
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

	protected int SetMaterialResource(UnityEngine.Object target, string fileName, out Material mat)
	{
		mat = null;
		if (target == null || string.IsNullOrEmpty (fileName))
			return 0;
		ResKey key = CreateKey (target.GetInstanceID (), typeof(Material));
		ResValue value;
		if (FindResValue (key, out value)) {
			if (string.Compare (value.tag, fileName) == 0)
				// 相等说明当前已经是，所以不需要外面设置了
				mat = value.obj as Material;
			return 1;
		}

		mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResources(target, null, typeof(Material[]));
		SetResource(target, mat, typeof(Material), "", fileName);
		return 2;
	}

	public bool LoadMaterial(MeshRenderer renderer, string fileName)
	{

		Material mat;
		int result = SetMaterialResource (renderer, fileName, out mat);
		if (result == 0) {
			renderer.sharedMaterial = null;
			return false;
		}

		if (result == 2)
			renderer.sharedMaterial = mat;
		return mat != null;
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
		Material mat;
		int result = SetMaterialResource (sprite, fileName, out mat);
		if (result == 0) {
			sprite.sharedMaterial = null;
			return false;
		}

		if (result == 2)
			sprite.sharedMaterial = mat;

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

	public bool LoadSprite(SpriteRenderer sprite, string fileName)
	{
		if (sprite == null || string.IsNullOrEmpty(fileName))
			return false;
		
		ResValue resValue;
		if (FindResValue (sprite, typeof(Sprite[]), out resValue)) {
			bool isSame = string.Compare(fileName, resValue.tag) == 0;
			if (isSame)
			{
				if (resValue.objs != null && resValue.objs.Length > 0) {
					Sprite sp = resValue.objs [0] as Sprite;
					sprite.sprite = sp;
					return sp != null;
				}

				sprite.sprite = null;
				return false;
			}
		};

		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName, ResourceCacheType.rctRefAdd);
		if (sps == null || sps.Length <= 0) {
			sprite.sprite = null;
			SetResources(sprite, null, typeof(Sprite[]));
			return false;
		}
		
		
		SetResources(sprite, sps, typeof(Sprite[]), string.Empty, fileName);
		Sprite sp1 = sps[0];
		sprite.sprite = sp1;
		return sp1 != null;
	}

	public bool LoadSprite(SpriteRenderer sprite, string fileName, string spriteName)
	{
		if (sprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
			return false;

		ResValue resValue;
		if (FindResValue (sprite, typeof(Sprite[]), out resValue)) {
			bool isSame = string.Compare (resValue.tag, fileName) == 0;
			if (isSame) {
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

			sprite.sprite = null;
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
				SetResources(sprite, sps, typeof(Sprite[]), string.Empty, fileName);
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

			sprite.sprite = null;
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

	public static void DestroyGameObj(GameObject obj)
	{
		if (obj == null)
			return;
		BaseResLoader loader = obj.GetComponent<BaseResLoader> ();
		if (loader != null) {
			loader.ClearAllResources ();
		}
		
		GameObject.Destroy (obj);
	}

	public List<UnityEngine.Object> GetResList()
		{
			if (m_ResMap == null) {
				return null;
			}

			List<UnityEngine.Object> list = new List<UnityEngine.Object> ();
			var iter = m_ResMap.GetEnumerator ();
			while (iter.MoveNext ()) {
				if (iter.Current.Value.obj != null) {
					list.Add (iter.Current.Value.obj);
				}
				if (iter.Current.Value.objs != null) {
					for (int i = 0; i < iter.Current.Value.objs.Length; ++i) {
						list.Add (iter.Current.Value.objs [i]);
					}
				}
			}
			iter.Dispose ();
			return list;
		}

		public void ClearMainTexture(MeshRenderer renderer)
		{
			if (renderer == null)
				return;
			
			ClearResource<Texture> (renderer, _cMainTex);
		}
			
		public bool LoadMainTexture(MeshRenderer renderer, string fileName)
		{
			if (renderer == null || string.IsNullOrEmpty (fileName) || renderer.sharedMaterial == null)
				return false;
			ClearMainTexture (renderer);
			Texture tex = ResourceMgr.Instance.LoadTexture (fileName, ResourceCacheType.rctRefAdd);
			SetResource(renderer, tex, typeof(Texture), _cMainTex);
			renderer.material.mainTexture = tex;
			return tex != null;
		}

	public GameObject CreateGameObject(string fileName)
	{
		GameObject ret = ResourceMgr.Instance.CreateGameObject(fileName);
		return ret;
	}

	public T CreateGameObject<T>(string fileName) where T: UnityEngine.Component
	{
		GameObject obj = ResourceMgr.Instance.CreateGameObject(fileName);
		if (obj == null)
			return null;
		T ret = obj.GetComponent<T>();
		return ret;
	}
}
