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
	rctNone = 0, // xiao xin shi yong
	rctTemp,
	rctRefAdd
}

public abstract class IResourceLoader
{
	#region public function
	public abstract bool OnSceneLoad(string sceneName);
	public abstract bool OnSceneLoadAsync(string sceneName, Action onEnd);
	public abstract bool OnSceneClose(string sceneName);
	public abstract Font LoadFont (string fileName, ResourceCacheType cacheType);
	public abstract bool LoadFontAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, Font> onProcess);
	public abstract GameObject LoadPrefab(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess);
	public abstract Material LoadMaterial(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess);
	public abstract Texture LoadTexture(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess);
	public abstract AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAudioClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AudioClip> onProcess);
	public abstract string LoadText(string fileName, ResourceCacheType cacheType);
	public abstract byte[] LoadBytes(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadTextAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, TextAsset> onProcess);
	public abstract RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess);
	public abstract AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess);
	public abstract Shader LoadShader(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess);
	public abstract Sprite[] LoadSprites(string fileName);
	public abstract bool LoadSpritesAsync(string fileName, Action<float, bool, UnityEngine.Object[]> onProcess);
	public abstract ScriptableObject LoadScriptableObject (string fileName, ResourceCacheType cacheType);
	public abstract bool LoadScriptableObjectAsync (string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.ScriptableObject> onProcess);
#if UNITY_5
	public abstract ShaderVariantCollection LoadShaderVarCollection(string fileName, ResourceCacheType cacheType);
	public abstract bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType ResourceCacheType, Action<float, bool, ShaderVariantCollection> onProcess);
#endif
	public abstract AssetCache CreateCache(UnityEngine.Object orgObj, string fileName);
	#endregion public function 
}
