using System;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateCheckVersionState: AutoUpdateBaseState
	{

		void ToNextState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResListReq);
		}

		void OnReadEvent(HttpClientResponse response, long totalReadBytes)
		{
			if (totalReadBytes >= response.MaxReadBytes)
			{
				// 判断版本
				HttpClientStrResponse r = response as HttpClientStrResponse;
				string versionStr = r.Txt;
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
						else
							ToNextState();
					} else
						AutoUpdateMgr.Instance.EndAutoUpdate();
				} else
					AutoUpdateMgr.Instance.EndAutoUpdate();
			}
		}
		
		void OnError(HttpClientResponse response, int status)
		{
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_NoGetVersion, status);
		}
		
		public override  void Enter(AutoUpdateMgr target)
		{
			string url = string.Format("{0}/{1}", target.ResServerAddr, AutoUpdateMgr._cVersionTxt);
			target.CreateHttpTxt(url, OnReadEvent, OnError);
		}

	}
}

