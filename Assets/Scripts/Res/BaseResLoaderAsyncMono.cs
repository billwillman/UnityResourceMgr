using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace NsLib.ResMgr {

    public enum BaseResLoaderAsyncType {
        SpriteRenderMainTexture = 0,
        MeshRenderMainTexture,
        UITextureMainTexture,
        UITextureShader,
        UISpriteMainTexture,
        UI2DSpriteMainTexture,
        UI2DSpriteShader,
        AnimatorController,
        TextMeshFont,
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

		protected bool AddLoadingNode(string fileName, ulong subID, UnityEngine.Object obj, bool isMatInst, string resName = "", string tag = "") {
            if (obj == null)
                return false;
			var node = ListenerLoaderNode.CreateNode(fileName, subID, obj, isMatInst, resName, tag);
            if (node != null) {
                if (m_LoadingList == null)
                    m_LoadingList = new LinkedList<INoLockPoolNode<ListenerLoaderNode>>();
                m_LoadingList.AddLast(node.PPoolNode);
                return true;
            }
            return false;
        }

        protected virtual void Awake() {
            m_UUID = ++m_GlobalUUID;
			var mgr = BaseResLoaderAsyncMgr.GetInstance ();
			if (mgr != null)
				mgr.RegListener (this);
        }

        public int UUID {
            get {
                return m_UUID;
            }
        }

		public override void ClearAllResources()
		{
			if (!m_IsAppQuit) {
				var mgr = BaseResLoaderAsyncMgr.GetInstance();
				if (mgr != null)
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

			base.ClearAllResources ();
		}

        // 清除所有OBJ的正在异步加载的队列
        public bool ClearAllLoadingAsync(UnityEngine.Object obj) {
            if (obj == null)
                return false;
            return RemoveSUBID(obj.GetInstanceID());
        }

        protected bool RemoveSUBID(int subID) {
            if (m_LoadingList == null)
                return false;
            bool ret = false;
            var node = m_LoadingList.First;
            while (node != null) {
                var next = node.Next;
                if (node.Value != null) {
                    var n = node.Value as ListenerLoaderNode;
					ulong ID = n.SubID;
                    
                    if (GetIntSubID(ID) == subID) {
                        n.Dispose();
                        ret = true;
                    }
                }
                node = next;
            }

            return ret;

        }

		private bool RemoveSUBID(string fileName, int subID, BaseResLoaderAsyncType asyncType, out bool isSame) {
			isSame = false;
            if (m_LoadingList == null)
                return false;
            var node = m_LoadingList.First;
            while (node != null) {
                var next = node.Next;
                if (node.Value != null) {
                    var n = node.Value as ListenerLoaderNode;
					ulong ID = n.SubID;

                    if (GetIntSubID(ID) == subID && GetSubType(ID) == asyncType) {
						isSame = string.Compare (fileName, n.fileName, true) == 0;
						if (isSame)
							return false;
						
                        n.Dispose();
                        return true;
                    }
                }
                node = next;
            }

            return false;
        }

		public void _RemoveSubID (ulong subID)
		{
			bool isMatInst;
			string resName, tag;
			RemoveSubID (subID, out isMatInst, out resName, out tag);
		}

		protected UnityEngine.Object RemoveSubID(ulong subID, out bool isMatInst, out string resName, out string tag) {
            isMatInst = false;
			resName = string.Empty;
			tag = string.Empty;
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
						resName = n.resName;
						tag = n.tag;
                        n.Dispose();
                        return ret;
                    }
                }
                node = next;
            }

            return null;
        }

        private int GetIntSubID(ulong subID) {
            int ret = (int)(subID & 0x00000000FFFFFFFF);
            return ret;
        }

		private BaseResLoaderAsyncType GetSubType(ulong subID) {
            BaseResLoaderAsyncType ret = (BaseResLoaderAsyncType)((subID >> 32) & 0xFFFF);
            return ret;
        }

        private ulong MakeLongSubID(int subID, BaseResLoaderAsyncType asyncType) {
			ulong v1 = (ulong)subID & 0xFFFFFFFF;
			ulong v2 = ((ulong)asyncType & 0xFFFF) << 32;
			ulong v3 = ((ulong)MakeGlobalSubID () & 0xFFFF) << 48;
			ulong ret = (v1 | v2 | v3);
			return ret;
        }

		protected int ReMake(string fileName, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst, out ulong id, string resName = "", string tag = "") {
            id = 0;
            if (obj == null)
                return -1;
            int subID = obj.GetInstanceID();
			bool isSame;
			RemoveSUBID(fileName, subID, asyncType, out isSame);
			if (!isSame) {
				id = MakeLongSubID (subID, asyncType);
				if (AddLoadingNode (fileName, id, obj, isMatInst, resName, tag))
					return 1;
				return -1;
			} else
				return 0;
        }

        void OnApplicationQuit() {
            m_IsAppQuit = true;
        }

        /*----------------------------------- 归属负责这些控件异步的，在这些控件被外部删除，请调用这个 ---------------------------*/
        // 外面释放控件的时候要通知，否则会出现列表Obj为NULL情况，特别注意。。。。需要手动回调一下
        public bool OnDestroyObj(UnityEngine.Object obj) {
            return ClearAllLoadingAsync(obj);
        }

        /*----------------------------------- 主动触发异步加载 -------------------------------------------------------------*/

        public bool LoadFontAsync(string fileName, TextMesh obj, int loadPriority = 0) {
            if (obj == null)
                return false;
            var mgr = BaseResLoaderAsyncMgr.GetInstance();
            if (mgr != null) {
                ulong id;
                int rk = ReMake(fileName, obj, BaseResLoaderAsyncType.TextMeshFont, false, out id);
                if (rk < 0)
                    return false;
                if (rk == 0)
                    return true;

                return mgr.LoadFontAsync(fileName, this, id, loadPriority);
            }

            return false;
        }

        public bool LoadAniControllerAsync(string fileName, Animator obj, int loadPriority = 0) {
            if (obj == null)
                return false;
            var mgr = BaseResLoaderAsyncMgr.GetInstance();
            if (mgr != null) {
                ulong id;
                int rk = ReMake(fileName, obj, BaseResLoaderAsyncType.AnimatorController, false, out id);
                if (rk < 0)
                    return false;
                if (rk == 0)
                    return true;

                return mgr.LoadAniControllerAsync(fileName, this, id, loadPriority);
            }

            return false;
        }

        // 加载
        public bool LoadMainTextureAsync(string fileName, SpriteRenderer renderer, bool isMatInst = false, int loadPriority = 0) {
            if (renderer == null)
                return false;

            var mgr = BaseResLoaderAsyncMgr.GetInstance();
            if (mgr != null) {
				ulong id;
				int rk = ReMake (fileName, renderer, BaseResLoaderAsyncType.SpriteRenderMainTexture, isMatInst, out id, _cMainTex);
				if (rk < 0)
                    return false;
				if (rk == 0)
					return true;
				
                return mgr.LoadTextureAsync(fileName, this, id, loadPriority);
            }
            return false;
        }

        public bool LoadMainTextureAsync(string fileName, MeshRenderer renderer, bool isMatInst = false, int loadPriority = 0) {
            if (renderer == null)
                return false;

            var mgr = BaseResLoaderAsyncMgr.GetInstance();
            if (mgr != null) {
				ulong id;
				int rk = ReMake (fileName, renderer, BaseResLoaderAsyncType.MeshRenderMainTexture, isMatInst, out id, _cMainTex);
				if (rk < 0)
                    return false;
				if (rk == 0)
					return true;

                return mgr.LoadTextureAsync(fileName, this, id, loadPriority);
            }
            return false;
        }

/*---------------------------------------------------- 异步加载回调 --------------------------------------------------------------*/

        protected virtual bool OnTextureLoaded(Texture target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst, string resName, string tag) {
            if (target != null && obj != null) {

                switch (asyncType) {
                    case BaseResLoaderAsyncType.SpriteRenderMainTexture: {
                            SpriteRenderer r1 = obj as SpriteRenderer;
                            var m1 = GetRealMaterial(r1, isMatInst);
                            if (m1 == null)
                                return false;
                            m1.mainTexture = target;
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

                SetResource<Texture>(obj, target, resName, tag);

                return true;
            }

            return false;
        }

        protected virtual bool OnShaderLoaded(Shader target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, bool isMatInst, string resName, string tag) {
                return false;
        }

        public bool _OnShaderLoaded(Shader target, ulong subID) {
            bool isMatInst;
            string resName, tag;
            UnityEngine.Object obj = RemoveSubID(subID, out isMatInst, out resName, out tag);
            if (obj != null) {
                if (!OnShaderLoaded(target, obj, GetSubType(subID), isMatInst, resName, tag))
                    return false;
            }
            return obj != null;
        }

        public bool _OnTextureLoaded(Texture target, ulong subID) {
            bool isMatInst;
            string resName, tag;
            UnityEngine.Object obj = RemoveSubID(subID, out isMatInst, out resName, out tag);
            if (obj != null) {

                if (!OnTextureLoaded(target, obj, GetSubType(subID), isMatInst, resName, tag))
                    return false;
            }
            return obj != null;
        }

        protected virtual bool OnAniControlLoaded(RuntimeAnimatorController target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, string resName, string tag) {
            if (target != null && obj != null) {
                switch (asyncType) {
                    case BaseResLoaderAsyncType.AnimatorController:
                        Animator ani = obj as Animator;
                        ani.runtimeAnimatorController = target;
                        break;
                    default:
                        return false;

                }

                SetResource<RuntimeAnimatorController>(obj, target, resName, tag);
                return true;
            }

            return false;
        }

        public bool _OnAniControlLoaded(RuntimeAnimatorController target, ulong subID) {
            bool isMatInst;
            string resName, tag;
            UnityEngine.Object obj = RemoveSubID(subID, out isMatInst, out resName, out tag);

            if (obj != null) {

                if (!OnAniControlLoaded(target, obj, GetSubType(subID), resName, tag))
                    return false;
            }
            return obj != null;
        }

        protected virtual bool OnFontLoaded(Font target, UnityEngine.Object obj, BaseResLoaderAsyncType asyncType, string resName, string tag) {
            if (target != null && obj != null) {
                switch (asyncType) {
                    case BaseResLoaderAsyncType.TextMeshFont:
                        TextMesh text = obj as TextMesh;
                        text.font = target;
                        break;
                    default:
                        return false;

                }

                SetResource<Font>(obj, target, resName, tag);
                return true;
            }

            return false;
        }

        public bool _OnFontLoaded(Font target, ulong subID) {
            bool isMatInst;
            string resName, tag;
            UnityEngine.Object obj = RemoveSubID(subID, out isMatInst, out resName, out tag);
            if (obj != null) {
                if (!OnFontLoaded(target, obj, GetSubType(subID), resName, tag))
                    return false;
            }
            return obj != null;
        }
    }

}
