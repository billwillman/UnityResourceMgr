using UnityEngine;
using System.Collections;

namespace NsLib.ApkUpdate
{
    // Apk更新监控器
    public class ApkUpdateMonitor : MonoBehaviour
    {
        private ApkUpdateMgr m_Mgr = new ApkUpdateMgr();

        void Update()
        {
            if (m_Mgr != null)
                m_Mgr.Update();
        }
    }
}
