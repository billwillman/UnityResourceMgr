using System;
using System.Net;
using System.IO;

namespace NsHttpClient
{
	public class HttpClientStrResponse: HttpClientResponse
	{
        public HttpClientStrResponse() : this(1024) { }
        public HttpClientStrResponse(int bufSize) : base(bufSize) { }
        protected override void Flush(int read) {
            if (m_Stream == null)
                m_Stream = new MemoryStream();
            m_Stream.Write(m_Buf, 0, read);
        }

        protected override void End() {
            lock (m_TxtLock) {
                byte[] buf = m_Stream.ToArray();
                string txt = string.Empty;
                if (buf != null && buf.Length > 0) {
                    txt = System.Text.Encoding.UTF8.GetString(buf);
                }
                lock (m_TxtLock) {
                    m_Txt = txt;
                }
            }
        }

        protected override void DoClose() {
            base.DoClose();
            if (m_Stream != null) {
                m_Stream.Close();
                m_Stream.Dispose();
                m_Stream = null;
            }
        }

        public string Txt {
            get {
                lock (m_TxtLock) {
                    return m_Txt;
                }
            }
        }

        private string m_Txt = string.Empty;
        private System.Object m_TxtLock = new object();
        private MemoryStream m_Stream = null;
    }
}

