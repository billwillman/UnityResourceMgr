using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NsLib.ResMgr {

    public class BaseResLoaderAsyncMono : MonoBehaviour, IBaseResLoaderAsyncListener {
        private bool m_IsAppQuit = false;
        private static int m_GlobalUUID = 0;
        private int m_UUID = 0;

        void Awake() {
            m_UUID = ++m_GlobalUUID;
        }

        public int UUID {
            get {
                return m_UUID;
            }
        }

        void OnDestroy() {
            if (m_IsAppQuit)
                return;
            var mgr = BaseResLoaderAsyncMgr.GetInstance();
            if (mgr == null)
                return;
            mgr.RemoveListener(this);
        }

        void OnApplicationQuit() {
            m_IsAppQuit = true;
        }
    }

}
