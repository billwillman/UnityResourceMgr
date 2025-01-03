/*----------------------------------------------------------------
// 模块名：资源加载抽象类
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年6月1日
// 模块描述：
//----------------------------------------------------------------*/

using System;
using UnityEngine;

public enum ResourceCacheType
{
	// 只用在读取配置（建议不要使用）
        rctNone = 0, 
        // 临时的,只用在同步的时候，立马实例化Prefab(建议不要使用)
		rctTemp,
        // 对原始资源进行引用计数 +1，切记把LOAD出来的资源当做指针，替换和OnDestroy的时候，不要忘记使用ResourceMgr.Instance.Destroy对引用计数-1
		rctRefAdd
}

	/*
     * 1.此处为底层资源管理模块，另外还有一个基于BaseResLoader的资源管理中间层，可以忽略引用计数，帮你管理引用计数，但基于BaseResLoader没有真正的异步加载（只有帧异步）
     *      能使用基于BaseResLoader的，就使用这个。
     * 2.如果需要使用真正的异步加载，请自己写管理类，来管理ResourceMgr的回调资源，以免导致引用计数并不正确，而资源无法释放。可以参考：MapResLoader中的MASK图异步加载。
     * 3.ResourceMgr.Instance.Destroy为释放对象的方法，可以释放被引用计数的Load资源，也可以释放实例化的GameObject,推荐统一使用这个方法对UNITY对象进行释放
     * 4.Resources.UnloadAsset不要使用了，可以使用ResourceMgr.Instance.Destroy的第二个参数设置为true,前提是：
     *      非常确认外面没有使用的情况下（Sprite要保证它对应的纹理都没有被其他地方使用），否则其他地方会丢失资源。
     * 5.ResourceMgr中的Load方法和CreateGameObject方法以及一切基于BaseResLoader的加载都使用，全小写文件名带后缀名的方式，否则AB中会有问题，因为需要做KEY去查找到底是哪个AB
     * 6.基于BaseResLoader的ref target加载方法，target变量需要是成员变量。
     * 7.实例化出Prefab,请使用简易函数UIBase以及基于BaseResLoader的，可以使用CreateGameObject,使用ResourceMgr可以使用ResourceMgr.Instance.CreateGameObject。
    */

public abstract class IResourceLoader
{
	#region public function
	public abstract bool OnSceneLoad(string sceneName);
	public abstract bool OnSceneLoadAsync(string sceneName, Action onEnd, int priority = 0);
	public abstract bool OnSceneClose(string sceneName);
	public abstract Font LoadFont (string fileName, ResourceCacheType cacheType);
	public abstract bool LoadFontAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, Font> onProcess, int priority = 0);
	public abstract GameObject LoadPrefab(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess, int priority = 0);
	public abstract Material LoadMaterial(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess, int priority = 0);
	public abstract Texture LoadTexture(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess, int priority = 0);
	public abstract AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAudioClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AudioClip> onProcess, int priority = 0);
	public abstract string LoadText(string fileName, ResourceCacheType cacheType);
	public abstract byte[] LoadBytes(string fileName, ResourceCacheType cacheType);
	public abstract TextAsset LoadTextAsset(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadTextAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, TextAsset> onProcess, int priority = 0);
	public abstract RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess, int priority = 0);
	public abstract AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess, int priority = 0);
	public abstract Shader LoadShader(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess, int priority = 0);
	public abstract Sprite[] LoadSprites(string fileName);
	public abstract bool LoadSpritesAsync(string fileName, Action<float, bool, UnityEngine.Object[]> onProcess, int priority = 0);
	public abstract ScriptableObject LoadScriptableObject (string fileName, ResourceCacheType cacheType);
	public abstract bool LoadScriptableObjectAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.ScriptableObject> onProcess, int priority = 0);
#if UNITY_5 || UNITY_2017_1_OR_NEWER
	public abstract ShaderVariantCollection LoadShaderVarCollection(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType ResourceCacheType, Action<float, bool, ShaderVariantCollection> onProcess, int priority = 0);
#endif
	public abstract AssetCache CreateCache(UnityEngine.Object orgObj, string fileName, System.Type orgType);
	#endregion public function 
}
