using System;
using System.Collections;
using System.Collections.Generic;
using Utils;

namespace NsHttpClient
{
	public static class HttpHelper
	{
		private static ObjectPool<HttpClient> m_Pool = new ObjectPool<HttpClient>();
		private static bool m_InitPool = false;
		private static LinkedList<HttpClient> m_LinkList = new LinkedList<HttpClient>();
		private static Timer m_Timer = null;

		public static int PoolCount
		{
			get
			{
				return m_Pool.Count;
			}
		}

		public static int RunCount
		{
			get
			{
				return m_LinkList.Count;
			}
		}

		private static void InitPool()
		{
			if (m_InitPool)
				return;
			m_InitPool = true;
			m_Pool.Init(0);
		}

		private static void InPool(HttpClient client)
		{
			if (client == null)
				return;
			InitPool();
            client.UserData = null;
            client.Clear();
			m_Pool.Store(client);
		}

		private static void OnTimerEvent(Timer obj, float timer)
		{
			int cnt = m_LinkList.Count;
			int tick = 0;
			var node = m_LinkList.First;
			while (node != null)
			{
				if (tick >= cnt || tick >= 5)
					break;

				var next = node.Next;

				if (node.Value.Listener == null || node.Value.Listener.IsEnd)
				{
                    HttpClientCallBack callBack = node.Value.UserData as HttpClientCallBack;
					if (callBack != null)
					{
						if (node.Value.Listener != null)
						{
                            // 再调用一次Process
                            if (callBack.OnProcess != null)
                                callBack.OnProcess(node.Value);

                            if (callBack.OnEnd != null)
                                callBack.OnEnd(node.Value, node.Value.Listener.Status);
						} else
						{
                            // 外部主动调用关闭
                            callBack.OnEnd(node.Value, HttpListenerStatus.hsClosed);
						}
                    }

					m_LinkList.Remove(node);
					// 断开HttpClient和Listener之间的链接
					InPool(node.Value);
				} else
				{
                    HttpClientCallBack callBack = (HttpClientCallBack)node.Value.UserData;
                    if (callBack != null && callBack.OnProcess != null)
                        callBack.OnProcess(node.Value);

                    m_LinkList.Remove(node);
					m_LinkList.AddLast(node);
				}

				node = next;

				++tick;
			}
		}

		private static void InitTimer()
		{
			if (m_Timer == null)
			{
				m_Timer = TimerMgr.Instance.CreateTimer(false, 0, true, true);
				m_Timer.AddListener(OnTimerEvent);
			}
		}

		private static HttpClient CreateHttpClient()
		{
			InitPool();
			HttpClient ret = m_Pool.GetObject();
			if (ret != null)
			{
				ret.Clear();
				ret.IsUsedPool = true;
				InitTimer();
			}
			return ret;
		}

        private class HttpClientCallBack {
            public Action<HttpClient, HttpListenerStatus> OnEnd {
                get;
                set;
            }

            public Action<HttpClient> OnProcess {
                get;
                set;
            }
        }

		// 保证OpenUrl在主线程调用
		public static HttpClient OpenUrl<T>(string url, T listener, long filePos,  Action<HttpClient, HttpListenerStatus> OnEnd = null, Action<HttpClient> OnProcess = null, float timeOut = 5.0f) where T: HttpClientResponse
		{
			if (string.IsNullOrEmpty(url) || listener == null || filePos < 0)
				return null;

			HttpClient ret = CreateHttpClient();
            HttpClientCallBack callBack = null;
            if (OnEnd != null || OnProcess != null) {
                callBack = new HttpClientCallBack();
                callBack.OnEnd = OnEnd;
                callBack.OnProcess = OnProcess;
            }
            ret.UserData = callBack;
			ret.Init(url, listener, filePos, timeOut);
			m_LinkList.AddLast(ret.LinkNode);

			return ret;
		}

		public static void OnAppExit()
		{
			var node = m_LinkList.First;
			while (node != null)
			{
				var next = node.Next;
				HttpClient client = node.Value;
				InPool(client);
				node = next;
			}
			m_LinkList.Clear();
		}

        public static string AddTimeStamp(string url) {
            bool isHttps = url.StartsWith("https://", StringComparison.CurrentCultureIgnoreCase);
            if (!isHttps) {
                if (url.IndexOf('?') > 0)
                    url += string.Format("&t={0}", DateTime.UtcNow.Ticks.ToString());
                else
                    url += string.Format("?t={0}", DateTime.UtcNow.Ticks.ToString());
            }
            return url;
        }

        // 保证OpenUrl在主线程调用
        public static HttpClient OpenUrl<T>(string url, T listener, Action<HttpClient, HttpListenerStatus> OnEnd = null, Action<HttpClient> OnProcess = null, float timeOut = 5.0f) where T: HttpClientResponse
		{
			if (string.IsNullOrEmpty(url) || listener == null)
				return null;

			return OpenUrl<T>(url, listener, 0, OnEnd, OnProcess, timeOut);
		}

	}
}
