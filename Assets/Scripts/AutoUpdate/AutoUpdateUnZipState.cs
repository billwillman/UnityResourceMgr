using System;
using System.IO;

namespace AutoUpdate
{
	// 解压流程
	public class AutoUpdateUnZipState: AutoUpdateBaseState
	{
		void ToNextState()
		{
			if (!AutoUpdateMgr.Instance.IsFileListNoUpdate())
				AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auGetResListReq);
			else
				AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auFinished);
		}

		public override void Enter(AutoUpdateMgr target)
		{
			// 解压
			string zipFileMd5 = target.CurrUpdateZipFileMd5;
			string writePath = target.WritePath;
			if (string.IsNullOrEmpty(zipFileMd5) || string.IsNullOrEmpty(writePath))
				AutoUpdateMgr.Instance.EndAutoUpdate();
			string zipFileName = string.Format("{0}/{1}", writePath, zipFileMd5);
			if (!File.Exists(zipFileName))
			{
				target.Error(AutoUpdateErrorType.auError_ResZipVerReq, 0);
				return;
			}

			// 未写完， 解压完要改名，删除冗余的
			ZipTools.UnCompress(zipFileMd5);
			if (m_UnZipTimer == null)
			{
				m_UnZipTimer = TimerMgr.Instance.CreateTimer(0, true, true);
				m_UnZipTimer.AddListener(OnUnZipTimer);
			} else
				m_UnZipTimer.Start();
		}

		public override void Exit(AutoUpdateMgr target)
		{
			if (m_UnZipTimer != null)
			{
				m_UnZipTimer.Dispose();
				m_UnZipTimer = null;
			}
		}

		void OnUnZipTimer(Timer obj, float timer)
		{
			float process = ZipTools.UnCompressProcess;
			AutoUpdateMgr.Instance.DownProcess = process;
			var status = ZipTools.UnStatus;
			if (status == ZipTools.UnCompressStatus.unCompFinished)
			{
				ToNextState();
			} else if (status == ZipTools.UnCompressStatus.unCompError)
			{
				if (m_UnZipTimer != null)
				{
					m_UnZipTimer.Dispose();
					m_UnZipTimer = null;
				}
				AutoUpdateMgr.Instance.OnError(AutoUpdateErrorType.auError_UnZipError, 0);
			}
		}

		private ITimer m_UnZipTimer = null;
	}
}

