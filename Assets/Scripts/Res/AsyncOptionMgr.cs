/*----------------------------------------------------------------
// 模块名：对AsyncOperation异步管理封装
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年6月1日
// 模块描述：
//----------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsyncOperationMgr: Singleton<AsyncOperationMgr>
{
	public interface IAsyncOperationItem
	{
		void Release();
		AsyncOperation GetOperation();
		void Process();
	}

	public class AsyncOperationItem<T>: IAsyncOperationItem where T: AsyncOperation
	{
		internal T opt = null;
		internal Action<T> onProcess = null;
		public void Release()
		{}

		public AsyncOperation GetOperation()
		{
			return opt;
		}

		public void Process()
		{
			if ((opt == null) || (onProcess == null))
				return;
			onProcess (opt);
		}

		public System.Object UserData
		{
			get; set;
		}
	}

	#region public function

	public AsyncOperationItem<T> FindItem<T>(T opt) where T: AsyncOperation
	{
		if (opt == null)
			return null;
		Timer time;
		if (mDic.TryGetValue (opt, out time))
		{
			return GetAsyncOptionTimerOprItem<T>(time);
		} else
			return null;
	}

	public AsyncOperationItem<T> AddAsyncOperation<T>(T opt, Action<T> onProcess) where T: AsyncOperation
	{
		Timer time;
		if (mDic.TryGetValue (opt, out time)) {
			if (time == null)
				return null;
			AsyncOperationItem<T> old = time.UserData as AsyncOperationItem<T>;
			if (old == null)
				return null;

			if (onProcess != null)
				old.onProcess += onProcess;
			return old;
		}

		time = TimerMgr.Instance.CreateTimer (false, 0, true);
		time.AddListener (OnTimerEvent);
		AsyncOperationItem<T> item = new AsyncOperationItem<T> ();
		//AsyncOperationItem item = AsyncOperationItem.NewItem ();
		item.opt = opt; 
		item.onProcess = onProcess;
		time.UserData = item;

		mDic.Add (opt, time);

		return item;
	}

	public void Clear()
	{
		Dictionary<AsyncOperation, Timer>.Enumerator iter = mDic.GetEnumerator ();
		while (iter.MoveNext()) {
			if (iter.Current.Value != null)
			{
				if (iter.Current.Value.UserData != null)
				{
					IAsyncOperationItem item = iter.Current.Value.UserData as IAsyncOperationItem;
					if (item != null)
						item.Release();
				}
				iter.Current.Value.Dispose();
			}
		}

		iter.Dispose ();
		mDic.Clear ();
	}
	                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
	public void RemoveAsyncOperation(IAsyncOperationItem item)
	{
		if (item == null)
			return;
		
		AsyncOperation opt = item.GetOperation ();
		RemoveAsyncOperation(opt);
		
		item.Release ();
	}

	public void RemoveAsyncOperation(AsyncOperation opt) {
		if (opt == null)
			return;
		Timer time;
		if (mDic.TryGetValue(opt, out time) && (time != null)) {
			mDic.Remove(opt);
			time.Dispose();
		}
	}

	public AsyncOperation GetAsyncOptionTimerOpr(Timer obj)
	{
		if (obj == null)
			return null;
		IAsyncOperationItem item = (IAsyncOperationItem)obj.UserData;
		if (item == null)
			return null;
		AsyncOperation opt = item.GetOperation ();
		return opt;
	}

	public AsyncOperationItem<T> GetAsyncOptionTimerOprItem<T>(Timer obj) where T: AsyncOperation
	{
		if (obj == null)
			return null;
		IAsyncOperationItem item = (IAsyncOperationItem)obj.UserData;
		if (item == null)
			return null;
		return item as AsyncOperationItem<T>;
	}

	#endregion public function

	#region protected function

	protected  void OnTimerEvent(Timer obj, float timer)
	{
		IAsyncOperationItem item = (IAsyncOperationItem)obj.UserData;
		if (item == null) {
			obj.Dispose();
			return;
		}

		AsyncOperation opt = item.GetOperation ();
		if (opt == null) {
			obj.Dispose();
			return;
		}

		item.Process ();
		if (opt.isDone)
			RemoveAsyncOperation(item);

	}
	#endregion protected function

	protected Dictionary<AsyncOperation, Timer> mDic = new Dictionary<AsyncOperation, Timer>();
}
