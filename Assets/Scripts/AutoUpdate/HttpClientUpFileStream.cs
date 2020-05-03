using System;
using System.IO;

namespace NsHttpClient {
    public class HttpClientUpFileStream: HttpClientResponse {
        public HttpClientUpFileStream(string fileName, int bufSize): base(bufSize) {
            m_FilePath = fileName;
            m_Process = 0;
            m_FileStream = null;
            m_FileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            m_FileLength = m_FileStream.Length;
        }

        private void FileClose() {
            lock (this) {
                if (m_FileStream != null) {
                    m_FileStream.Dispose();
                    m_FileStream = null;
                }
            }
        }

        protected override void DoClose() {
            base.DoClose();

            FileClose();
        }

        public override void OnRequested() {
            base.OnRequested();

            FileClose();
        }

        public override bool WritePostStream(Stream stream) {
            long curProcess = this.Process;
            if (curProcess < m_FileLength) {
                int readSize;
                lock (this) {
                    readSize = m_FileStream.Read(m_Buf, 0, m_Buf.Length);
                }
                if (readSize > 0) {
                    stream.Write(m_Buf, 0, readSize);
                } else
                    return false;

                curProcess += readSize;
                this.Process = curProcess;

                return (curProcess < m_FileLength);
            }
            return false;
        }

        public override long GetPostContentLength() {
            return m_FileLength;
        }

        public override string GetPostFileName() {
            return m_FilePath;
        }

        public long Process {
            get {
                lock(this) {
                    return m_Process;
                }
            }

            private set {
                lock (this) {
                    m_Process = value;
                }
            }
        }

        private FileStream m_FileStream = null;
        private string m_FilePath = string.Empty;
        private long m_FileLength = 0;
        private long m_Process = 0;
    }
}