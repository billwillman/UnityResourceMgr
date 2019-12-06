using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NsLib.ApkUpdate
{

    public enum ApkUpdateError
    {
        // 获得当前VersonCode错误
        Get_Local_VersionCode_Error = 0,
        // 获得服务器Verson失败
        Get_Server_Version_Error,
        Get_Server_Version_Url_Error,
        Get_Local_ApkSavePath_Error,
        Get_Local_DiffZipSavePath_Error,
        FILE_APK_ERROR,
        Get_Server_ApkDiff_Url_Error,
        Get_Server_ApkDiff_Error,
        // 没有对应的APK DIFF描述
        Get_Server_NoApkDiffInfo,
        Get_Server_DiffZip_Url_Error,
       Get_Server_DiffZip_Down_Error,
    }

    public interface IApkUpdateMonitor
    {
        string Get_Https_CurApkVer();
        string Get_Https_ApkDiffs();

        string Get_Http_DiffZipUrl();

        int GetLocalVersionCode();
        // 新APK保存路径
        string GetNewApkSavePath();
        string GetDiffZipSavePath();
    }

    // Apk更新监控器
    public class ApkUpdateMonitor : MonoBehaviour
    {
        private static ApkUpdateMonitor m_Instance = null;

        private ApkUpdateStateMgr m_Mgr = null;

        private IApkUpdateMonitor m_Inter = null;

        private ApkVersionJson m_Jsons = new ApkVersionJson();

        internal IApkUpdateMonitor Inter
        {
            get
            {
                return m_Inter;
            }
        }

        internal int ServerVersionCode
        {
            get
            {
                return m_Jsons.CurApkVer.VersionCode;
            }
        }

        internal bool ChangeState(ApkUpdateState newState)
        {
            if (m_Mgr == null)
                return false;
            return m_Mgr.ChangeState(newState);
        }

        void Start()
        {}

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
                return m_Inter.Get_Https_CurApkVer();
            }
        }

        internal string Https_ApkDiffs
        {
            get
            {
                return m_Inter.Get_Https_ApkDiffs();
            }
        }

        internal bool LoadCurApkVer(string str)
        {
            return m_Jsons.LoadCurrApkVersionJson(str);
        }

        internal localDiffZipInfo LoadLocalDiffZipInfo(string fileName)
        {
            return m_Jsons.LoadLocalDiffZipInfo(fileName);
        }

        internal void SaveLocalDiffZipInfo(string fileName, localDiffZipInfo info)
        {
            m_Jsons.SaveLocalDiffZipInfo(fileName, info);
        }

        internal bool LoadApkDiff(string str)
        {
            return m_Jsons.LoadDiffApkJson(str);
        }

        internal string GetNewApkDiffMd5(int oldVer, int newVer)
        {
            var info = m_Jsons.GetDiffApkInfo(oldVer, newVer);
            if (info == null)
                return string.Empty;
            return info.NewApkMd5;
        }

        internal string GetNewApkDiffMd5()
        {
            return GetNewApkDiffMd5(m_Jsons.CurApkVer.VersionCode, ServerVersionCode);
        }

        internal string GetNewZipDiffMd5(int oldVer, int newVer)
        {
            var info = m_Jsons.GetDiffApkInfo(oldVer, newVer);
            if (info == null)
                return string.Empty;
            return info.DiffZipMd5;
        }

        internal DiffApkInfo GetDiffApkInfo(int oldVer, int newVer)
        {
            var info = m_Jsons.GetDiffApkInfo(oldVer, newVer);
            return info;
        }

        internal DiffApkInfo GetDiffApkInfo()
        {
            return GetDiffApkInfo(m_Jsons.CurApkVer.VersionCode, ServerVersionCode);
        }

        internal string GetNewZipDiffMd5()
        {
            return GetNewZipDiffMd5(m_Jsons.CurApkVer.VersionCode, ServerVersionCode);
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

        public bool Start(IApkUpdateMonitor inter)
        {
            Clear();
            m_Inter = inter;
            if (m_Inter == null || string.IsNullOrEmpty(m_Inter.Get_Https_CurApkVer()) || string.IsNullOrEmpty(m_Inter.Get_Https_ApkDiffs()))
                return false;
            return true;
        }

        /// <summary>
        /// 出现错误的回调
        /// </summary>
        /// <param name="errState">具体哪个状态出错</param>
        internal void OnError(ApkUpdateState errState, ApkUpdateError errType)
        { 
            
        }

        internal static void Log(string fmt, params object[] objs)
        {
            if (string.IsNullOrEmpty(fmt))
                return;
            fmt = StringHelper.Format("[ApkUpdate] {0}", fmt);
            Debug.LogFormat(fmt, objs);
        }

        internal static void Error(string fmt, params object[] objs)
        {
            if (string.IsNullOrEmpty(fmt))
                return;
            fmt = StringHelper.Format("[ApkUpdate] {0}", fmt);
            Debug.LogErrorFormat(fmt, objs);
        }

        static internal long GetFileSize(string fileName)
        {
            if (!File.Exists(fileName))
                return 0;
            try
            {
                FileInfo info = new FileInfo(fileName);
                if (!info.Exists)
                    return 0;
                return info.Length;
            } catch(Exception e)
            {
#if DEBUG
                Debug.LogError(e.ToString());
#endif
            }

            return 0;
        }

        // 获得大文件MD5
        static internal string GetFileMd5(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            int bufferSize = 1024; // 缓冲区大小
            byte[] buff = new byte[bufferSize];

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            md5.Initialize();

            long offset = 0;
            while (offset < fs.Length)
            {
                long readSize = bufferSize;
                if (offset + readSize > fs.Length)
                {
                    readSize = fs.Length - offset;
                }

                fs.Read(buff, 0, Convert.ToInt32(readSize)); // 读取一段数据到缓冲区

                if (offset + readSize < fs.Length) // 不是最后一块
                {
                    md5.TransformBlock(buff, 0, Convert.ToInt32(readSize), buff, 0);
                }
                else // 最后一块
                {
                    md5.TransformFinalBlock(buff, 0, Convert.ToInt32(readSize));
                }

                offset += bufferSize;
            }

            fs.Close();
            byte[] result = md5.Hash;
            md5.Clear();

            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }

            return sb.ToString();
   
        }

        internal void ClearZip(string noDelZipName)
        {
            string savePath = m_Inter.GetDiffZipSavePath();
            if (string.IsNullOrEmpty(savePath))
                return;
            string[] zips = Directory.GetFiles(savePath, "*.zip", SearchOption.TopDirectoryOnly);
            if (zips == null || zips.Length <= 0)
                return;
            for (int i = 0; i < zips.Length; ++i)
            {
                try
                {
                    string fileName = zips[i];
                    if (string.IsNullOrEmpty(fileName))
                        continue;
                    bool isDelete = true;
                    if (!string.IsNullOrEmpty(noDelZipName))
                    {
                        string name = Path.GetFileNameWithoutExtension(fileName);
                        if (string.Compare(noDelZipName, name, true) == 0)
                        {
                            isDelete = false;
                        }
                    }
                    if (isDelete)
                    {
                        File.Delete(fileName);
                        string jsonFileName = Path.ChangeExtension(fileName, ".json");
                        if (File.Exists(jsonFileName))
                            File.Delete(jsonFileName);
                    }
                }
                catch (Exception e)
                {
                    Error(e.ToString());
                }
            }
        }

        internal void ClearApk(string noDelApkName)
        {
            string savePath = m_Inter.GetNewApkSavePath();
            if (string.IsNullOrEmpty(savePath))
                return;
            string[] apks = Directory.GetFiles(savePath, "*.apk", SearchOption.TopDirectoryOnly);
            if (apks == null || apks.Length <= 0)
                return;
            for (int i = 0; i < apks.Length; ++i)
            {
                try
                {
                    string fileName = apks[i];
                    if (string.IsNullOrEmpty(fileName))
                        continue;
                    bool isDelete = true;
                    if (!string.IsNullOrEmpty(noDelApkName))
                    {
                        string name = Path.GetFileNameWithoutExtension(fileName);
                        if (string.Compare(noDelApkName, name, true) == 0)
                        {
                            isDelete = false;
                        }
                    }

                    if (isDelete)
                        File.Delete(fileName);

                } catch (Exception e)
                {
                    Error(e.ToString());
                }
            }
        }


    }
}
