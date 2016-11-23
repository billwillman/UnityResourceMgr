using System;
using System.IO;
using Utils;

namespace AutoUpdate
{

	public class AutoUpdateBaseState: IState<AutoUpdateState, AutoUpdateMgr>
	{
		public virtual bool CanEnter(AutoUpdateMgr target)
		{
			return true;
		}
		
		public virtual  bool CanExit(AutoUpdateMgr target)
		{
			return true;
		}
		
		public virtual  void Enter(AutoUpdateMgr target)
		{}
		
		public virtual  void Exit(AutoUpdateMgr target)
		{}
		
		public virtual  void Process(AutoUpdateMgr target)
		{}

		public AutoUpdateState Id
		{
			get;
			set;
		}
	}

	// nothing
	public class AutoUpdateStateEnd: AutoUpdateBaseState
	{
		public override void Enter(AutoUpdateMgr target)
		{
			target.DownProcess = 1.0f;
			target.CurDownM = target.TotalDownM;
		}
	}

	public class AutoUpdateStateMgr: StateMgr<AutoUpdateState, AutoUpdateMgr>
	{
		public AutoUpdateStateMgr(AutoUpdateMgr mgr): base(mgr)
		{}
	}
}
