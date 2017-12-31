using System;
using System.Collections.Generic;
using System.Threading;

namespace Utils {

    public class ThreadPoolThread: CustomThread {

        public ThreadPoolThread(Action<System.Object> onThreadProcess, System.Object state, Action<ThreadPoolThread> onMainThreadEnd = null) {
            Init(onThreadProcess, state, onMainThreadEnd);
        }

        public void Init(Action<System.Object> onThreadProcess, System.Object state, Action<ThreadPoolThread> onMainThreadEnd = null) {
            OnThreadProcess = onThreadProcess;
            OnMainThreadEnd = onMainThreadEnd;
            State = state;
        }

        // 子线程回调，处理
        public Action<System.Object> OnThreadProcess {
            get;
            private set;
        }

        // 主线程回调，线程结束
        public Action<ThreadPoolThread> OnMainThreadEnd {
            get;
            private set;
        }

        public System.Object State {
            get;
            private set;
        }

        // 加入事件
        protected override void Execute() {
            if (OnThreadProcess != null)
                OnThreadProcess(State);
            // 直接退出即可
            Abort();
        }

        protected override void End() {
            if (OnMainThreadEnd != null)
                OnMainThreadEnd(this);
        }
    }

    // 用来代替C#的ThreadPool,原因：在IL2CPP容易卡死
    public class ThreadPool {
        private Stack<ThreadPoolThread> m_Pool = new Stack<ThreadPoolThread>();
        private static ThreadPool m_MainThreadPool = null;

        public static ThreadPool MainThreadPool {
            get {
                if (m_MainThreadPool == null)
                    m_MainThreadPool = new ThreadPool();
                return m_MainThreadPool;
            }
        }

        public void Clear() {
            ThreadPoolThread thread = m_Pool.Pop();
            while (thread != null) {
                thread.Dispose();
                thread = m_Pool.Pop();
            }
            m_Pool.Clear();
        }

        // 线程池创建线程
        private ThreadPoolThread CreateThread(Action<System.Object> onThreadProcess, System.Object state = null,
            Action<ThreadPoolThread> onMainThreadEnd = null) {
            ThreadPoolThread ret = m_Pool.Pop();
            if (ret == null)
                ret = new ThreadPoolThread(onThreadProcess, state, onMainThreadEnd);
            else {
                ret.Init(onThreadProcess, state, onMainThreadEnd);
            }
            // 线程开始
            ret.Start();
            return ret;
        }

        // 模拟系统C#使用线程
        public static void QueueUserWorkItem(Action<System.Object> onThreadProcess, System.Object state) {
            MainThreadPool.CreateThread(onThreadProcess, state, OnMainThreadEnd);
        }

        // 主线程调用
        private static void OnMainThreadEnd(ThreadPoolThread thread) {
            if (thread == null)
                return;
            // 线程回线程池
            MainThreadPool.DestroyThread(thread);
        }

        // 线程池销毁线程
        private void DestroyThread(ThreadPoolThread thread) {
            if (thread == null)
                return;
            thread.Dispose();
            m_Pool.Push(thread);
        }
    }
}