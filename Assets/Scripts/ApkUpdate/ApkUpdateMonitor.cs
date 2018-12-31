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

        private ApkVersionJson m_Jsons = new ApkVersionJson();

        internal bool ChangeState(ApkUpdateState newState)
        {
            if (m_Mgr == null)
                return false;
            return m_Mgr.ChangeState(newState);
        }

        public void Clear()
        {
            if (m_Mgr != null)
            {
                var curState = m_Mgr.CurrState as ApkUpdateBaseState;
                if (curState != null)
                    curState.Clear();
            }
        }

        internal string Https_CurApkVer
        {
            get
            {
                return m_Https_CurApkVer;
            }
        }

        internal bool LoadCurApkVer(string str)
        {
            return m_Jsons.LoadCurrApkVersionJson(str);
        }

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

        public bool Start(string https_curApkVer, string https_ApkDiffs)
        {
            Clear();
            m_Https_CurApkVer = https_curApkVer;
            m_Https_ApkDiffs = https_ApkDiffs;
            if (string.IsNullOrEmpty(m_Https_CurApkVer) || string.IsNullOrEmpty(m_Https_ApkDiffs))
                return false;
            return true;
        }

        /// <summary>
        /// 出现错误的回调
        /// </summary>
        /// <param name="errState">具体哪个状态出错</param>
        internal void OnError(ApkUpdateState errState)
        { 
            
        }
    }
}
