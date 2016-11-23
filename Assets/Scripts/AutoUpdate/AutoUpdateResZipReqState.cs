using System;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateResZipReqState: AutoUpdateBaseState
	{
		private string m_ZipFileName = string.Empty;

		private void ToNextState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResListReq);
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
					ToNextState();
			} else
				ToNextState();
		}

		private void OnHttpError(HttpClientResponse rep, int status)
		{
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_ResZipReq, status);
			if (status == 404)
			{
				ToNextState();
			}
		}

		public override  void Enter(AutoUpdateMgr target)
		{
			string oldVer = target.LocalResVersion;
			string newVer = target.CurrServeResrVersion;

			var updateFile = target.LocalUpdateFile;
			m_ZipFileName = ZipTools.GetZipFileName(oldVer, newVer);

			long read = 0;
			AutoUpdateCfgItem item;
			m_ZipFileName = string.Format("{0}.zip", m_ZipFileName);
			bool isSaveUpdateFile = false;
			if (updateFile.FindItem(m_ZipFileName, out item))
			{
				if (item.isDone)
				{
					ToNextState();
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
			bool isHttps = resAddr.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase);
			string url;
			if (isHttps)
				url = string.Format("{0}/{1}", resAddr, m_ZipFileName);
			else
			{
				long tt = DateTime.UtcNow.Ticks;
				url = string.Format("{0}/{1}?time={2}", resAddr, m_ZipFileName, tt.ToString());
			}
			target.CreateHttpFile(url, read, OnHttpRead, OnHttpError); 
		}
	}
}

