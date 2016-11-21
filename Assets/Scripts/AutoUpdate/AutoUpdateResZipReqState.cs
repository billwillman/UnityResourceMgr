using System;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateResZipReqState: AutoUpdateBaseState
	{
		private string m_ZipFileName = string.Empty;

		private void ToNextState()
		{
			
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
		}

		public override  void Enter(AutoUpdateMgr target)
		{
			string oldVer = target.LocalResVersion;
			string newVer = target.CurrServeResrVersion;

			var updateFile = target.LocalUpdateFile;
			m_ZipFileName = ZipTools.GetZipFileName(oldVer, newVer);

			long read = 0;
			AutoUpdateCfgItem item;
			if (updateFile.FindItem(m_ZipFileName, out item))
			{
				if (item.isDone)
				{
					ToNextState();
					return;
				}

				read = item.readBytes;
			}

			string url = string.Format("{0}/{1}.zip", target.ResServerAddr, m_ZipFileName);
			target.CreateHttpFile(url, read, OnHttpRead, OnHttpError); 
		}
	}
}

