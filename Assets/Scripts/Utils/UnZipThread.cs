using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Utils
{
	public class UnZipThread: CustomThread
	{
		public enum UnZipStatus
        {
            // 无状态
            uzNone,
            // 正在解压
            uzDoing,
            // 解压完成
            uzOk,
            // 解压失败
            uzFail
        }

        public UnZipThread(string fileName, string exportDir = "", bool isUnZipDelete = true, Action<bool> onEnd = null): base()
        {
            m_UnZipDelete = isUnZipDelete;
            m_FileName = fileName;
            m_ExportDir = exportDir;
            OnZipedEnd = onEnd;
            if (!string.IsNullOrEmpty (m_ExportDir)) {
                if (!m_ExportDir.EndsWith("/"))
                    m_ExportDir += "/";
            }
            if (!File.Exists (m_FileName)) {
                Status = UnZipStatus.uzFail;
                if (OnZipedEnd != null) {
                    OnZipedEnd (false);
                }
                return;
            }

            Process = 0f;
            m_Cnt = 0;
            m_Cur = 0;

            Start ();
        }

        // 解压进度
        public float Process
        {
            get
            {
                lock (this) {
                    return m_Process;
                }
            }

            private set {
                lock (this) {
                    m_Process = value;
                }
            }
        }

        private Action<bool> OnZipedEnd
        {
            get;
            set;
        }

        // 解压状态
        public UnZipStatus Status
        {
            get
            {
                lock (this) {
                    return m_UnZipStatus;
                }
            }

            private set {
                lock (this) {
                    if (m_UnZipStatus == value)
                        return;
                    m_UnZipStatus = value;
                }
            }
        }

        // 初始化UNZIP
        private void InitUnZip()
        {
            if (m_Stream != null)
                return;
            m_Stream = new ZipInputStream (File.OpenRead (m_FileName));
            m_Cnt = m_Stream.Length;
            Process = 0;
        }
		
		private byte[] m_UnZipBuf = new byte[2048];

        private void UnzipOneFile(ZipEntry theEntry, ZipInputStream s, string strDirectory, bool overWrite) {
            string directoryName = string.Empty;
            string pathToZip = theEntry.Name;
            if (!string.IsNullOrEmpty(pathToZip))
                directoryName = Path.GetDirectoryName(pathToZip) + "/";

            string fileName = Path.GetFileName(pathToZip);

            string exportDir = strDirectory + directoryName;
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            if (!string.IsNullOrEmpty(fileName)) {
                string newFileName = exportDir + fileName;
                if ((File.Exists(newFileName) && overWrite)
                    || (!File.Exists(newFileName))) {
                    using (FileStream streamWriter = new FileStream(newFileName, FileMode.Create, FileAccess.Write)) {
                        float d = (float)theEntry.CompressedSize / (float)theEntry.Size;
                        int size = m_UnZipBuf.Length;
                        byte[] data = m_UnZipBuf;
                        while (true) {
                            size = s.Read(data, 0, data.Length);

                            if (size > 0) {
                                m_Cur += size * d;
                                Process = ((float)m_Cur) / ((float)m_Cnt);
                        //       UnityEngine.Debug.LogFormat ("UnZip Process: {0}", Process.ToString ());
                                streamWriter.Write(data, 0, size);
                                streamWriter.Flush ();

                        //        System.Threading.Thread.Sleep (1);
                            }
                            else {
                              //  m_Cur += theEntry.CompressedSize;
                              //  Process = ((float)m_Cur) / ((float)m_Cnt);
                            //    UnityEngine.Debug.LogFormat ("UnZip Process: {0}", Process.ToString ());
                                break;
                            }
                        }
                        streamWriter.Close();
                        streamWriter.Dispose ();
                    }
                }
            }

        }

        private void ProcessUnZip()
        {
            if (m_Stream != null) {
                Status = UnZipStatus.uzDoing;
                ZipEntry theEntry;
                while ((theEntry = m_Stream.GetNextEntry ()) != null) {
              //      Logger.StartTime ();
                    UnzipOneFile(theEntry, m_Stream, m_ExportDir, true);
                //    Logger.StopTime (theEntry.Name);

          //          System.Threading.Thread.Sleep (1);
                }
                    
                Status = UnZipStatus.uzOk;
                // 退出线程
                Abort ();
            }
        }

        // 子线程调用
        protected override void Execute()
        {
            try
            {
                InitUnZip();
                ProcessUnZip();
            } catch
            {
                // 失败
                Status = UnZipStatus.uzFail;
                Abort ();
            }
        }


        protected override void End()
        {
            ClearStream ();

            if (m_UnZipDelete) {
                if (File.Exists (m_FileName)) {
                    File.Delete (m_FileName);
                }
            }

            if (OnZipedEnd != null) {
                OnZipedEnd (Status == UnZipStatus.uzOk);
            }

        }

        private void ClearStream()
        {
            if (m_Stream != null) {
                m_Stream.Close ();
                m_Stream.Dispose ();
                m_Stream = null;
            }
        }


        private string m_FileName = string.Empty;
        private string m_ExportDir = string.Empty;
        private bool m_UnZipDelete = false;
        private ZipInputStream m_Stream = null;
        private float m_Process = 0;
        private float m_Cnt = 0;
        private float m_Cur = 0;
        private UnZipStatus m_UnZipStatus = UnZipStatus.uzNone;


	}
}