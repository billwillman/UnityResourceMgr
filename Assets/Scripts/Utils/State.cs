using System;
using System.Collections.Generic;

namespace Utils
{
	public interface IState<U, V>
	{
		bool CanEnter(V target);
		bool CanExit(V target);
		void Enter(V target);
		void Exit(V target);
		void Process(V target);

		U Id
		{
			get;
			set;
		}
	}

	public class StateMgr<U, V> where V: class
	{
		public StateMgr(V target)
		{
			mTarget = target;
		}

		public virtual void Process(V target)
		{
			IState<U, V> state = CurrState;
			if (state != null)
				state.Process(target);
		}

		public virtual bool ChangeState(U id)
		{
			if (mTarget == null)
				return false;

			IState<U, V> now;
			if (mStateMap.TryGetValue(mCurrState, out now))
			{
				if (!now.CanExit(mTarget))
					return false;
			}

			IState<U, V> news;
			if (mStateMap.TryGetValue(id, out news) && news != null)
			{
				if (!news.CanEnter(mTarget))
					return false;
			} else
				return false;

			if (now != null)
				now.Exit(mTarget);
			news.Enter(mTarget);
			mCurrState = id;
			return true;
		}

		public U CurrStateKey
		{
			get
			{
				return mCurrState;
			}
		}

		public IState<U, V> CurrState
		{
			get
			{
				IState<U, V> ret;
				if (!mStateMap.TryGetValue(mCurrState, out ret))
					ret = null;
				return ret;
			}
		}

	public static void Register(U id, IState<U, V> state)
	{
		IState<U, V> now;
		if (mStateMap.TryGetValue(id, out now))
			mStateMap[id] = state;
		else
		{
			mStateMap.Add(id, state);
		}
	}

	public static bool FindState(U id, out IState<U, V> target)
	{
		bool ret = mStateMap.TryGetValue(id, out target);
		return ret;
	}

	protected V mTarget;
	protected static Dictionary<U, IState<U, V>> mStateMap = new Dictionary<U, IState<U, V>>();
	protected U mCurrState;
}

}

