using System;

namespace AutoUpdate
{
	public class AutoUpdateFinishState: AutoUpdateBaseState
	{

		void ToNextState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auEnd);
		}

		public override void Enter(AutoUpdateMgr target)
		{
			target.ServerFileListToClientFileList();
			target.ChangeUpdateFileNames();
			target.ServerResVerToClientResVer();
			ToNextState();
		}
	}
}

