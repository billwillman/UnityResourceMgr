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

        private void OnHttpsCallBack(HttpClient https, HttpListenerStatus status)
        {
            m_Https = null;
            switch (status)
            {
                case HttpListenerStatus.hsError:
                    ApkUpdateMonitor.GetInstance().OnError(this.Id);
                    break;
                case HttpListenerStatus.hsDone:
                    var rep = https.Listener as HttpClientStrResponse;
                    if (rep != null)
                    {
                        string str = rep.Txt;
                        bool isOk = ApkUpdateMonitor.GetInstance().LoadCurApkVer(str);
                        if (!isOk)
                        {
                            ApkUpdateMonitor.GetInstance().OnError(this.Id);
                            return;
                        }
                        // 处理吧，这个获得成功

                    } else
                        ApkUpdateMonitor.GetInstance().OnError(this.Id);
                    break;
                default:
                    ApkUpdateMonitor.GetInstance().OnError(this.Id);
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
                target.OnError(this.Id);
                return;
            }
            m_Https = HttpHelper.OpenUrl<HttpClientStrResponse>(url, new HttpClientStrResponse(), OnHttpsCallBack);
        }

        public override void Exit(ApkUpdateMonitor target)
        {
            base.Exit(target);
            Clear();
        }
    }
}
