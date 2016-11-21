using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AutoUpdate
{
	// 压缩包类
	public static class ZipTools
	{

		public static void BuildVersionZip (string outDir, string oldVersion, string newVersion, 
		                                   ResListFile oldFileList, ResListFile newFileList)
		{
			if (string.IsNullOrEmpty (outDir) ||
			    string.IsNullOrEmpty (newVersion) || newFileList == null ||
			    string.IsNullOrEmpty (oldVersion) || oldFileList == null)
				return;
			
			List<string> diffFileList = CompareDiffList (oldFileList, newFileList);
			if (diffFileList == null || diffFileList.Count <= 0)
				return;
			
			string zipFileName = string.Format ("{0}/{1}-{2}.zip", outDir, oldVersion, newVersion);
			Compress (zipFileName, diffFileList.ToArray ());
		}

		private static List<string> CompareDiffList (ResListFile oldFileList, ResListFile newFileList)
		{
			if (newFileList == null)
				return null;

			List<string> ret = null;
			if (oldFileList == null) {
				var infos = newFileList.AllToDiffInfos ();
				if (infos == null || infos.Length <= 0)
					return ret;
				for (int i = 0; i < infos.Length; ++i) {
					var info = infos [i];
					if (string.IsNullOrEmpty (info.fileName) || string.IsNullOrEmpty (info.fileContentMd5))
						continue;
					
					if (ret == null)
						ret = new List<string> ();
					// 更新的文件都是以MD5为文件名的
					ret.Add (info.fileContentMd5);
				}
			} else {
				var iter = newFileList.GetIter ();
				while (iter.MoveNext ()) {
					string oldMd5 = oldFileList.GetFileContentMd5 (iter.Current.Key);
					if (string.Compare (oldMd5, iter.Current.Value.fileContentMd5) != 0) {
						if (string.IsNullOrEmpty (iter.Current.Value.fileContentMd5))
							continue;
						
						if (ret == null)
							ret = new List<string> ();
						ret.Add (iter.Current.Value.fileContentMd5);
					}
				}
				iter.Dispose ();
			}

			return ret;
		}

		#if UNITY_EDITOR

		private static string m_TempZipFileName = string.Empty;
		private static string[] m_ZipFiles = null;
		private static int m_ZipIdx = -1;
		private static FileStream m_outStream = null;
	//	private static GZipStream m_GStream = null;

		static void Reset()
		{
			EditorUtility.ClearProgressBar();

			/*
			if (m_GStream != null)
			{
				m_GStream.Close();
				m_GStream.Dispose();
				m_GStream = null;
			}*/

			if (m_outStream != null)
			{
				m_outStream.Close();
				m_outStream.Dispose();
				m_outStream = null;
			}

			m_TempZipFileName = string.Empty;
			m_ZipFiles = null;
			m_ZipIdx = -1;
		}
		/*
		private static void OnCompressing(System.Object obj, ProgressEventArgs args)
		{
			float process = ((float)args.PercentDone)/100f;
			EditorUtility.DisplayProgressBar("压缩中...", m_TempZipFileName, process);
		}

		private static void OnCompressEnd(System.Object obj, EventArgs args)
		{
			EditorUtility.ClearProgressBar();	
		}*/

		[MenuItem ("Assets/压缩文件夹")]
		public static void CompressZipDirectory ()
		{
			var select = Selection.activeObject;
			if (select == null)
				return;
			string path = AssetDatabase.GetAssetPath (select);
			if (string.IsNullOrEmpty (path))
				return;
			path = Path.GetFullPath (path);
			string[] ss = Directory.GetFiles (path);
			Compress (@"D:\tt.zip", ss);
		}

		static void OnCompress(IAsyncResult result)
		{
			if (result.IsCompleted)
			{
				EditorUtility.DisplayProgressBar("压缩中...", m_ZipFiles[m_ZipIdx], (float)m_ZipIdx/(float)m_ZipFiles.Length);
				++m_ZipIdx;
				NextCompress();
			}
		}

		static void NextCompress()
		{
			if (m_ZipFiles == null || m_ZipFiles.Length <= 0 || /*m_GStream == null ||*/ m_outStream == null || m_ZipIdx < 0)
				return;

			if (m_ZipIdx >= m_ZipFiles.Length)
			{
				Reset();
				return;
			}

			string fileName = m_ZipFiles[m_ZipIdx];
			if (string.IsNullOrEmpty(fileName))
			{
				++m_ZipIdx;
				NextCompress();
				return;
			}

			FileStream inStream = new FileStream(fileName, FileMode.Open);

			byte[] buf = new byte[inStream.Length];
			inStream.Read(buf, 0, buf.Length);
			inStream.Close();
			inStream.Dispose();

			//m_GStream.BeginWrite(buf, 0, buf.Length, OnCompress, null); 
		}

		#endif

		public static void Compress (string zipFileName, string[] files)
		{
			#if UNITY_EDITOR

			Reset();
			if (string.IsNullOrEmpty (zipFileName) || files == null || files.Length <= 0)
				return;

			m_TempZipFileName = zipFileName;
			EditorUtility.DisplayProgressBar ("压缩中...", zipFileName, 0);

			m_ZipFiles = files;
			m_outStream = new FileStream(zipFileName, FileMode.Create);
			try
			{
				//m_GStream = new GZipStream(m_outStream, CompressionMode.Compress);
				m_ZipIdx = 0;

				NextCompress();
			} catch(Exception e)
			{
				Reset();
			}

			/*
			SevenZipCompressor comp = new SevenZip.SevenZipCompressor();
			comp.ArchiveFormat = OutArchiveFormat.Zip;
			comp.CompressionMethod = CompressionMethod.Lzma;
			comp.Compressing += OnCompressing;
			comp.CompressionFinished += OnCompressEnd;
			comp.CompressionMode = CompressionMode.Create;
			FileStream stream = new FileStream(zipFileName, FileMode.Create);
			try
			{
				
				//comp.com
			} finally
			{
				stream.Close();
				stream.Dispose();
			}*/


			#endif
		}
	}

}
