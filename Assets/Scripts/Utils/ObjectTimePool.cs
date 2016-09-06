/*
 * 带时间戳的缓存池
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
	public abstract class ITimePoolObj<K>
	{
		internal enum TimeObjState
		{
			to_None,
			to_Used,
			to_NotUsed
		}

		internal abstract K GetHashKey();

		public ITimePoolObj()
		{
			m_Node = new LinkedListNode<ITimePoolObj<K>>(this);
		}

		public bool IsUsed
		{
			get
			{
				return State == TimeObjState.to_Used;
			}
		}

		internal TimeObjState State
		{
			get;
			set;
		}

		public float UsedTime
		{
			get;
			internal set;
		}

		protected virtual void OnDestroy()
		{}

		internal LinkedListNode<ITimePoolObj<K>> LinkNode
		{
			get
			{
				return m_Node;
			}
		}

		internal void Destroy(bool isDestroy = false)
		{
			OnDestroy();

			State = ITimePoolObj<K>.TimeObjState.to_None;
			UsedTime = 0;

			if (isDestroy)
			{
				if (m_Node != null)
				{
					m_Node.Value = null;
					m_Node = null;
				}
			}
		}

		private LinkedListNode<ITimePoolObj<K>> m_Node = null;
	}

	public class ObjectTimePool<T, K>: DisposeObject where T: ITimePoolObj<K>, new()
	{
		public T Create(K key)
		{
			T ret = GetFromNotUsed(key);
			if (ret != null)
				return ret;
			
			InitPool();
			ret = m_Pool.GetObject();
			ret.State = ITimePoolObj<K>.TimeObjState.to_Used;
			ret.UsedTime = GetCurrentTime();
			return ret;
		}

		private T GetFromNotUsed(K key)
		{
			T ret;
			if (m_NotUsedHashMap.TryGetValue(key, out ret))
			{
				m_NotUsedList.Remove(ret.LinkNode);
				m_NotUsedHashMap.Remove(key);
				ret.State = ITimePoolObj<K>.TimeObjState.to_Used;
				ret.UsedTime = GetCurrentTime();
				return ret;
			}

			return null;
		}

		public void Destroy(T obj)
		{
			if (obj == null)
				return;
			AddToNotUsedList(obj);
		}

		public ObjectTimePool(int poolMaxCnt = 50, int maxNotUsedCnt = 10, int loopCnt = 5)
		{
			m_PoolMaxCnt = poolMaxCnt;
			m_LoopCnt = loopCnt;
			// 默认30秒未使用删除
			MaxNotUsedTime = 30.0f;
			m_NotUsedMaxCnt = maxNotUsedCnt;
			m_Timer = TimerMgr.Instance.CreateTimer(false, 0, true, true);
			m_Timer.AddListener(OnTick);
		}

		public float MaxNotUsedTime
		{
			get;
			set;
		}

		void UpdateNotUsedList()
		{
			if (m_NotUsedList == null || m_NotUsedList.Count <= 0)
				return;
			float t = GetCurrentTime();
			var node = m_NotUsedList.First;
			int tick = 0;
			while (node != null)
			{
				if (m_LoopCnt >= 0 && tick >= m_LoopCnt)
					break;

				float detlaTime = t - node.Value.UsedTime;
				if (detlaTime < MaxNotUsedTime)
					break;
				var nextNode = node.Next;
				InPool(node.Value as T);

				++tick;
				node = nextNode;

			}
		}

		void OnTick(Timer obj, float timer)
		{
			UpdateNotUsedList();
		}

		protected override void OnFree(bool isManual)
		{
			if (m_Timer != null)
			{
				m_Timer.Dispose();
				m_Timer = null;
			}
		}

		internal void AddToNotUsedList(T obj)
		{
			if (obj == null)
				return;
			if (obj.State == ITimePoolObj<K>.TimeObjState.to_NotUsed)
				return;

			if (m_NotUsedMaxCnt >= 0 && m_NotUsedList.Count == m_NotUsedMaxCnt)
			{
				var firstNode = m_NotUsedList.First;
				if (firstNode != null)
					InPool(firstNode.Value as T);
			} 


			if (m_NotUsedMaxCnt == 0)
			{
				InPool(obj);
			} else
			{
				obj.UsedTime = GetCurrentTime();
				obj.State = ITimePoolObj<K>.TimeObjState.to_NotUsed;
				m_NotUsedList.AddLast(obj.LinkNode);
				m_NotUsedHashMap.Add(obj.GetHashKey(), obj);
			}
		}

		private float GetCurrentTime()
		{
			return Time.realtimeSinceStartup;
		}

		private void InitPool()
		{
			if (m_InitPool)
				return;
			m_InitPool = true;
			m_Pool.Init(0);
		}

		private void InPool(T obj)
		{
			if (obj == null)
				return;
			if (obj.State != ITimePoolObj<K>.TimeObjState.to_None)
			{
				if (obj.State == ITimePoolObj<K>.TimeObjState.to_NotUsed)
				{
					m_NotUsedHashMap.Remove(obj.GetHashKey());
					m_NotUsedList.Remove(obj.LinkNode);
				}

				bool isDestroy = (m_PoolMaxCnt >= 0) && m_Pool.Count == m_PoolMaxCnt;
				obj.Destroy(isDestroy);
				if (!isDestroy)
				{
					InitPool();
					m_Pool.Store(obj);
				}
			}
		}

		public int PoolMaxCnt
		{
			get
			{
				return m_PoolMaxCnt;
			}
		}

		public void Clear(bool isClearPool)
		{
			var node = m_NotUsedList.First;
			while (node != null)
			{
				var obj = node.Value;
				node = node.Next;
				if (isClearPool)
					obj.Destroy(true);
				else
					InPool(obj as T);
			}
			m_NotUsedList.Clear();
			m_NotUsedHashMap.Clear();
			if (isClearPool)
				m_Pool.Clear();
		}

		public int GetPoolCount()
		{
			return m_Pool.Count;
		}

		public int GetNotUsedListCount()
		{
			return m_NotUsedList.Count;
		}

		private Dictionary<K, T> m_NotUsedHashMap = new Dictionary<K, T>();
		private LinkedList<ITimePoolObj<K>> m_NotUsedList = new LinkedList<ITimePoolObj<K>>();
		private ObjectPool<T> m_Pool = new ObjectPool<T>();
		private int m_PoolMaxCnt;
		private int m_LoopCnt;
		private int m_NotUsedMaxCnt;
		private bool m_InitPool = false;
		private Timer m_Timer = null;
	}
}
