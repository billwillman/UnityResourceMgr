using System;
using UnityEngine;

namespace NsLib.ApkUpdate
{
    internal static class ApkNative
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass m_fee = new AndroidJavaClass("com.UnityResources.Test.MainActivity");
#endif
        // 返回负数说明有错误
        static public int GetCurrentVersionCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string verCode = m_fee.CallStatic<string>("GetCurrentVersionCode");
            Debug.LogFormat("current VersionCode: {0:D}", verCode);
            int ret;
            if (!int.TryParse(verCode, out ret))
                ret = -1;
            return ret;
#else
            return 1;
#endif
        }

        public static string GetSDCardPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        return m_fee.CallStatic<string>("GetSDCardPath");
#else
            return string.Empty;
#endif
        }
    }
}