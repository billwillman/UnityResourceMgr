/*
 * 对象池
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    public class ObjectPool<T> where T : class, new()
    {
        public delegate T CreateFunc();

        //
        // Methods
        //
        public T GetObject()
        {
            if (m_objStack.Count > 0)
            {
                T t = m_objStack.Pop();
                if (m_resetAction != null)
                {
                    m_resetAction(t);
                }
                return t;
            }
            return (m_createFunc != null) ? m_createFunc() : new T();
        }

        public void Init(int poolSize, CreateFunc createFunc = null, Action<T> resetAction = null)
        {
            m_objStack = new Stack<T>();
            m_resetAction = resetAction;
            m_createFunc = createFunc;
            for (int i = 0; i < poolSize; i++)
            {
                T item = (m_createFunc != null) ? m_createFunc() : new T();
                m_objStack.Push(item);
            }
        }

        public void Store(T obj)
        {
			if (obj == null)
				return;
			if (m_resetAction != null)
				m_resetAction(obj);
            m_objStack.Push(obj);
        }

		public int Count
		{
			get
			{
				if (m_objStack == null)
					return 0;
				return m_objStack.Count;
			}
		}

        private Stack<T> m_objStack = null;
        private Action<T> m_resetAction = null;
        private CreateFunc m_createFunc = null;
    }
}