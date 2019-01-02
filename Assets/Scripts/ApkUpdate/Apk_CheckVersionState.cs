using System;
using System.Collections.Generic;
using NsHttpClient;

namespace NsLib.ApkUpdate
{
    internal class Apk_CheckVersionState : ApkUpdateBaseState
    {
        private HttpClient m_Https = null;

        public override void Clear()
        {
            if (m_Https == null)
            {
                m_Https.Dispose();
                m_Https = null;
            }
        }

        private void OnCheckVersion()
        {
            // 检测当前版本, 获得当前本地的VersionCode
            int localVerCode = ApkUpdateMonitor.GetInstance().Inter.GetLocalVersionCode();
            if (localVerCode < 0)
            {
                ApkUpdateMonitor.GetInstance().OnError(this.Id, ApkUpdateError.Get_Local_VersionCode_Error);
                return;
            }

            int serverVerCode = ApkUpdateMonitor.GetInstance().ServerVersionCode;
            ApkUpdateMonitor.Log("local versionCode: %d, server versionCode: %d", localVerCode, serverVerCode);
            if (serverVerCode > localVerCode)
            {
                // 说明要更新, 先检查本地是否有生成好的APK
                ApkUpdateMonitor.GetInstance().ChangeState(ApkUpdateState.CheckApkDiff);
            } else
            {
                // 说明本地比服务器版本还高则忽略

                // 检查OBB完整性
                ApkUpdateMonitor.GetInstance().ChangeState(ApkUpdateState.CheckObbMd5);
            }
        }

        private void OnHttpsCallBack(HttpClient https, HttpListenerStatus status)
        {
            m_Https = null;
            switch (status)
            {
                case HttpListenerStatus.hsError:
                    ApkUpdateMonitor.GetInstance().OnError(this.Id, ApkUpdateError.Get_Server_Version_Error);
                    break;
                case HttpListenerStatus.hsDone:
                    var rep = https.Listener as HttpClientStrResponse;
                    if (rep != null)
                    {
                        string str = rep.Txt;
                        bool isOk = ApkUpdateMonitor.GetInstance().LoadCurApkVer(str);
                        if (!isOk)
                        {
                            ApkUpdateMonitor.GetInstance().OnError(this.Id, ApkUpdateError.Get_Server_Version_Error);
                            return;
                        }
                        // 处理吧，这个获得成功
                        OnCheckVersion();
                    } else
                        ApkUpdateMonitor.GetInstance().OnError(this.Id, ApkUpdateError.Get_Server_Version_Error);
                    break;
                default:
                    ApkUpdateMonitor.GetInstance().OnError(this.Id, ApkUpdateError.Get_Server_Version_Error);
                    break;
            }
        }

        public override void Enter(ApkUpdateMonitor target)
        {
            base.Enter(target);
            Clear();

            // 重新请求
            string url = ApkUpdateMonitor.GetInstance().Https_CurApkVer;
            if (string.IsNullOrEmpty(url))
            {
                target.OnError(this.Id, ApkUpdateError.Get_Server_Version_Url_Error);
                return;
            }
            url = HttpHelper.AddTimeStamp(url);
            m_Https = HttpHelper.OpenUrl<HttpClientStrResponse>(url, new HttpClientStrResponse(), OnHttpsCallBack);
        }

        public override void Exit(ApkUpdateMonitor target)
        {
            base.Exit(target);
            Clear();
        }
    }
}
