using System;
using UnityEngine;

namespace NsLib.ResMgr {

    public class EmptyLoader: IResourceLoader {
        public override bool OnSceneLoad(string sceneName) {
            return false;
        }

        public override bool OnSceneLoadAsync(string sceneName, Action onEnd, int priority = 0) {
            return false;
        }

        public override bool OnSceneClose(string sceneName) {
            return false;
        }

        public override Font LoadFont(string fileName, ResourceCacheType cacheType) {
            return null;
        }

        public override bool LoadFontAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Font> onProcess, int priority = 0) {
            return false;
        }

        public override GameObject LoadPrefab(string fileName, ResourceCacheType cacheType) {
            return null;
        }

        public override bool LoadPrefabAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, GameObject> onProcess, int priority = 0) {
            return false;
        }
        public override Material LoadMaterial(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadMaterialAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Material> onProcess, int priority = 0) {
            return false;
        }
        public override Texture LoadTexture(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadTextureAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Texture> onProcess, int priority = 0) {
            return false;
        }
        public override AudioClip LoadAudioClip(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadAudioClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AudioClip> onProcess, int priority = 0) {
            return false;
        }
        public override string LoadText(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override byte[] LoadBytes(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadTextAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, TextAsset> onProcess, int priority = 0) {
            return false;
        }
        public override RuntimeAnimatorController LoadAniController(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadAniControllerAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, RuntimeAnimatorController> onProcess, int priority = 0) {
            return false;
        }
        public override AnimationClip LoadAnimationClip(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadAnimationClipAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, AnimationClip> onProcess, int priority = 0) {
            return false;
        }
        public override Shader LoadShader(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadShaderAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, Shader> onProcess, int priority = 0) {
            return false;
        }
        public override Sprite[] LoadSprites(string fileName) {
            return null;
        }
        public override bool LoadSpritesAsync(string fileName, Action<float, bool, UnityEngine.Object[]> onProcess, int priority = 0) {
            return false;
        }
        public override ScriptableObject LoadScriptableObject(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadScriptableObjectAsync(string fileName, ResourceCacheType cacheType, Action<float, bool, UnityEngine.ScriptableObject> onProcess, int priority = 0) {
            return false;
        }
#if UNITY_5
        public override ShaderVariantCollection LoadShaderVarCollection(string fileName, ResourceCacheType cacheType) {
            return null;
        }
        public override bool LoadShaderVarCollectionAsync(string fileName, ResourceCacheType ResourceCacheType, Action<float, bool, ShaderVariantCollection> onProcess, int priority = 0) {
            return false;
        }
#endif
        public override AssetCache CreateCache(UnityEngine.Object orgObj, string fileName, System.Type orgType) {
            return null;
        }
    }
}