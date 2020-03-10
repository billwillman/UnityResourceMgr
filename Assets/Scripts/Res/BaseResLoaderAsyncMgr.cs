using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace NsLib.ResMgr {

     public interface IBaseResLoaderAsyncListener {
        int UUID {
            get;
        }
    }

    

    public class BaseResLoaderAsyncMgr : SingetonMono<BaseResLoaderAsyncMgr> {
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

        public bool LoadTextureAsync(string fileName, IBaseResLoaderAsyncListener listener, int subID) {

        }
    }
}
