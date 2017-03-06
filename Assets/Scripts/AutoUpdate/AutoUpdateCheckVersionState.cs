//#define USE_ZIPVER

using System;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateCheckVersionState: AutoUpdateBaseState
	{

		void ToResListState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResListReq);
		}

		void ToGetZipVerReq()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetZipVerReq);
		}

		void DoVersionTxt(string versionStr)
		{
			if (!string.IsNullOrEmpty(versionStr))
			{
				AutoUpdateMgr.Instance.LoadServerResVer(versionStr);
				string resVer = AutoUpdateMgr.Instance.CurrServeResrVersion;
				if (!string.IsNullOrEmpty(resVer))
				{
					if (AutoUpdateMgr.Instance.IsVersionNoUpdate())
					{
						// CheckFileList

						AutoUpdateMgr.Instance.EndAutoUpdate();
					}
					#if USE_ZIPVER
					else if (!AutoUpdateMgr.Instance.IsVersionTxtNoUpdate())
					ToGetZipVerReq();
					#endif
					else
						ToResListState();	
				} else
					AutoUpdateMgr.Instance.EndAutoUpdate();
			} else
				AutoUpdateMgr.Instance.EndAutoUpdate();
		}

		void OnReadEvent(HttpClientResponse response, long totalReadBytes)
		{
			if (totalReadBytes >= response.MaxReadBytes)
			{
				// 判断版本
				HttpClientStrResponse r = response as HttpClientStrResponse;
				string versionStr = r.Txt;
				DoVersionTxt(versionStr);
			}
		}
		
		void OnError(HttpClientResponse response, int status)
		{
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_NoGetVersion, status);
		}

		void OnEnd(HttpClient client, HttpListenerStatus status)
		{
			switch (status)
			{
			case HttpListenerStatus.hsDone:
				{
					HttpClientStrResponse r = client.Listener as HttpClientStrResponse;
					string versionStr = r.Txt;
					DoVersionTxt(versionStr);
					break;
				}
			case HttpListenerStatus.hsError:
				AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_NoGetVersion, -1);
				break;
			}
		}
		
		public override  void Enter(AutoUpdateMgr target)
		{
			string resAddr = target.ResServerAddr;
			bool isHttps = resAddr.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase);
			string url;
			if (isHttps)
				url = string.Format("{0}/{1}", resAddr, AutoUpdateMgr._cVersionTxt);
			else
			{
				long t = DateTime.UtcNow.Ticks;
				url = string.Format("{0}/{1}?time={2}", resAddr, AutoUpdateMgr._cVersionTxt, t.ToString());
			}

			target.CreateHttpTxt(url, OnEnd);
		   // target.CreateHttpTxt(url, OnReadEvent, OnError);

		}

	}
}

