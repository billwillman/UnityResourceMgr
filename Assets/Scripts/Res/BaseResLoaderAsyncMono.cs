using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace NsLib.ResMgr {

    public enum BaseResLoaderAsyncType {
        SpriteRenderMainTexture = 0,
        MeshRenderMainTexture = 1,
    }

    public class BaseResLoaderAsyncMono : BaseResLoader, IBaseResLoaderAsyncListener {
        private bool m_IsAppQuit = false;
        private static int m_GlobalUUID = 0;
        private int m_UUID = 0;
        private short m_GlobalSubID = 0;

        private LinkedList<INoLockPoolNode<ListenerLoaderNode>> m_LoadingList = null;

        private int MakeGlobalSubID() {
            return ++m_GlobalSubID;
        }

        private bool AddLoadingNode(long subID, UnityEngine.Object obj, bool isMatInst) {
            if (obj == null)
                return false;
            var node = ListenerLoaderNode.CreateNode(subID, obj, isMatInst);
            if (node != null) {
                if (m_LoadingList == null)
                    m_LoadingList = new LinkedList<INoLockPoolNode<ListenerLoaderNode>>();
                m_LoadingList.AddLast(node.PPoolNode);
                return true;
            }
            return false;
        }

        void Awake() {
            m_UUID = ++m_GlobalUUID;
        }

        public int UUID {
            get {
                return m_UUID;
            }
        }

        protected override void OnDestroy() {
            if (!m_IsAppQuit) {
                var mgr = BaseResLoaderAsyncMgr.GetInstance();
                if (mgr == null)
                    return;
                mgr.RemoveListener(this);

                // 回池
                if (m_LoadingList != null) {
                    var node = m_LoadingList.First;
                    while (node != null) {
                        var next = node.Next;
                        if (node.Value != null)
                            node.Value.Dispose();
                        node = next;
                    }

                    m_LoadingList.Clear();
                }
            }

            base.OnDestroy();
        }

        // 外面释放控件的时候要通知，否则会出现列表Obj为NULL情况，特别注意。。。。需要手动回调一下
        public bool OnDestroyObj(UnityEngine.Object obj) {
            if (obj == null)
                return false;
            return RemoveSUBID(obj.GetInstanceID());
        }

        private bool RemoveSUBID(int subID) {
            if (m_LoadingList == null)
                return false;
            bool ret = false;
            var node = m_LoadingList.First;
            while (node != null) {
                var next = node.Next;
                if (node.Value != null) {
                    var n = node.Value as ListenerLoaderNode;
                    long ID = n.SubID;
                    
                    if (GetIntSubID(ID) == subID) {
                        n.Dispose();
                        ret = true;
                    }
                }
                node = next;
            }

            return ret;

        }

        private bool RemoveSUBID(int subID, BaseResLoaderAsyncType asyncType) {
            if (m_LoadingList == null)
                return false;
            var node = m_LoadingList.First;
            while (node != null) {
                var next = node.Next;
                if (node.Value != null) {
                    var n = node.Value as ListenerLoaderNode;
                    long ID = n.SubID;

                    if (GetIntSubID(ID) == subID && GetSubType(ID) == asyncType) {
                        n.Dispose();
                        return true;
                    }
                }
                node = next;
            }

            return false;
        }

        private UnityEngine.Object RemoveSubID(long subID, out bool isMatInst) {
            isMatInst = false;
            if (m_LoadingList == null)
                return null;
            var node = m_LoadingList.First;
            while (node != null) {
                var next = node.Next;
                if (node.Value != null) {
                    var n = node.Value as ListenerLoaderNode;
                    if (n.SubID == subID) {
                        UnityEngine.Object ret = n.obj;
                        isMatInst = n.isMatInst;
                        n.Dispose();
                        return ret;
                    }
                }
                node = next;
            }

            return null;
        }

        private int GetIntSubID(long subID) {
            int ret = (int)(subID & 0x00000000FFFFFFFF);
            return ret;
        }

        private BaseResLoaderAsyncType GetSubType(long subID) {
            BaseResLoaderAsyncType ret = (BaseResLoaderAsyncType)((subID >> 48) & 0xFFFF);
            return ret;
        }

        private long MakeLongSubID(int subID, BaseResLoaderAsyncType asyncType) {
            long ret = ((long)subID) & ((long)asyncType << 32) & ((long)MakeGlobalSubID() << 48);
            return ret;
        }

        protected bool OnTextureLoaded(Texture target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst) {
            if (target != null && obj != null) {
                SetResource<Texture>(obj, target);

                switch (asyncType) {
                    case BaseResLoaderAsyncType.SpriteRenderMainTexture: {
                            SpriteRenderer r1 = obj as SpriteRenderer;
                            var m1 = GetRealMaterial(r1, isMatInst);
                            if (m1 == null)
                                return false;
                            m1.mainTexture = target;
                            m1.SetTexture("_MainTex", target);
                            break;
                        }
                    case BaseResLoaderAsyncType.MeshRenderMainTexture:
                        MeshRenderer r2 = obj as MeshRenderer;
                        var m2 = GetRealMaterial(r2, isMatInst);
                        if (m2 == null)
                            return false;
                        m2.mainTexture = target;

                        break;
                    default:
                        return false;
                }

                return true;
            }
         
            return false;
        }

        public bool _OnTextureLoaded(Texture target, long subID) {
            bool isMatInst;
            UnityEngine.Object obj = RemoveSubID(subID, out isMatInst);
            if (obj != null) {
                if (!OnTextureLoaded(target, obj, GetSubType(subID), isMatInst))
                    return false;
            }
            return obj != null;
        }

        private bool ReMake(UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst, out long id) {
            id = 0;
            if (obj == null)
                return false;
            int subID = obj.GetInstanceID();
            RemoveSUBID(subID, asyncType);
            id = MakeLongSubID(subID, asyncType);
            return AddLoadingNode(id, obj, isMatInst);
        }

        // 加载
        public bool LoadMainTextureAsync(string fileName, SpriteRenderer renderer, bool isMatInst = false, int loadPriority = 0) {
            if (renderer == null)
                return false;

            long id;
            if (!ReMake(renderer,  BaseResLoaderAsyncType.SpriteRenderMainTexture, isMatInst, out id))
                return false;

            var mgr = BaseResLoaderAsyncMgr.GetInstance();
            if (mgr != null) {
                return mgr.LoadTextureAsync(fileName, this, id, loadPriority);
            }
            return false;
        }

        void OnApplicationQuit() {
            m_IsAppQuit = true;
        }
    }

}
