using System;
using System.Threading;
using System.IO;

namespace Utils
{
	public class CustomThread: DisposeObject
	{
		// 线程是否在运行
        public bool IsThreadRuning
        {
            get {
                return m_Thread != null && m_Thread.ThreadState == ThreadState.Running 
                    && LocalThreadState == ThreadState.Running;
            }
        }

        public CustomThread()
        {
            LocalThreadState = ThreadState.Unstarted;
            m_IsCallEnd = false;
        }

        protected virtual void End()
        {}

        private void CallEnd()
        {
            if (m_IsCallEnd)
                return;
            m_IsCallEnd = true;

            End ();
        }

        private void OnCheckEndStatus(Timer obj, float timer)
        {
            if (LocalThreadState == ThreadState.Aborted) {
                CallEnd ();
                if (m_Time != null) {
                    m_Time.Dispose ();
                    m_Time = null;
                }
            }
        }

        // 开始线程
        public void Start()
        {
            if (m_Time == null) {
                m_Time = TimerMgr.Instance.CreateTimer (false, 0, true, true);
                m_Time.AddListener (OnCheckEndStatus);
            }

            if (m_Thread == null) {
                m_IsCallEnd = false;
                m_Thread = new Thread (ThreadProc);
                LocalThreadState = ThreadState.Running;
                m_Thread.Start ();
            }
        }

        protected override void OnFree(bool isManual)
        {
            // isCallEnd目的：保证OnEnd事件里可以再调用Dispose不会陷入死循环
            bool isCallEnd = false;
            if (m_Thread != null) {
                Abort ();
                m_Thread.Join();
                isCallEnd = true;
            }  

            if (m_Time != null) {
                m_Time.Dispose ();
                m_Time = null;
            }
                
            m_Thread = null;

            if (isCallEnd)
                CallEnd ();
        }

        protected virtual void Execute()
        {}

        private void ThreadProc()
        {
            while (IsThreadRuning) {
                try
                {
                    Execute();
                    Thread.Sleep(1);
                } catch (ThreadAbortException ex) {
                    #if DEBUG
                    // 不做处理
					UnityEngine.Debug.Log(ex.ToString());
                    #endif
                }
            }

            LocalThreadState = ThreadState.Aborted;
        }

        protected void Abort()
        {
            if (LocalThreadState == ThreadState.Running)
                LocalThreadState = ThreadState.AbortRequested;
        }

        // 用于模拟abort
        protected ThreadState LocalThreadState {
            get {
                lock (m_Mutex) {
                    return m_ThreadStatus;
                }
            }

            set {
                lock (m_Mutex) {
                    m_ThreadStatus = value;
                }
            }
        }

        // 线程句柄
        private Thread m_Thread = null;
        // 线程状态
        private ThreadState m_ThreadStatus = ThreadState.Unstarted;
        // 锁
        private System.Object m_Mutex = new object();
        // 检测线程状态
        private Timer m_Time = null;

        private bool m_IsCallEnd = false;

	}
}
