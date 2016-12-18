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
		}
	}
}

