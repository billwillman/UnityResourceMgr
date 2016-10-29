using System;
using System.Collections;
using System.Collections.Generic;
using Utils;

namespace NsHttpClient
{
	internal interface IThreadMsg
	{
		LinkedListNode<IThreadMsg> ListNode
		{
			get;
		}

		void DoCallMsg();

		void Release();
	}

	internal class ThreadMsg<T>: IThreadMsg
	{
		public ThreadMsg()
		{
			m_ListNode = new LinkedListNode<IThreadMsg>(this);
		}

		public LinkedListNode<IThreadMsg> ListNode
		{
			get
			{
				return m_ListNode;
			}
		}

		public void DoCallMsg()
		{
			if (OnMsg != null)
				OnMsg(Data);
		}

		public T Data
		{
			get;
			set;
		}

		public Action<T> OnMsg
		{
			get;
			set;
		}

		public static ThreadMsg<T> Create(T data, Action<T> onMsg)
		{
			InitPool();
			ThreadMsg<T> ret = m_Pool.GetObject();
			ret.Data = data;
			ret.OnMsg = onMsg;
			return ret;
		}

		public void Release()
		{
			InPool(this);
		}

		private static void InitPool()
		{
			if (m_InitPool)
				return;
			m_InitPool = true;
			m_Pool.Init(0);
		}

		private static void InPool(ThreadMsg<T> obj)
		{
			if (obj == null)
				return;
			InitPool();
			obj.Data = default(T);
			obj.OnMsg = null;
			m_Pool.Store(obj);
		}

		private static ObjectPool<ThreadMsg<T>> m_Pool = new ObjectPool<ThreadMsg<T>>();
		private static bool m_InitPool = false;

		private LinkedListNode<IThreadMsg> m_ListNode = null;
	}

	public class ThreadMsgList: DisposeObject
	{
		public ThreadMsgList()
		{
			m_Timer = TimerMgr.Instance.CreateTimer(false, 0, true, true);
			m_Timer.AddListener(OnTimerEvent);
		}

		public void CreateThreadMsg<T>(T data, Action<T> onMsg)
		{
			lock (m_Lock)
			{
				ThreadMsg<T> msg = ThreadMsg<T>.Create(data, onMsg);
				m_MsgList.AddLast(msg.ListNode);
			}
		}

		protected override void OnFree(bool isManual)
		{
			if (m_Timer != null)
			{
				m_Timer.Dispose();
				m_Timer = null;
			}

			ClearMsgList();
		}

		private void ClearMsgList()
		{
			lock(m_Lock)
			{
				var node = m_MsgList.First;
				while (node != null)
				{
					if (node.Value != null)
						node.Value.Release();
					node = node.Next;
				}

				m_MsgList.Clear();
			}
		}

		private void OnTimerEvent(Timer obj, float timer)
		{
			LinkedListNode<IThreadMsg> node;
			lock(m_Lock)
			{
				node = m_MsgList.First;
				m_MsgList.RemoveFirst();
			}

			if (node.Value != null)
			{
				node.Value.DoCallMsg();
				lock (m_Lock)
				{
					node.Value.Release();
				}
			}
		}

		private LinkedList<IThreadMsg> m_MsgList = new LinkedList<IThreadMsg>();
		private System.Object m_Lock = new object();
		private Timer m_Timer = null;
	}
}
