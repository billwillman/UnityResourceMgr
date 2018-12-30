using UnityEngine;
using System.Collections;

namespace NsLib.ApkUpdate
{
    // Apk更新监控器
    public class ApkUpdateMonitor : MonoBehaviour
    {
        private static ApkUpdateMonitor m_Instance = null;

        private ApkUpdateStateMgr m_Mgr = null;

        private string m_Https_CurApkVer = string.Empty;
        private string m_Https_ApkDiffs = string.Empty;

        public static ApkUpdateMonitor GetInstance()
        {
            if (m_Instance == null)
            {
                GameObject obj = new GameObject("ApkUpdateMonitor", typeof(ApkUpdateMonitor));
                m_Instance = obj.GetComponent<ApkUpdateMonitor>();
            }
            return m_Instance;
        }

        public static ApkUpdateMonitor Instance
        {
            get
            {
                return GetInstance();
            }
        }

        

        void Awake()
        {
            m_Mgr = new ApkUpdateStateMgr(this);
        }

        void Update()
        {
        }

        /// <summary>
        /// 出现错误的回调
        /// </summary>
        /// <param name="errState">具体哪个状态出错</param>
        internal void OnError(ApkUpdateState errState)
        { }
    }
}
