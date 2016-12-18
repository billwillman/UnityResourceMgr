using System;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateGetZipVerReqState: AutoUpdateBaseState
	{
		void ToNextStatus()
		{
			if (string.IsNullOrEmpty(AutoUpdateMgr.Instance.CurrUpdateZipFileMd5))
			{
				if (!AutoUpdateMgr.Instance.IsFileListNoUpdate())
					AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResListReq);
				else
				{
					AutoUpdateMgr.Instance.EndAutoUpdate();
				}
			} else
			{
				AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResZipReq);
			}
		}

		void OnError(HttpClientResponse response, int status)
		{
			if (status == 404 || status < 0)
			{
				ToNextStatus();
			} else
				AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_ResZipVerReq, status);
		}

		void OnReadEvent(HttpClientResponse response, long totalReadBytes)
		{
			if (totalReadBytes >= response.MaxReadBytes)
			{
				HttpClientStrResponse r = response as HttpClientStrResponse;
				string zipVerList = r.Txt;
				if (string.IsNullOrEmpty(zipVerList))
				{
					AutoUpdateMgr.Instance.CurrUpdateZipFileMd5 = string.Empty;
					ToNextStatus();
					return;
				}

				string oldVer = AutoUpdateMgr.Instance.LocalResVersion;
				string newVer = AutoUpdateMgr.Instance.CurrServeResrVersion;
				string zipFileName = ZipTools.GetZipFileName(oldVer, newVer);

				ResListFile zipFiles = new ResListFile();
				zipFiles.Load(zipVerList);

				ResListFile.ResDiffInfo[] diff;
				if (!zipFiles.FileToDiffInfo(zipFileName, out diff) || diff == null ||
					string.IsNullOrEmpty(diff[0].fileContentMd5) || 
					diff[0].fileSize <= 0)
				{
					AutoUpdateMgr.Instance.CurrUpdateZipFileMd5 = string.Empty;
					ToNextStatus();
					return;
				}
					
				AutoUpdateMgr.Instance.UpdateToUpdateTxt(diff);
				AutoUpdateMgr.Instance.UpdateTotalDownloadBytes(diff);
				AutoUpdateMgr.Instance.CurrUpdateZipFileMd5 = diff[0].fileContentMd5;
				ToNextStatus();
			}
		}

		public override  void Enter(AutoUpdateMgr target)
		{
			string verMd5 = target.ServerZipVerMd5;
			if (string.IsNullOrEmpty(verMd5))
			{
				ToNextStatus();
				return;
			}

			string resAddr = target.ResServerAddr;
			// 已经是内容MD5，所以不需要加时间戳
			string url = string.Format("{0}/{1}.txt", resAddr, verMd5);
			target.CurrUpdateZipFileMd5 = string.Empty;
			target.CreateHttpTxt(url, OnReadEvent, OnError);
		}
	}
}

