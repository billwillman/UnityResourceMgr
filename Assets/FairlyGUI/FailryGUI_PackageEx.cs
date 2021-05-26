using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public partial class UIPackage
    {
        private static LoadResource m_EvtDoLoad = null;
        private static void InitEvents() {
            if (m_EvtDoLoad == null)
                m_EvtDoLoad = new LoadResource(DoLoad);
        }

        public delegate System.Object _DestroyMethod(string fileName, ref DestroyMethod method);

        private static Dictionary<System.Type, _DestroyMethod> m_ResLoadMap = new Dictionary<Type, _DestroyMethod>();
        private static void InitResLoadMap() {
            if (m_ResLoadMap.Count > 0)
                return;

            m_ResLoadMap[typeof(TextAsset)] = _LoadBytes;
            m_ResLoadMap[typeof(Texture)] = _LoadTexture;
            m_ResLoadMap[typeof(Texture2D)] = _LoadTexture;
            m_ResLoadMap[typeof(AudioClip)] = _LoadAudioClip;

            NTexture.CustomDestroyMethod += _UnloadObject;
            NAudioClip.CustomDestroyMethod += _UnloadObject;
        }

        private static void _UnloadObject(Texture target) {
            ResourceMgr.Instance.DestroyObject(target);
        }

        private static void _UnloadObject(AudioClip target) {
            ResourceMgr.Instance.DestroyObject(target);
        }

        private static System.Object _LoadBytes(string fileName, ref DestroyMethod method) {
            method = DestroyMethod.None;
            byte[] buffer = ResourceMgr.Instance.LoadBytes(fileName);
            return buffer;
        }

        private static System.Object _LoadTexture(string fileName, ref DestroyMethod method) {
            Texture ret = ResourceMgr.Instance.LoadTexture(fileName, ResourceCacheType.rctRefAdd);
            return ret;
        }

        private static System.Object _LoadAudioClip(string fileName, ref DestroyMethod method) {
            AudioClip ret = ResourceMgr.Instance.LoadAudioClip(fileName, ResourceCacheType.rctRefAdd);
            return ret;
        }

        private static System.Object _CallResLoad(string fileName, System.Type t, ref DestroyMethod destroyMethod) {
            if (t == null)
                return null;
            InitResLoadMap();
            _DestroyMethod callBack;
            if (m_ResLoadMap.TryGetValue(t, out callBack) && callBack != null) {
                return callBack(fileName, ref destroyMethod);
            }
            return null;
        }

        private static System.Object DoLoad(string name, string extension, System.Type type, out DestroyMethod destroyMethod) {
            destroyMethod = DestroyMethod.Custom;
            string fileName = StringHelper.Format("{0}{1}", name, extension);

            return _CallResLoad(fileName, type, ref destroyMethod);
        }
        protected static ByteBuffer _GetByteBuffer(System.Object obj) {
            if (obj == null)
                return null;
            ByteBuffer buffer = null;
            if (obj is TextAsset) {
                buffer = new ByteBuffer(((TextAsset)obj).bytes);
            } else if (obj is byte[]) {
                buffer = new ByteBuffer(((byte[])obj));
            }
            return buffer;
        }

        public static UIPackage AddPackageEx(string assetPath) {
            if (string.IsNullOrEmpty(assetPath))
                return null;
            InitEvents();
            return AddPackage(assetPath, m_EvtDoLoad);
        }
    }
}
