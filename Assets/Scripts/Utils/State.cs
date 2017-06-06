using System;
using System.Collections.Generic;

namespace Utils
{
    public interface IState<U, V> {
        bool CanEnter(V target);
        bool CanExit(V target);
        void Enter(V target);
        void Exit(V target);
        void Process(V target);

        U Id {
            get;
            set;
        }
    }

    public class StateMgr<U, V> where V : class {
        public StateMgr(V target) {
            m_Target = target;
        }

        public virtual void Process(V target) {
            IState<U, V> state = CurrState;
            if (state != null)
                state.Process(target);
        }

        public virtual bool ChangeState(U id) {
            if (m_Target == null)
                return false;

            IState<U, V> now = m_CurrStatus;
            if (now != null) {
                if (!now.CanExit(m_Target))
                    return false;
            }

            IState<U, V> news;
            if (m_StateMap.TryGetValue(id, out news) && news != null) {
                if (!news.CanEnter(m_Target))
                    return false;
            } else
                return false;

            if (now != null)
                now.Exit(m_Target);
            news.Enter(m_Target);

            m_CurrKey = id;
            m_CurrStatus = news;
            return true;
        }

        public U CurrStateKey {
            get {
                return m_CurrKey;
            }
        }

        public IState<U, V> CurrState {
            get {
                return m_CurrStatus;
            }
        }

        public static void Register(U id, IState<U, V> state) {
            IState<U, V> now;
            if (m_StateMap.TryGetValue(id, out now))
                m_StateMap[id] = state;
            else {
                m_StateMap.Add(id, state);
            }
        }

        public static bool FindState(U id, out IState<U, V> target) {
            bool ret = m_StateMap.TryGetValue(id, out target);
            return ret;
        }

        protected V m_Target;
        protected static Dictionary<U, IState<U, V>> m_StateMap = new Dictionary<U, IState<U, V>>();
        protected U m_CurrKey;
        protected IState<U, V> m_CurrStatus = null;
    }
}

