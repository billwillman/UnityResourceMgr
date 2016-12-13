// 是否允许版本回退
#define _CanBackVer

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Utils;

namespace AutoUpdate
{
	public class AutoUpdatePrepareState: AutoUpdateBaseState
	{
		private string m_FileListName = string.Empty;
		private string m_VersionName = string.Empty;
		private string m_UpdateName = string.Empty;

		private void CopyFileList()
		{
			WWWFileLoadTask task = AutoUpdateMgr.Instance.CreateWWWStreamAssets(AutoUpdateMgr._cFileListTxt, true);
			task.AddResultEvent(OnFileListloaded);
		}

		private void CopyVersion()
		{
			WWWFileLoadTask task = AutoUpdateMgr.Instance.CreateWWWStreamAssets(AutoUpdateMgr._cVersionTxt, true);
			task.AddResultEvent(OnVersionLoaded);
		}

		void LoadLocalResVersion()
		{
			AutoUpdateMgr.Instance.LocalResVersion = string.Empty;
			AutoUpdateMgr.Instance.LocalFileListContentMd5 = string.Empty;

			if (File.Exists(m_VersionName))
			{
				FileStream stream = new FileStream(m_VersionName, FileMode.Open, FileAccess.Read);
				try
				{
					byte[] src = new byte[stream.Length];
					stream.Read(src, 0, src.Length);
					string s = System.Text.Encoding.ASCII.GetString(src);
					char[] splits = new char[1];
					splits[0] = '\n';
					string[] lines = s.Split(splits, StringSplitOptions.RemoveEmptyEntries);
					if (lines != null)
					{
						for (int i = 0; i < lines.Length; ++i)
						{
							int idx = lines[i].IndexOf('=');
							if (idx >= 0)
							{
								string key = lines[i].Substring(0, idx).Trim();
								if (string.Compare(key, "res", StringComparison.CurrentCultureIgnoreCase) == 0)
								{
									AutoUpdateMgr.Instance.LocalResVersion = lines[i].Substring(idx + 1).Trim();
								} else if (string.Compare(key, "fileList", StringComparison.CurrentCultureIgnoreCase) == 0)
								{
									AutoUpdateMgr.Instance.LocalFileListContentMd5 = lines[i].Substring(idx + 1).Trim();
								}
							}
						}
					}
				} finally
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}
		}

		void LoadLocalFileList()
		{
			var file = AutoUpdateMgr.Instance.LocalResListFile;
			file.Clear();
			if (File.Exists(m_FileListName))
			{
				file.LoadFromFile(m_FileListName);
			}
		}

		void LoadUpdateTxt()
		{
			var cfg = AutoUpdateMgr.Instance.LocalUpdateFile;
			cfg.Clear();

			if (File.Exists(m_UpdateName))
				cfg.LoadFromFile(m_UpdateName);
		}

		void ToNextState()
		{
			LoadLocalResVersion();
			LoadLocalFileList();
			LoadUpdateTxt();
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auCheckVersionReq);
		}

		public override  void Enter(AutoUpdateMgr target)
		{
			string writePath = target.WritePath;
			if (string.IsNullOrEmpty(writePath))
			{
				target.EndAutoUpdate();
				return;
			}
			
			m_FileListName = string.Format("{0}/{1}", writePath, AutoUpdateMgr._cFileListTxt);
			m_VersionName = string.Format("{0}/{1}", writePath, AutoUpdateMgr._cVersionTxt);
			m_UpdateName = string.Format("{0}/{1}", writePath, AutoUpdateMgr._cUpdateTxt);

			DoNextCopyVersion();
		}

		private void DoNextCopyVersion()
		{
			/*
			if (!File.Exists(m_VersionName))
				CopyVersion();
			else
				DoCopyFileList();
			*/

			// 不管如何都讀一次Version,因爲要判斷APK包的版本和可寫目錄的版本，是否一致
			CopyVersion();
		}

		private void DoCopyFileList()
		{
			if (!File.Exists(m_FileListName))
				CopyFileList();
			else
				ToNextState();
		}

		// 刪除所有可寫目錄文件
		private void RemovePersistFiles()
		{
			// 刪除Update.txt裏的文件
			if (File.Exists(m_UpdateName))
			{
				AutoUpdateCfgFile cfg = new AutoUpdateCfgFile();
				cfg.LoadFromFile(m_UpdateName);
				cfg.RemoveAllDowningFiles();
				File.Delete(m_UpdateName);
			}

			// 刪除FileList裏的文件
			if (File.Exists(m_FileListName))
			{
				ResListFile cfg = new ResListFile();
				cfg.LoadFromFile(m_FileListName);
				cfg.DeleteAllFiles();
				File.Delete(m_FileListName);
			}

			// 刪除Version和FileList文件
			if (File.Exists(m_VersionName))
				File.Delete(m_VersionName);
		}

		private bool DoChecWriteNewVersion(byte[] pkgBuf)
		{
			if (pkgBuf == null || pkgBuf.Length <= 0)
				return false;
			
			if (string.IsNullOrEmpty(m_VersionName))
				return true;
			if (!File.Exists(m_VersionName))
				return true;

			#if !_CanBackVer
			string str = string.Empty;
			FileStream stream = new FileStream(m_VersionName, FileMode.Open, FileAccess.Read);
			try
			{
				if (stream.Length <= 0)
					return true;
				byte[] buf = new byte[stream.Length];
				stream.Read(buf, 0, buf.Length);
				str = System.Text.Encoding.ASCII.GetString(buf);
				if (string.IsNullOrEmpty(str))
					return true;
			} finally
			{
				stream.Close();
				stream.Dispose();
			}

			string persistVer;
			string persistMd5;
			string zipMd5;
			if (AutoUpdateMgr.Instance.GetResVer(str, out persistVer, out persistMd5, out zipMd5))
			{
				string ss = System.Text.Encoding.ASCII.GetString(pkgBuf);
				if (string.IsNullOrEmpty(ss))
					return false;
				string pkgVer;
				string pkgMd5;
				string pkgZip;
				if (!AutoUpdateMgr.Instance.GetResVer(ss, out pkgVer, out pkgMd5, out pkgZip))
					return false;

				int comp = string.Compare(pkgVer, persistVer, StringComparison.CurrentCultureIgnoreCase);

				bool ret = comp > 0;

				if (ret)
					RemovePersistFiles();

				return ret;
			}

			return true;
			#else
			return false;
			#endif
		}

		private void OnVersionLoaded(ITask task)
		{
			if (task.IsOk)
			{
				WWWFileLoadTask t = task as WWWFileLoadTask;

				if (DoChecWriteNewVersion(t.ByteData))
				{
					FileStream stream = new FileStream(m_VersionName, FileMode.Create, FileAccess.Write);
					try
					{
						stream.Write(t.ByteData, 0, t.ByteData.Length);
					} finally
					{
						stream.Close();
						stream.Dispose();
						stream = null;
					}
				}
			}

			DoCopyFileList();
		}

		private void OnFileListloaded(ITask task)
		{
			if (task.IsOk)
			{
				WWWFileLoadTask t = task as WWWFileLoadTask;
				FileStream stream = new FileStream(m_FileListName, FileMode.Create, FileAccess.Write);
				try
				{
					stream.Write(t.ByteData, 0, t.ByteData.Length);
				} finally
				{
					stream.Close();
					stream.Dispose();
					stream = null;
				}
			}


			ToNextState();
		}
	}


}

