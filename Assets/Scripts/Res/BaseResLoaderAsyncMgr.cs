using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace NsLib.ResMgr {

     public interface IBaseResLoaderAsyncListener {
        int UUID {
            get;
        }

		bool _OnTextureLoaded(Texture target, ulong subID);
        bool _OnAniControlLoaded(RuntimeAnimatorController target, ulong subID);

		void _RemoveSubID (ulong subID);
    }


    public class ListenerLoaderNode: NoLockPoolNode<ListenerLoaderNode> {
        
		public ulong SubID {
            get;
            protected set;
        }

        public bool isMatInst {
            get;
            protected set;
        }

        public UnityEngine.Object obj {
            get;
            protected set;
        }

		public string fileName {
			get;
			protected set;
		}

		public string resName {
			get;
			protected set;
		}

		public string tag {
			get;
			protected set;
		}

		public static ListenerLoaderNode CreateNode (string fileName, ulong id, UnityEngine.Object obj, bool isMatInst = false, string resName = "", string tag = "") {
            ListenerLoaderNode ret = AbstractNoLockPool<ListenerLoaderNode>.GetNode() as ListenerLoaderNode;
            ret.SubID = id;
            ret.obj = obj;
            ret.isMatInst = isMatInst;
			ret.resName = resName;
			ret.tag = tag;
			ret.fileName = fileName;
            return ret;
        }

        protected override void OnFree() {
            SubID = 0;
            obj = null;
			isMatInst = false;
			resName = string.Empty;
			tag = string.Empty;
			fileName = string.Empty;
        }
    }
    

	public class BaseResLoaderAsyncMgr : Singleton<BaseResLoaderAsyncMgr> {
        private Dictionary<int, IBaseResLoaderAsyncListener> m_ListernMap = new Dictionary<int, IBaseResLoaderAsyncListener>();

        public bool RegListener(IBaseResLoaderAsyncListener listener) {
            if (listener == null)
                return false;
            m_ListernMap[listener.UUID] = listener;
            return true;
        }

        public void RemoveListener(IBaseResLoaderAsyncListener listener) {
            if (listener == null)
                return;
            var uuid = listener.UUID;
            if (m_ListernMap.ContainsKey(uuid))
                m_ListernMap.Remove(uuid);
        }

        public bool LoadAniControllerAsync(string fileName, IBaseResLoaderAsyncListener listener, ulong subID, int loadPriority = 0) {
            if (listener == null || string.IsNullOrEmpty(fileName))
                return false;
            int uuid = listener.UUID;
            listener = null;

            return ResourceMgr.Instance.LoadAniControllerAsync(fileName,
                (float process, bool isDone, RuntimeAnimatorController target) =>
                {
                    if (isDone) {
                        if (target != null) {
                            IBaseResLoaderAsyncListener listen;

                            if (m_ListernMap.TryGetValue(uuid, out listen) && listen != null) {
                                if (!listen._OnAniControlLoaded(target, subID))
                                    ResourceMgr.Instance.DestroyObject(target);
                            } else {
                                ResourceMgr.Instance.DestroyObject(target);
                            }
                        } else {
                            IBaseResLoaderAsyncListener listen;
                            if (m_ListernMap.TryGetValue(uuid, out listen) && listen != null)
                                listen._RemoveSubID(subID);
                        }
                    }
                },
                ResourceCacheType.rctRefAdd, loadPriority);
        }


        public bool LoadTextureAsync(string fileName, IBaseResLoaderAsyncListener listener, ulong subID, int loadPriority = 0) {
            if (listener == null || string.IsNullOrEmpty(fileName))
                return false;

            int uuid = listener.UUID;
            listener = null;

            return ResourceMgr.Instance.LoadTextureAsync(fileName, 
                (float process, bool isDone, Texture target)=>
                {
                    if (isDone) {
						if (target != null)
						{
							IBaseResLoaderAsyncListener listen;

							if (m_ListernMap.TryGetValue(uuid, out listen) && listen != null) {
                            	if (!listen._OnTextureLoaded(target, subID))
                                	ResourceMgr.Instance.DestroyObject(target);
                        	} else {
                            	ResourceMgr.Instance.DestroyObject(target);
                        	}
						} else
						{
							IBaseResLoaderAsyncListener listen;
							if (m_ListernMap.TryGetValue(uuid, out listen) && listen != null)
								listen._RemoveSubID(subID);
						}
					}
                }
                , ResourceCacheType.rctRefAdd, loadPriority);
        }
    }
}
