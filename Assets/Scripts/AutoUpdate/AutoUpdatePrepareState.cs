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
								} else if (string.Compare(key, "fileList", StringComparison.OrdinalIgnoreCase) == 0)
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

			string writePath = AutoUpdateMgr.Instance.WritePath;
			string fileName = string.Format("{0}/{1}", writePath, AutoUpdateMgr._cUpdateTxt);
			if (File.Exists(fileName))
				cfg.LoadFromFile(fileName);
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
			if (!File.Exists(m_FileListName))
				CopyFileList();
			else
			{
				if (!File.Exists(m_VersionName))
					CopyVersion();
				else
					ToNextState();
			}

		}

		private void OnVersionLoaded(ITask task)
		{
			if (task.IsOk)
			{
				WWWFileLoadTask t = task as WWWFileLoadTask;
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

			ToNextState();
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


			if (!File.Exists(m_VersionName))
				CopyVersion();
			else
				ToNextState();
		}
	}


}

