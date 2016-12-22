using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Utils;

namespace NsHttpClient
{
	public enum HttpListenerStatus
	{
		hsNone,
		// 发生错误
		hsError,
		// 正在进行
		hsDoing,
		// 进行完成
		hsDone,
		// 被主动关闭
		hsClosed
	}

	public interface IHttpClientListener
	{
		void OnStart();
		void OnClose();
		void OnError(int status);
		void OnResponse(HttpWebResponse rep);
        void OnEnd();

		HttpListenerStatus Status
		{
			get;
		}
    }

	public class HttpClientResponse: IHttpClientListener
	{
		public HttpClientResponse(int bufSize)
		{
			Init(bufSize);
		}

		public void Init(int bufSize)
		{
			m_Buf = new byte[bufSize];
		}

		public void OnStart()
		{
			Status = HttpListenerStatus.hsDoing;
		}

		public void OnClose()
		{
			Status = HttpListenerStatus.hsNone;
			DoClose();
		}
		
		public void Close() {
            OnClose();
        }

		public Action<HttpClientResponse, long> OnReadEvt
		{
			get;
			set;
		}
		
		public Action<HttpClientResponse, int> OnErrorEvt
		{
			get;
			set;
		}

        // 读取完成
        public Action<HttpClientResponse> OnEndEvt {
            get;
            set;
        }

        public System.Object UserData
		{
			get;
			set;
		}

		public HttpListenerStatus Status
		{
			get
			{
				lock (this)
				{
					return m_Status;
				}
			}

			internal set
			{
				lock (this)
				{
					m_Status = value;
				}
			}
		}

		private HttpListenerStatus m_Status = HttpListenerStatus.hsNone;

		protected virtual void DoClose()
		{
			if (m_OrgStream != null)
			{
				m_OrgStream.Close();
				m_OrgStream.Dispose();
				m_OrgStream = null;
			}

			if (m_Rep != null)
			{
				m_Rep.Close();
				m_Rep = null;
			}
		}

        // 读取完成
        public void OnEnd() {
			Status = HttpListenerStatus.hsDone;
            End();
            if (OnEndEvt != null)
                OnEndEvt(this);
        }

        protected virtual void End() {
        }

        public virtual void OnError(int status)
		{
			Status = HttpListenerStatus.hsError;

			if (OnErrorEvt != null)
			{
				OnErrorEvt(this, status);
			}

			DoClose();
		}


		public void OnResponse(HttpWebResponse rep)
		{
			if (rep == null)
			{
				DoClose();
				return;
			}

			/*
			if (rep.StatusCode == HttpStatusCode.PartialContent)
				m_Rep = rep;
			else*/
				m_Rep = rep;
			
			m_OrgStream = rep.GetResponseStream();
			if (m_OrgStream == null)
			{
				DoClose();
				return;
			}

			if (m_OrgStream.CanTimeout)
				m_OrgStream.ReadTimeout = _cReadTimeOut;

			m_MaxReadBytes = rep.ContentLength;
			if (m_MaxReadBytes <= 0)
			{
				DoClose();
				return;
			}
		//	UnityEngine.Debug.Log("OnResponse");
			m_OrgStream.BeginRead(m_Buf, 0, m_Buf.Length, m_ReadCallBack, this);
			// don't m_OrgStream.Close
		}

		protected virtual void Flush(int read)
		{}

		private void DoFlush(int read)
		{
			Flush(read);

			if (OnReadEvt != null)
			{
				OnReadEvt(this, ReadBytes + read);
			}
		}

		private static void OnRead(IAsyncResult result)
		{
		//	UnityEngine.Debug.Log("OnRead Start");

			if (result == null)
			{
				return;
			}

			HttpClientResponse req = result.AsyncState as HttpClientResponse;
			if (req == null)
				return;

			int read = req.m_OrgStream.EndRead(result);
			if (read > 0)
			{
				req.DoFlush(read);
				req.m_ReadBytes += read;
                if (req.ReadBytes < req.MaxReadBytes)
                    req.m_OrgStream.BeginRead(req.m_Buf, 0, req.m_Buf.Length, m_ReadCallBack, req);
                else {
                    req.OnEnd();
                    req.DoClose();
                }
			} else
			{
				req.DoClose();
			//	UnityEngine.Debug.Log("OnRead Close");
			}
		}

		public long ReadBytes
		{
			get
			{
				return m_ReadBytes;
			}
		}

		public long MaxReadBytes
		{
			get
			{
				return m_MaxReadBytes;
			}
		}


		private static readonly int _cReadTimeOut = 10000;
		private static AsyncCallback m_ReadCallBack = new AsyncCallback(OnRead);
		private Stream m_OrgStream = null;
		private HttpWebResponse m_Rep = null;

		protected byte[] m_Buf = null;
		protected long m_ReadBytes = 0;
		protected long m_MaxReadBytes = 0;
	}

	// Http模块
	public class HttpClient: DisposeObject
	{
		public HttpClient()
		{}

		public HttpClient(string url, IHttpClientListener listener, float timeOut)
		{
			Init(url, listener, 0, timeOut);
		}
			
		public HttpClient(string url, IHttpClientListener listener, long filePos, float timeOut)
		{
			Init(url, listener, filePos, timeOut);
		}

		public void Init(string url, IHttpClientListener listener, long filePos, float timeOut)
		{
			m_Url = url;
			m_TimeOut = timeOut;
			m_Listener = listener;
			m_FilePos = filePos;

			CheckServicePoint();
			// Get
			Start();
		}

		public string Url
		{
			get
			{
				return m_Url;
			}
		}

		public float Timeout
		{
			get
			{
				return m_TimeOut;
			}
		}

		public System.Object UserData
		{
			get;
			set;
		}

		public static void ResetServerPointCallBack()
		{
			m_IsServerPointInited = false;
		}

		public IHttpClientListener Listener
		{
			get
			{
				return m_Listener;
			}
		}

		internal LinkedListNode<HttpClient> LinkNode
		{
			get
			{
				if (m_LinkNode == null)
					m_LinkNode = new LinkedListNode<HttpClient>(this);
				return m_LinkNode;
			}
		}

		private void OnResponse(IAsyncResult result)
		{
			lock (m_TimerLock)
			{
				if (m_TimeOutTimer != null)
				{
					m_TimeOutTimer.Dispose();
					m_TimeOutTimer = null;
				}
			}

			HttpWebRequest req = result.AsyncState as HttpWebRequest;
			if (req == null)
				return;
			try
			{
				HttpWebResponse rep = req.EndGetResponse(result) as HttpWebResponse;
				if (rep == null)
					return;
				if (rep.StatusCode != HttpStatusCode.OK && rep.StatusCode != HttpStatusCode.PartialContent)
				{
					rep.Close();
					if (m_Listener != null)
						m_Listener.OnError((int)rep.StatusCode);
					return;
				}
				
				if (m_Listener != null)
					m_Listener.OnResponse(rep);
				else
					rep.Close();

			} catch (Exception except)
			{
				UnityEngine.Debug.LogErrorFormat("OnResponse Exception: {0}", except.ToString());
				if (m_Listener != null)
					m_Listener.OnError(-1); 
			}
		}

		internal void Clear()
		{
			Close();
			UserData = null;
			m_Listener = null;
			m_FilePos = 0;
			m_Url = string.Empty;
			m_TimeOut = 5.0f;
		}

		private void Close()
		{
			lock (m_TimerLock)
			{
				if (m_TimeOutTimer != null)
				{
					m_TimeOutTimer.Dispose();
					m_TimeOutTimer = null;
				}
			}

			if (m_Listener != null)
				m_Listener.OnClose();

			if (m_Req != null)
			{
				m_Req.Abort();
				m_Req = null;
			}
		}

		internal bool IsUsedPool
		{
			get;
			set;
		}

		public override void Dispose()
		{
			if (!IsUsedPool)
				base.Dispose();
			else
				Clear();
		}

		private void OnTimeOutTime(Timer obj, float timer)
		{
			// 408请求超时
			if (m_Listener != null)
				m_Listener.OnError(408);
			Dispose();
		}

		private void Start()
		{
			m_Req = WebRequest.Create(m_Url) as HttpWebRequest;
			m_Req.Timeout = (int)(m_TimeOut * 1000);
			if (m_FilePos > 0)
			{
				m_Req.AddRange((int)m_FilePos);
			}
			AsyncCallback callBack = new AsyncCallback(OnResponse);
			m_Req.BeginGetResponse(callBack, m_Req);

			lock (m_TimerLock)
			{
				if (m_TimeOutTimer != null)
					m_TimeOutTimer.Dispose();
				m_TimeOutTimer = TimerMgr.Instance.CreateTimer(true, m_TimeOut, true, true);
				m_TimeOutTimer.AddListener(OnTimeOutTime);
			}

			if (m_Listener != null)
				m_Listener.OnStart();
		}

		protected override void OnFree(bool isManual)
		{
			Close();
		}

		private static void CheckServicePoint()
		{
			if (m_IsServerPointInited)
				return;
			m_IsServerPointInited = true;
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
		}

		private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)  
		{
			return true;
		}

		private string m_Url;
		private float m_TimeOut = 5.0f;
		private Timer m_TimeOutTimer = null;
		private static System.Object m_TimerLock = new object();
		private HttpWebRequest m_Req = null;
		private IHttpClientListener m_Listener = null;
		private long m_FilePos = 0;
		private LinkedListNode<HttpClient> m_LinkNode = null;

		private static bool m_IsServerPointInited = false;
	}

}
