using UnityEngine;
using System.Collections;

namespace NsLib.ApkUpdate
{
    // Apk更新监控器
    public class ApkUpdateMonitor : MonoBehaviour
    {
        private ApkUpdateStateMgr m_Mgr = null;

        void Awake()
        {
            m_Mgr = new ApkUpdateStateMgr(this);
        }

        void Update()
        {
            if (m_Mgr != null)
                m_Mgr.Update();
        }
    }
}
