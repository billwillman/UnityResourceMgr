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

		void OnReadEvent(HttpClientResponse response, long totalReadBytes)
		{
			if (totalReadBytes >= response.MaxReadBytes)
			{
				HttpClientStrResponse r = response as HttpClientStrResponse;
				string fileList = r.Txt;
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

					ToNextState();
					return;
				}
	
				ResListFile srcListFile = AutoUpdateMgr.Instance.LocalResListFile;
				ResListFile dstListFile = AutoUpdateMgr.Instance.ServerResListFile;

				dstListFile.Load(fileList);
					
				ResListFile.ResDiffInfo[] diffInfos = srcListFile.GetDiffInfos(dstListFile);

				AutoUpdateMgr.Instance.UpdateToUpdateTxt(diffInfos);
				ToNextState();
			}
		}
		
		void OnError(HttpClientResponse response, int status)
		{
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_NoGetFileList, status);
		}
		
		void DoGetServerFileList()
		{
			string resAddr = AutoUpdateMgr.Instance.ResServerAddr;
			bool isHttps = resAddr.IndexOf("https://", StringComparison.CurrentCultureIgnoreCase) >= 0;
			string ver = AutoUpdateMgr.Instance.CurrServeResrVersion;
			string url;
			if (isHttps)
				url = string.Format("{0}/{1}/{2}", resAddr, ver, AutoUpdateMgr._cFileListTxt);
			else
			{
				long t = DateTime.UtcNow.Ticks;
				url = string.Format("{0}/{1}/{2}?time={3}", resAddr, ver, AutoUpdateMgr._cFileListTxt, t.ToString());
			}
			AutoUpdateMgr.Instance.CreateHttpTxt(url, OnReadEvent, OnError);
		}
		
		public override  void Enter(AutoUpdateMgr target)
		{
			string ver = target.CurrServeResrVersion;
			if (string.IsNullOrEmpty(ver))
			{
				AutoUpdateMgr.Instance.EndAutoUpdate();
				return;
			}
			
			string writePath = target.WritePath;
			if (string.IsNullOrEmpty(writePath))
			{
				AutoUpdateMgr.Instance.EndAutoUpdate();
				return;
			}
			
			DoGetServerFileList();
		}
	}
}

