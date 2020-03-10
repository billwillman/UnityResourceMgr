#define USE_CHECK_VISIBLE
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class BaseResLoader: CachedMonoBehaviour
{
	public static readonly string _cMainTex = "_MainTex";
	public static readonly string _cMainMat = "_Mat_0";
	public static readonly string _cMat1 = "_Mat_1";
	public static readonly string _cMat2 = "_Mat_2";
	public static readonly string _cMat3 = "_Mat_3";

	private Dictionary<ResKey, ResValue> m_ResMap = null;

	#if USE_CHECK_VISIBLE
	private bool m_IsCheckedVisible = false;
#endif

    #region Instance Material
    // 记录实例化的材质Map
    private Dictionary<int, Material> m_InstanceMaterialMap = null;


    protected Material GetRealMaterial(Renderer render, bool isMatInst) {
        if (render == null)
            return null;
        int instId = render.GetInstanceID();
        Material ret = GetInstanceMaterialMap(instId);
        if (ret == null) {
            ret = render.sharedMaterial;
            if (isMatInst && ret != null) {
                ret = GameObject.Instantiate(ret);
                AddOrSetInstanceMaterialMap(instId, ret);
                render.sharedMaterial = ret;
            }
        }
        return ret;
    }

    protected Material GetInstanceMaterialMap(int instanceId) {
        if (m_InstanceMaterialMap != null && m_InstanceMaterialMap.Count > 0) {
            Material ret;
            if (m_InstanceMaterialMap.TryGetValue(instanceId, out ret))
                return ret;
        }
        return null;
    }

    protected void AddOrSetInstanceMaterialMap(int instanceId, Material cloneMaterial) {
        if (m_InstanceMaterialMap != null && m_InstanceMaterialMap.Count > 0) {
            Material oldMat;
            if (m_InstanceMaterialMap.TryGetValue(instanceId, out oldMat)) {
                if (oldMat != null)
                    GameObject.Destroy(oldMat);
                if (cloneMaterial != null)
                    m_InstanceMaterialMap[instanceId] = cloneMaterial;
                else
                    m_InstanceMaterialMap.Remove(instanceId);
                return;
            }
        }

        if (cloneMaterial == null)
            return;

        if (m_InstanceMaterialMap == null)
            m_InstanceMaterialMap = new Dictionary<int, Material>();
        m_InstanceMaterialMap.Add(instanceId, cloneMaterial);
    }

    protected void ClearInstanceMaterialMap() {
        if (m_InstanceMaterialMap == null || m_InstanceMaterialMap.Count <= 0)
            return;
        var iter = m_InstanceMaterialMap.GetEnumerator();
        while (iter.MoveNext()) {
            Material mat = iter.Current.Value;
            if (mat != null)
                GameObject.Destroy(mat);
        }
        iter.Dispose();
        m_InstanceMaterialMap.Clear();
    }

    protected void ClearInstanceMaterialMap(int instanceId) {
        AddOrSetInstanceMaterialMap(instanceId, null);
    }
    #endregion
	
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
			m_ResMap = new Dictionary<ResKey, ResValue> (ResKeyComparser.Default);
	}

	protected bool FindResValue(ResKey key, out ResValue value)
	{
		value = null;
		if (m_ResMap == null)
			return false;
		return m_ResMap.TryGetValue (key, out value);
	}

    protected bool Contains(ResKey key) {
        if (m_ResMap == null)
            return false;
        return m_ResMap.ContainsKey(key);
    }

	protected bool FindResValue(int instanceId, System.Type resType, out ResValue value, string resName = "")
	{
		ResKey key = CreateKey (instanceId, resType, resName);
		return FindResValue (key, out value);
	}

	protected bool FindResValue(UnityEngine.Object target, System.Type resType, out ResValue value, string resName = "")
	{
		value = null;
		if (target == null)
			return false;
		return FindResValue (target.GetInstanceID (), resType, out value, resName);
	}

	protected string GetMatResName(int matIdx)
	{
		if (matIdx < 0)
			return string.Empty;
		string ret = StringHelper.Format("_Mat_{0:D}", matIdx);
		return ret;
	}

	protected class ResKeyComparser: StructComparser<ResKey>
	{}

	protected struct ResKey : IEquatable<ResKey>
	{
		public int instanceId;
		public System.Type resType;
		public string resName;

        public bool Equals(ResKey other) {
            return this == other;
        }

        public override bool Equals(object obj) {
			if (obj == null)
				return false;
			
			if (GetType() != obj.GetType())
				return false;
			
            if (obj is ResKey) {
                ResKey other = (ResKey)obj;
                return Equals(other);
            }
            else
                return false;

        }

        public override int GetHashCode() {
            int ret = FilePathMgr.InitHashValue();
            FilePathMgr.HashCode(ref ret, instanceId);
            FilePathMgr.HashCode(ref ret, resType);
            FilePathMgr.HashCode(ref ret, resName);
            return ret;
        }

        public static bool operator ==(ResKey a, ResKey b) {
            return (a.instanceId == b.instanceId) && (a.resType == b.resType) && (string.Compare(a.resName, b.resName) == 0);
        }

        public static bool operator !=(ResKey a, ResKey b) {
            return !(a == b);
        }
	}

	protected class ResValue
	{
		public UnityEngine.Object obj;
		public UnityEngine.Object[] objs;
		public string tag;

        public void Release() {
            InPool(this);
        }

        public static ResValue Create() {
            InitPool();
            ResValue ret = m_Pool.GetObject();
            return ret;
        }

        private void Reset() {
            obj = null;
            objs = null;
            tag = string.Empty;
        }

        private static void InPool(ResValue obj) {
            if (obj == null)
                return;
            InitPool();
            obj.Reset();
            m_Pool.Store(obj);
        }

        private static void InitPool() {
            if (m_InitPool)
                return;
            m_InitPool = true;
            m_Pool.Init(0);
        }

        private static bool m_InitPool = false;
        private static ObjectPool<ResValue> m_Pool = new ObjectPool<ResValue>();
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
        ResValue ret = ResValue.Create();
		ret.obj = obj;
		ret.objs = null;
		ret.tag = tag;
		return ret;
	}

	protected ResValue CreateValue(UnityEngine.Object[] objs, string tag = "")
	{
        ResValue ret = ResValue.Create();
		ret.obj = null;
		ret.objs = objs;
		ret.tag = tag;
		return ret;
	}

	protected virtual bool InternalDestroyResource(ResKey key, ResValue res)
	{
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
            res.Release();
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
		{
			ResourceMgr.Instance.DestroyObject(res);
			return;
		}
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
		{
			ResourceMgr.Instance.DestroyObjects(res);
			return;
		}
		SetResources(target.GetInstanceID(), res, resType, resName, tag);
	}

	public virtual void ClearAllResources()
	{

		// 关闭所有携程
		//StopAllCoroutines();
		StopAllLoadCoroutines();

        // 清理掉所有实例化的Material
        ClearInstanceMaterialMap();

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

            // 進入池
            iter.Current.Value.Release();
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
			{
				// 相等说明当前已经是，所以不需要外面设置了
				mat = value.obj as Material;
				return 1;
			}
		}

		mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		if (mat == null)
			return 0;
		SetResources(target, null, typeof(Material[]));
		SetResource(target, mat, typeof(Material), "", fileName);
		return 2;
	}

	public delegate bool OnGetItem<T>(int index, out T a, out string fileName);
	public delegate bool OnGetItem1<T>(int index, out T a, out string fileName, out string param);

	private static readonly WaitForEndOfFrame m_EndOfFrame = new WaitForEndOfFrame();

    /*
	protected IEnumerator LoadAsync<T>(int start, int end, OnGetItem<T> onGetItem, Func<T, string, bool> onLoad, float delayTime = 0) where T: UnityEngine.Object
	{
		if (start < 0 || end < 0 || end < start || onGetItem == null || onLoad == null)
			yield break;
		
		bool isDelayMode = delayTime > float.Epsilon;
		WaitForSeconds seconds = null;
		if (isDelayMode)
			seconds = new WaitForSeconds(delayTime);

		Delegate key = onGetItem as Delegate;
		for (int i = start; i <= end; ++i)
		{
			T target;
			string fileName;
			if (!onGetItem(i, out target, out fileName))
			{
				StopLoadCoroutine(key);
				yield break;
			}
			if (target != null && !string.IsNullOrEmpty(fileName))
				onLoad(target, fileName);

			if (seconds != null)
				yield return seconds;
			else
				yield return m_EndOfFrame;
		}
			
		StopLoadCoroutine(key);
	}

	protected IEnumerator LoadAsync<T>(int start, int end, OnGetItem1<T> onGetItem, Func<T, string, string, bool> onLoad, float delayTime = 0) where T: UnityEngine.Object
	{
		if (start < 0 || end < 0 || end < start || onGetItem == null || onLoad == null)
			yield break;

		bool isDelayMode = delayTime > float.Epsilon;
		WaitForSeconds seconds = null;
		if (isDelayMode)
			seconds = new WaitForSeconds(delayTime);

		Delegate key = onGetItem as Delegate;
		for (int i = start; i <= end; ++i)
		{
			T target;
			string fileName;
			string param;
			if (!onGetItem(i, out target, out fileName, out param))
			{
				StopLoadCoroutine(key);
				yield break;
			}
			if (target != null && !string.IsNullOrEmpty(fileName))
				onLoad(target, fileName, param);

			if (seconds != null)
				yield return seconds;
			else
				yield return m_EndOfFrame;
		}
			
		StopLoadCoroutine(key);
	}*/

	public bool LoadMaterial(MeshRenderer renderer, string fileName)
	{

		Material mat;
		int result = SetMaterialResource (renderer, fileName, out mat);
		if (result == 0) {
			renderer.sharedMaterial = null;
			return false;
		}

        if (result == 2) {
            renderer.sharedMaterial = mat;
        } else if (result == 1) {
            if (renderer.sharedMaterial == null)
                renderer.sharedMaterial = mat;
        }
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
        else if (result == 1) {
            if (sprite.sharedMaterial == null)
                sprite.sharedMaterial = mat;
        }

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
	
	public static GameObject InstantiateGameObj(GameObject orgObj)
    {
        return ResourceMgr.Instance.InstantiateGameObj(orgObj);
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

		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName);
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

		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName);
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
	
	public bool LoadTexture(MeshRenderer renderer, string fileName, string matName, bool isMatInst = false) {
            if (renderer == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(matName))
                return false;

            Material mat = GetRealMaterial(renderer, isMatInst);
            if (mat == null)
                return false;

            Texture tex = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
            SetResource(renderer, tex, typeof(Texture), matName);
            mat.SetTexture(matName, tex);

            return tex != null;
        }

     public void ClearTexture(MeshRenderer renderer, string matName) {
        if (renderer == null)
            return;

        ClearResource<Texture>(renderer, matName);
        Material mat = GetRealMaterial(renderer, false);
        if (mat != null)
          mat.SetTexture(matName, null);
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

	public void ClearFont(TextMesh textMesh)
	{
		if (textMesh == null)
			return;
		SetResource (textMesh.GetInstanceID (), null, typeof(Font));
	}

	public bool LoadFont(TextMesh textMesh, string fileName)
	{
		if (textMesh == null)
			return false;
		ClearFont (textMesh);
		Font font = ResourceMgr.Instance.LoadFont (fileName, ResourceCacheType.rctRefAdd);
		if (font != null)
			SetResource (textMesh.GetInstanceID (), font, typeof(Font));
		textMesh.font = font;
		return font != null;
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
			
		public bool LoadMainTexture(MeshRenderer renderer, string fileName, bool isMatInst = false)
		{
			if (renderer == null || string.IsNullOrEmpty (fileName) || renderer.sharedMaterial == null)
				return false;
			ClearMainTexture (renderer);
			Texture tex = ResourceMgr.Instance.LoadTexture (fileName, ResourceCacheType.rctRefAdd);
			SetResource(renderer, tex, typeof(Texture), _cMainTex);
            var mat = GetRealMaterial(renderer, isMatInst);
            mat.mainTexture = tex;
			return tex != null;
		}

	public static GameObject CreateGameObject(string fileName)
	{
		GameObject ret = ResourceMgr.Instance.CreateGameObject(fileName);
		return ret;
	}

	public static T CreateGameObject<T>(string fileName) where T: UnityEngine.Component
	{
		GameObject obj = ResourceMgr.Instance.CreateGameObject(fileName);
		if (obj == null)
			return null;
		T ret = obj.GetComponent<T>();
		return ret;
	}

	/*------------------------------------------ 异步方法 ---------------------------------------------*/

	private Dictionary<Delegate, Coroutine> m_EvtCoroutineMap = null;

	protected void StopAllLoadCoroutines()
	{
		if (m_EvtCoroutineMap != null)
		{
			var iter = m_EvtCoroutineMap.GetEnumerator();
			while (iter.MoveNext())
			{
				var c = iter.Current.Value;
				if (c != null)
					StopCoroutine(c);
			}
			iter.Dispose();
			m_EvtCoroutineMap.Clear();
		}
	}
	/*
	protected void StopLoadCoroutine(Delegate key)
	{
		if (key == null || m_EvtCoroutineMap == null)
			return;
		Coroutine value;
		if (m_EvtCoroutineMap.TryGetValue(key, out value))
		{
			m_EvtCoroutineMap.Remove(key);
			if (value != null)
				StopCoroutine(value);
		}
	}

	protected Coroutine StartLoadCoroutine(Delegate key, IEnumerator iter)
	{
		if (key == null || iter == null)
			return null;

		StopLoadCoroutine(key);

		Coroutine ret = StartCoroutine(iter);
		if (ret != null)
		{
			if (m_EvtCoroutineMap == null)
			{
				m_EvtCoroutineMap = new Dictionary<Delegate, Coroutine>();
			} 

			m_EvtCoroutineMap.Add(key, ret);

		}

		return ret;
	}

	protected Coroutine StartLoadCoroutine<T>(OnGetItem<T> evtKey, IEnumerator iter) where T: UnityEngine.Object
	{
		if (evtKey == null || iter == null)
			return null;

		Delegate key = evtKey as Delegate;
		if (key == null)
			return null;

		return StartLoadCoroutine(key, iter);
	}

    
	protected Coroutine StartLoadCoroutine<T>(OnGetItem1<T> evtKey, IEnumerator iter) where T: UnityEngine.Object
	{
		if (evtKey == null || iter == null)
			return null;

		Delegate key = evtKey as Delegate;
		if (key == null)
			return null;

		return StartLoadCoroutine(key, iter);
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<MeshRenderer> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<MeshRenderer>(start, end, onGetItem, LoadMaterial, delayTime));
	}

	public void LoadMaterialAsync(int start, int end, OnGetItem<SpriteRenderer> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<SpriteRenderer>(start, end, onGetItem, LoadMaterial, delayTime));
	}

	public void LoadMainTextureAsync(int start, int end, OnGetItem<MeshRenderer> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<MeshRenderer>(start, end, onGetItem, LoadMainTexture, delayTime));
	}

	public void LoadFontAsync(int start, int end, OnGetItem<TextMesh> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<TextMesh>(start, end, onGetItem, LoadFont, delayTime));
	}

	public void LoadAniControllerAsync(int start, int end, OnGetItem<Animator> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<Animator>(start, end, onGetItem, LoadAniController, delayTime));
	}

	public void LoadSpriteAsync(int start, int end, OnGetItem1<SpriteRenderer> onGetItem, float delayTime = 0)
	{
		StartLoadCoroutine(onGetItem, LoadAsync<SpriteRenderer>(start, end, onGetItem, LoadSprite, delayTime));
	}
    */

    /*
		protected bool LoadAllLoaderGroupBegin(UnityEngine.Object target, LoaderGroupSubNodeType subType) {
            if (target == null || !IsCheckLoaderGroup)
                return false;

            // 针对自己，还要针对上抛的对象考虑，也要通知上抛对象的LoadAll
            if (m_LoaderGroup != null) {
                ++m_LoaderGroupAllRef;
                int instanceId = target.GetInstanceID();
                m_LoaderGroup.LoadAll(instanceId, subType);
                if (!IsTopLoader) {
                    EventDispatcher.Notify<UnityEngine.Object, LoaderGroupSubNodeType>(
                        EnumNotify.CACGE_MANAGER_LOADALL, target, subType);
                }
                return true;
            } else {
                // 判断自己是否已经是顶部UI的UIBASE,如果不是则考虑上抛
                if (!IsTopLoader) {
                    ++m_LoaderGroupAllRef;
                    // 说明不是顶部的UIBase,上抛
                    EventDispatcher.Notify<UnityEngine.Object, LoaderGroupSubNodeType>(
                        EnumNotify.CACGE_MANAGER_LOADALL, target, subType);
                    return true;
                }
            }
            return false;
        }

        protected void LoadAllLoaderGroupEnd() {
            DecLoaderGroupAllRef();
        }

        private int m_LoaderGroupAllRef = 0;
        public bool IsCheckLoaderGroup {
            get {
                return m_LoaderGroupAllRef <= 0;
            }
        }
        public void AddLoaderGroupAllRef() {
            ++m_LoaderGroupAllRef;
        }

        public void DecLoaderGroupAllRef() {
            --m_LoaderGroupAllRef;
            if (m_LoaderGroupAllRef < 0)
                m_LoaderGroupAllRef = 0;
        }
		*/
}
