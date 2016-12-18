using System;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateResZipReqState: AutoUpdateBaseState
	{
		// Md5 fileName
		private string m_ZipFileName = string.Empty;

		private void ToUnZipRes()
		{
			// 进入解压步骤
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auUnZipRes);
		}

		private void ToNextStatus()
		{
			if (!AutoUpdateMgr.Instance.IsFileListNoUpdate())
			{
				// 获得FileList列表
				AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResListReq);
			} else
				AutoUpdateMgr.Instance.EndAutoUpdate();
		}

		private void OnHttpRead(HttpClientResponse rep, long totalRead)
		{
			AutoUpdateCfgItem item;
			var updateFile = AutoUpdateMgr.Instance.LocalUpdateFile;
			if (updateFile.FindItem(m_ZipFileName, out item))
			{
				item.readBytes = totalRead;
				if (totalRead >= rep.MaxReadBytes)
				{
					item.isDone = true;
				}
				AutoUpdateMgr.Instance.DownloadUpdateToUpdateTxt(item);

				float currProcess = 0;
				if (rep.MaxReadBytes > 0)
					currProcess = (float)totalRead/(float)rep.MaxReadBytes;

				AutoUpdateMgr.Instance.DownProcess = currProcess;
				if (totalRead >= rep.MaxReadBytes)
					ToUnZipRes();
			} else
				ToNextStatus();
		}

		private void OnHttpError(HttpClientResponse rep, int status)
		{
			if (status == 404 || status < 0)
			{
				ToNextStatus();
			} else
				AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_ResZipReq, status);
		}

		public override  void Enter(AutoUpdateMgr target)
		{
			var updateFile = target.LocalUpdateFile;
			m_ZipFileName = target.CurrUpdateZipFileMd5;

			long read = 0;
			AutoUpdateCfgItem item;
			bool isSaveUpdateFile = false;
			if (updateFile.FindItem(m_ZipFileName, out item))
			{
				if (item.isDone)
				{
					ToUnZipRes();
					return;
				}

				read = item.readBytes;
			} else
			{
				item = new AutoUpdateCfgItem();
				item.fileContentMd5 = m_ZipFileName;
				item.isDone = false;
				item.readBytes = 0;
				updateFile.AddOrSet(item);
				isSaveUpdateFile = true;
			}

			isSaveUpdateFile = isSaveUpdateFile || updateFile.RemoveDowningZipFiles(m_ZipFileName);
			if (isSaveUpdateFile)
				updateFile.SaveToLastFile();

			string resAddr = target.ResServerAddr;
			// m_ZipFileName是内容MD5所以不用加时间戳
			string url = string.Format("{0}/{1}", resAddr, m_ZipFileName);
			target.CreateHttpFile(url, read, OnHttpRead, OnHttpError); 
		}
	}
}

