using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;
using Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AutoUpdate
{

    // 压缩包类
    public static class ZipTools {

        private static ZipInputStream m_ZipInputStream = null;
        private static FileStream m_inputStream = null;
        private static readonly int m_BufSize = 2048;

        private static System.Threading.Thread m_UnCompressThread = null;
        private static System.Object m_ThreadLock = new System.Object();
        private static byte[] m_Buf = new byte[m_BufSize];
        private static FileStream m_localFileStream = null;
        private static long m_FileRead = 0;
        private static long m_AllFileRead = 0;
        private static ZipEntry m_CurEntry = null;
        private static string m_WritePath = string.Empty;

        private static void ResetUnCompressThread() {
            if (m_UnCompressThread != null) {
                m_UnCompressThread.Abort();
                m_UnCompressThread.Join();
                m_UnCompressThread = null;
            }

            m_FileRead = 0;
            m_AllFileRead = 0;
            m_CurEntry = null;
        }

        private static void ResetLocalFile() {
            if (m_localFileStream != null) {
                m_localFileStream.Close();
                m_localFileStream.Dispose();
                m_localFileStream = null;
            }
        }

        private static void ResetUnCompress() {

            ResetUnCompressThread();

            if (m_ZipInputStream != null) {
                m_ZipInputStream.Close();
                m_ZipInputStream.Dispose();
                m_ZipInputStream = null;
            }

            if (m_inputStream != null) {
                m_inputStream.Close();
                m_inputStream.Dispose();
                m_inputStream = null;
            }

            ResetLocalFile();
        }

        private static void ThreadUnCompressProc() {
            while (m_UnCompressThread.ThreadState == System.Threading.ThreadState.Running) {
                if (m_CurEntry == null) {

                    m_CurEntry = m_ZipInputStream.GetNextEntry();

                    if (m_CurEntry != null) {
                        // 新文件

                        ResetLocalFile();
                        string fileName = string.Format("{0}/{1}", m_WritePath, m_CurEntry.Name);
                        m_localFileStream = new FileStream(fileName, FileMode.Create);
                    }
                }

                if (m_CurEntry == null) {
                    ResetUnCompress();
                    return;
                }

                int readSize = m_ZipInputStream.Read(m_Buf, 0, m_Buf.Length);
                if (readSize > 0) {
                    m_localFileStream.Write(m_Buf, 0, readSize);
                    m_localFileStream.Flush();
                    m_FileRead += readSize;
                } else {
                    ResetLocalFile();
                    m_CurEntry = null;
                }

                Thread.Sleep(1);
            }
        }

        // 解压进度
        public static float UnCompressProcess {
            get {
                if (m_AllFileRead <= 0)
                    return 0;

                float process;
                lock (m_ThreadLock) {
                    process = m_FileRead;
                }
                return process / (float)m_AllFileRead;
            }
        }

        // 开始解压线程
        private static void StartUnCompressThread() {
            ResetUnCompressThread();

            if (m_ZipInputStream == null)
                return;

            m_AllFileRead = m_ZipInputStream.Length;

            m_WritePath = FilePathMgr.Instance.WritePath;
            m_UnCompressThread = new System.Threading.Thread(ThreadUnCompressProc);
            m_UnCompressThread.Start();
        }

        public static void UnCompress(string zipFileName) {

            if (string.IsNullOrEmpty(zipFileName))
                return;

            m_inputStream = new FileStream(zipFileName, FileMode.Open);
            try {
                m_ZipInputStream = new ZipInputStream(m_inputStream);
                if (!m_ZipInputStream.CanRead) {
                    ResetUnCompress();
                    return;
                }
                StartUnCompressThread();
            }
            catch (Exception ee) {
                ResetUnCompress();
            }
        }

		public static string GetZipFileName(string oldVersion, string newVersion)
		{
			string ret = string.Format("{0}-{1}", oldVersion, newVersion);
			return ret;
		}

        public static bool BuildVersionZip(string outDir, string oldVersion, string newVersion,
                                           ResListFile oldFileList, ResListFile newFileList) {
            if (string.IsNullOrEmpty(outDir) ||
                string.IsNullOrEmpty(newVersion) || newFileList == null ||
                string.IsNullOrEmpty(oldVersion) || oldFileList == null)
                return false;

            List<string> diffFileList = CompareDiffList(oldFileList, newFileList);
            if (diffFileList == null || diffFileList.Count <= 0)
                return false;

			// 增加fileList.txt
			diffFileList.Add("fileList.txt");

			string zipFileName = string.Format("{0}/{1}.zip", outDir, GetZipFileName(oldVersion, newVersion));

			for (int i = 0; i < diffFileList.Count; ++i)
			{
				string fileName = string.Format("{0}/{1}/{2}", outDir, newVersion, diffFileList[i]);
				diffFileList[i] = Path.GetFullPath(fileName);
			}

            Compress(zipFileName, diffFileList.ToArray());

			return true;
        }

        private static List<string> CompareDiffList(ResListFile oldFileList, ResListFile newFileList) {
            if (newFileList == null)
                return null;

            List<string> ret = null;
            if (oldFileList == null) {
                var infos = newFileList.AllToDiffInfos();
                if (infos == null || infos.Length <= 0)
                    return ret;
                for (int i = 0; i < infos.Length; ++i) {
                    var info = infos[i];
                    if (string.IsNullOrEmpty(info.fileName) || string.IsNullOrEmpty(info.fileContentMd5))
                        continue;

                    if (ret == null)
                        ret = new List<string>();
                    // 更新的文件都是以MD5为文件名的
                    ret.Add(info.fileContentMd5);
                }
            } else {
                var iter = newFileList.GetIter();
                while (iter.MoveNext()) {
                    string oldMd5 = oldFileList.GetFileContentMd5(iter.Current.Key);
                    if (string.Compare(oldMd5, iter.Current.Value.fileContentMd5) != 0) {
                        if (string.IsNullOrEmpty(iter.Current.Value.fileContentMd5))
                            continue;

                        if (ret == null)
                            ret = new List<string>();
                        ret.Add(iter.Current.Value.fileContentMd5);
                    }
                }
                iter.Dispose();
            }

            return ret;
        }

#if UNITY_EDITOR

        private static string m_TempZipFileName = string.Empty;
        private static string[] m_ZipFiles = null;
        private static int m_ZipCompressIdx = -1;
        private static FileStream m_outStream = null;
        private static ZipOutputStream m_ZipOutStream = null;

        static void ResetCompress() {
            EditorUtility.ClearProgressBar();

            if (m_ZipOutStream != null) {
                m_ZipOutStream.Close();
                m_ZipOutStream.Dispose();
                m_ZipOutStream = null;
            }

            if (m_outStream != null) {
                m_outStream.Close();
                m_outStream.Dispose();
                m_outStream = null;
            }

            m_TempZipFileName = string.Empty;
            m_ZipFiles = null;
            m_ZipCompressIdx = -1;
        }

        [MenuItem("Assets/测试压缩文件夹")]
        public static void CompressZipDirectory() {
            var select = Selection.activeObject;
            if (select == null)
                return;
            string path = AssetDatabase.GetAssetPath(select);
            if (string.IsNullOrEmpty(path))
                return;
            path = Path.GetFullPath(path);
            string[] ss = Directory.GetFiles(path);
            string zipFileName = Path.GetFullPath("Assets/zipTest.zip");
            Compress(zipFileName, ss);
        }

        [MenuItem("Assets/测试解压文件夹")]
        public static void UnCompressZipFiles() {
            var select = Selection.activeObject;
            if (select == null)
                return;
            string path = AssetDatabase.GetAssetPath(select);
            if (string.IsNullOrEmpty(path))
                return;
            path = Path.GetFullPath(path);
            UnCompress(path);
        }

		static void OnCompress(IAsyncResult result)
		{
			if (result.IsCompleted)
			{
				EditorUtility.DisplayProgressBar("压缩中...", m_ZipFiles[m_ZipCompressIdx], (float)m_ZipCompressIdx / (float)m_ZipFiles.Length);
				++m_ZipCompressIdx;
				NextCompress(result);
			}
		}

        static void DoCopressed() {
            EditorUtility.DisplayProgressBar("压缩中...", m_ZipFiles[m_ZipCompressIdx], (float)m_ZipCompressIdx / (float)m_ZipFiles.Length);
            ++m_ZipCompressIdx;
            NextCompress(null);
        }

        static void NextCompress(IAsyncResult result = null)
		{
			if (m_ZipFiles == null || m_ZipFiles.Length <= 0 || m_ZipOutStream == null || m_outStream == null || m_ZipCompressIdx < 0)
				return;

            if (result != null) {
                m_ZipOutStream.EndWrite(result);
                m_ZipOutStream.Flush();
            }

			if (m_ZipCompressIdx >= m_ZipFiles.Length)
			{
                ResetCompress();
				return;
			}

			string fileName = m_ZipFiles[m_ZipCompressIdx];
			if (string.IsNullOrEmpty(fileName))
			{
				++m_ZipCompressIdx;
				NextCompress();
				return;
			}

			FileStream inStream = new FileStream(fileName, FileMode.Open);

            string name = Path.GetFileName(fileName);
            ZipEntry zipEntry = new ZipEntry(name);
            m_ZipOutStream.PutNextEntry(zipEntry);

            while (true) {
                int read = inStream.Read(m_Buf, 0, m_Buf.Length);

                if (read > 0) {
                    m_ZipOutStream.Write(m_Buf, 0, read);
                    m_ZipOutStream.Flush();
                    m_outStream.Flush();
                } else
                {
                    inStream.Close();
                    inStream.Dispose();
                    break;
                }
            }

            DoCopressed();
        }

		#endif

		public static void Compress (string zipFileName, string[] files)
		{
#if UNITY_EDITOR

            ResetCompress();
			if (string.IsNullOrEmpty (zipFileName) || files == null || files.Length <= 0)
				return;

			m_TempZipFileName = zipFileName;
			EditorUtility.DisplayProgressBar ("压缩中...", zipFileName, 0);

			m_ZipFiles = files;
			m_outStream = new FileStream(zipFileName, FileMode.Create);
			try
			{
                m_ZipOutStream = new ZipOutputStream(m_outStream);
                m_ZipOutStream.SetLevel(9);
                m_ZipCompressIdx = 0;

				NextCompress();
			} catch(Exception e)
			{
                ResetCompress();
#if DEBUG
                UnityEngine.Debug.LogError(e.ToString());
#endif
            }


			#endif
		}
	}

}
