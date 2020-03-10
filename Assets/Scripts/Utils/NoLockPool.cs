using System;
using System.Collections.Generic;

namespace Utils
{
	public interface INoLockPoolNode<T> {
		LinkedListNode<INoLockPoolNode<T>> PPoolNode {
			get;
		}

		void Dispose();
	}

	public class NoLockPoolNode<T>: INoLockPoolNode<T> where T : INoLockPoolNode<T>, new() {
		private LinkedListNode<INoLockPoolNode<T>> m_PoolNode = null;

		protected virtual void OnFree()
		{
		}

		public void Dispose() {
			if (IsDisposed)
				return;
			OnFree ();
			AbstractNoLockPool<T>._DestroyNode (this);
		}

		private bool IsDisposed
		{
			get {
				if (m_PoolNode == null)
					return false;
				return AbstractNoLockPool<T>.IsInNodePool (m_PoolNode);
			}
		}

		public LinkedListNode<INoLockPoolNode<T>> PPoolNode {
			get {
				if (m_PoolNode == null)
					m_PoolNode = new LinkedListNode<INoLockPoolNode<T>>(this);
				return m_PoolNode;
			}
		}
	}

	public sealed class AbstractNoLockPool<T> where T: INoLockPoolNode<T>, new()
	{
		private static LinkedList<INoLockPoolNode<T>> m_NodePool = new LinkedList<INoLockPoolNode<T>>();

		internal static bool IsInNodePool(LinkedListNode<INoLockPoolNode<T>> node) {
			return (node != null) && (m_NodePool == node.List);
		}

		internal static void _DestroyNode(INoLockPoolNode<T> node)
		{
			if (node != null) {
				var n = node.PPoolNode;
				if (n.List != m_NodePool) {
						var list = n.List;
						if (list != m_NodePool) {
							if (list != null)
								list.Remove (n);
							m_NodePool.AddLast (n);
						}
				}
			}
		}

		public static INoLockPoolNode<T> GetNode()
		{
			INoLockPoolNode<T> ret = null;
				LinkedListNode<INoLockPoolNode<T>> n = m_NodePool.First;
				if (n != null) {
					m_NodePool.Remove (n);
					ret = n.Value;
				}
			if (ret != null)
				return ret;

			ret = new T();
			return ret;
		}
	}
}