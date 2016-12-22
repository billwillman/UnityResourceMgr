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

				if (node.Value.Listener == null || node.Value.Listener.Status != HttpListenerStatus.hsDoing)
				{
					if (node.Value.Listener != null)
					{
						Action<HttpClient, HttpListenerStatus> onEnd = node.Value.UserData as Action<HttpClient, HttpListenerStatus>;
						if (onEnd != null)
							onEnd(node.Value, node.Value.Listener.Status);
					}

					m_LinkList.Remove(node);
					// 断开HttpClient和Listener之间的链接
					InPool(node.Value);
				} else
				{
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

		// 保证OpenUrl在主线程调用
		public static HttpClient OpenUrl<T>(string url, T listener, long filePos, Action<HttpClient, HttpListenerStatus> OnEnd, float timeOut = 5.0f) where T: HttpClientResponse
		{
			if (string.IsNullOrEmpty(url) || listener == null || filePos < 0)
				return null;

			HttpClient ret = CreateHttpClient();
			ret.UserData = OnEnd;
			ret.Init(url, listener, filePos, timeOut);
			m_LinkList.AddLast(ret.LinkNode);

			return ret;
		}

		// 保证OpenUrl在主线程调用
		public static HttpClient OpenUrl<T>(string url, T listener, Action<HttpClient, HttpListenerStatus> OnEnd, float timeOut = 5.0f) where T: HttpClientResponse
		{
			if (string.IsNullOrEmpty(url) || listener == null)
				return null;

			HttpClient ret = CreateHttpClient();
			ret.UserData = OnEnd;
			ret.Init(url, listener, timeOut);
			m_LinkList.AddLast(ret.LinkNode);

			return ret;
		}

	}
}
