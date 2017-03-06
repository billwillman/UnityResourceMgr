using System;
using System.IO;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateFileListState: AutoUpdateBaseState
	{

		void ToNextState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auUpdateFileProcess);
		}

		void DoFileListTxt(string fileList)
		{
			if (string.IsNullOrEmpty(fileList))
			{
				AutoUpdateMgr.Instance.EndAutoUpdate();
				return;
			}

			string writePath = AutoUpdateMgr.Instance.WritePath;
			string srcFileName = string.Format("{0}/{1}", writePath, AutoUpdateMgr._cFileListTxt);
			if (!File.Exists(srcFileName))
			{
				// 直接生成写到update.txt里
				ResListFile resListFile = AutoUpdateMgr.Instance.ServerResListFile;
				resListFile.Load(fileList);
				ResListFile.ResDiffInfo[] infos = resListFile.AllToDiffInfos();

				AutoUpdateMgr.Instance.UpdateToUpdateTxt(infos);
				AutoUpdateMgr.Instance.UpdateTotalDownloadBytes(infos);

				ToNextState();
				return;
			}

			ResListFile srcListFile = AutoUpdateMgr.Instance.LocalResListFile;
			ResListFile dstListFile = AutoUpdateMgr.Instance.ServerResListFile;

			dstListFile.Load(fileList);

			ResListFile.ResDiffInfo[] diffInfos = srcListFile.GetDiffInfos(dstListFile);

			AutoUpdateMgr.Instance.UpdateToUpdateTxt(diffInfos);
			AutoUpdateMgr.Instance.UpdateTotalDownloadBytes(diffInfos);
			ToNextState();
		}

		void OnReadEvent(HttpClientResponse response, long totalReadBytes)
		{
			if (totalReadBytes >= response.MaxReadBytes)
			{
				HttpClientStrResponse r = response as HttpClientStrResponse;
				string fileList = r.Txt;
				DoFileListTxt(fileList);
			}
		}
		
		void OnError(HttpClientResponse response, int status)
		{
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_NoGetFileList, status);
		}

		void OnEnd(HttpClient client, HttpListenerStatus status)
		{
			switch (status)
			{
			case HttpListenerStatus.hsDone:
				{
					HttpClientStrResponse r = client.Listener as HttpClientStrResponse;
					string fileList = r.Txt;
					DoFileListTxt(fileList);
					break;
				}
			case HttpListenerStatus.hsError:
				AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_NoGetFileList, -1);
				break;
			}
		}
		
		void DoGetServerFileList()
		{
			string resAddr = AutoUpdateMgr.Instance.ResServerAddr;
			string ver = AutoUpdateMgr.Instance.CurrServeResrVersion;
			// use fileList ContentMD5
			string serverFileListMd5 = AutoUpdateMgr.Instance.ServerFileListContentMd5;
			string url = string.Format("{0}/{1}/{2}.txt", resAddr, ver, serverFileListMd5);
			//AutoUpdateMgr.Instance.CreateHttpTxt(url, OnReadEvent, OnError);
			AutoUpdateMgr.Instance.CreateHttpTxt(url, OnEnd); 
		}
		
		public override  void Enter(AutoUpdateMgr target)
		{
			string ver = target.CurrServeResrVersion;
			if (string.IsNullOrEmpty(ver))
			{
				target.EndAutoUpdate();
				return;
			}
			
			string writePath = target.WritePath;
			if (string.IsNullOrEmpty(writePath))
			{
				target.EndAutoUpdate();
				return;
			}

			target.TotalDownM = 0;
			target.CurDownM = 0;
			DoGetServerFileList();
		}
	}
}

