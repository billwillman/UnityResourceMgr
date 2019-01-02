using System;
using System.Collections.Generic;
using NsHttpClient;

namespace NsLib.ApkUpdate
{
    internal class Apk_CheckApkDiff: ApkUpdateBaseState
    {
        private HttpClient m_Https = null;
        public override void Clear()
        {
            base.Clear();
            if (m_Https != null)
            {
                m_Https.Dispose();
                m_Https = null;
            }
        }

        private void OnCheckApkDiff()
        {
            string apkName = ApkUpdateMonitor.GetInstance().GetNewApkDiffMd5();
            if (string.IsNullOrEmpty(apkName))
            {
                ApkUpdateMonitor.GetInstance().OnError(ApkUpdateState.CheckApkDiff, ApkUpdateError.Get_Server_ApkDiff_Error);
                return;
            }
            ApkUpdateMonitor.GetInstance().ClearApk(apkName);

            ApkUpdateMonitor.GetInstance().ChangeState(ApkUpdateState.CheckLocalNewApk);
        }

        private void OnHttpEnd(HttpClient client, HttpListenerStatus status)
        {
            m_Https = null;
            switch (status)
            {
                case HttpListenerStatus.hsError:
                    ApkUpdateMonitor.GetInstance().OnError(ApkUpdateState.CheckApkDiff, ApkUpdateError.Get_Server_ApkDiff_Error);
                    break;
                case HttpListenerStatus.hsDone:
                    HttpClientStrResponse rep = client.Listener as HttpClientStrResponse;
                    if (rep != null)
                    {
                        bool ret = ApkUpdateMonitor.GetInstance().LoadApkDiff(rep.Txt);
                        if (!ret)
                        {
                            ApkUpdateMonitor.GetInstance().OnError(ApkUpdateState.CheckApkDiff, ApkUpdateError.Get_Server_ApkDiff_Error);
                            return;
                        }
                        // 更新完了
                        OnCheckApkDiff();
                    }
                    else
                        ApkUpdateMonitor.GetInstance().OnError(ApkUpdateState.CheckApkDiff, ApkUpdateError.Get_Server_ApkDiff_Error);
                    break;
                default:
                    ApkUpdateMonitor.GetInstance().OnError(ApkUpdateState.CheckApkDiff, ApkUpdateError.Get_Server_ApkDiff_Error);
                    break;
            }
        }

        public override void Enter(ApkUpdateMonitor target)
        {
            base.Enter(target);
            Clear();
            string url = target.Inter.Get_Https_ApkDiffs();
            if (string.IsNullOrEmpty(url))
            {
                target.OnError(ApkUpdateState.CheckApkDiff, ApkUpdateError.Get_Server_ApkDiff_Url_Error);
                return;
            }
            url = HttpHelper.AddTimeStamp(url);
            m_Https = HttpHelper.OpenUrl<HttpClientStrResponse>(url, new HttpClientStrResponse(), OnHttpEnd);
        }

    }
}
