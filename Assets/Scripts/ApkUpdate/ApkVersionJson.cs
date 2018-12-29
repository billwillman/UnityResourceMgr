using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

namespace NsLib.ApkUpdate
{

    internal class CurrentApkVersion
    {
        // 版本号
        public int VersionCode
        {
            get;
            set;
        }

        // 对应Obb的MD5
        public string ObbMd5
        {
            get;
            set;
        }
    }

    // APK差异信息
    internal class DiffApkInfo
    {
        // 差异化的名字，例如："2015-2016"表示版本从2015到1016
        public string DiffName
        {
            get;
            set;
        }

        // 差异化的ZIP包的MD5
        public string DiffZipMd5
        {
            get;
            set;
        }

        // 老APK + 差异化后ZIP生成APK的MD5码（这个可以只检测一部分的MD5，类似UNITY检测OBB的MD5方式）
        public string NewApkMd5
        {
            get;
            set;
        }
    }

    // Apk版本Json
    internal class ApkVersionJson
    {
        // 当前版本的
        private CurrentApkVersion m_CurrApkVer = null;
        // 比较APK的MAP
        private Dictionary<string, DiffApkInfo> m_DiffApkMap = null;

        public bool LoadCurrApkVersionJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            try
            {
                m_CurrApkVer = JsonMapper.ToObject<CurrentApkVersion>(str);
                return m_CurrApkVer != null;
            } catch (Exception e)
            {
#if DEBUG
                Debug.LogError(e.ToString());
#endif
                return false;
            }
        }

        public CurrentApkVersion CurApkVer
        {
            get
            {
                return m_CurrApkVer;
            }
        }

        // 获得Diff信息
        public DiffApkInfo GetDiffApkInfo(int oldVerCode, int newVerCode)
        {
            if (m_DiffApkMap == null)
                return null;
            string key = string.Format("%d-%d", oldVerCode, newVerCode);
            DiffApkInfo ret;
            if (!m_DiffApkMap.TryGetValue(key, out ret))
                ret = null;
            return ret;
        }


        public bool LoadDiffApkJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            try
            {
                m_DiffApkMap = JsonMapper.ToObject<Dictionary<string, DiffApkInfo>>(str);
                return m_DiffApkMap != null;
            } catch (Exception e)
            {
#if DEBUG
                Debug.LogError(e.ToString());
#endif
                return false;
            }
        }
    }
}