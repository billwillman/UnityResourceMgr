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
    }
}
