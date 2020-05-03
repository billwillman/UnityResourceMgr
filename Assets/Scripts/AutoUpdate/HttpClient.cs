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
        hsRequesting,
        hsRequested,
        // 等待链接
        hsWating,
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
		void OnResponse(HttpWebResponse rep, HttpClient client);
        void OnEnd();
        // 流写入
        void OnRequesting();
        void OnRequested();
        bool MainThreadOnRequestEnd();
        // Post相关
        long GetPostContentLength();
        // Post上传文件
        string GetPostFileName();
        // 
        string GetContentType();
        // Post上传文件写入，返回false则全部传完了，true则还要继续写入文件没有写完
        bool WritePostStream(Stream stream);
        //-----

        HttpListenerStatus Status
		{
			get;
		}

		bool IsEnd
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

        public virtual bool MainThreadOnRequestEnd() {
            return true;
        }

        public virtual string GetContentType() {
            return string.Empty;
        }


        public void OnStart()
		{
			DownProcess = 0;
			Status = HttpListenerStatus.hsWating;
		}

        public virtual long GetPostContentLength() {
            return 0;
        }

        public virtual string GetPostFileName() {
            return string.Empty;
        }

        public void OnRequesting() {
            Status = HttpListenerStatus.hsRequesting;
        }

        public void OnRequested() {
            Status = HttpListenerStatus.hsRequested;
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

		public bool IsEnd
		{
			get
			{
				return (Status != HttpListenerStatus.hsDoing) && (Status != HttpListenerStatus.hsWating) &&
						(Status != HttpListenerStatus.hsNone) && (Status != HttpListenerStatus.hsRequesting) &&
                        (Status != HttpListenerStatus.hsRequested);
			}
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

			protected set
			{
				lock (this)
				{
					m_Status = value;
				}
			}
		}

        public virtual bool WritePostStream(Stream stream) {
            return false;
        }


        private HttpListenerStatus m_Status = HttpListenerStatus.hsNone;

		protected virtual void DoClose()
		{
			try
			{
            //    UnityEngine.Debug.LogFormat("<color=Yellow>DoClose:</color><color=green> {0:D}</color>", (this as HttpClientResponse).GetHashCode());
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

				m_Client = null;
			} catch
			{
			}
		}

        // 读取完成
        public void OnEnd() {
			DownProcess = 1.0f;
            End();
            if (OnEndEvt != null)
                OnEndEvt(this);
			Status = HttpListenerStatus.hsDone;
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


		public void OnResponse(HttpWebResponse rep, HttpClient client)
		{
			if (rep == null)
			{
                OnError(-1);
                return;
			}

			/*
			if (rep.StatusCode == HttpStatusCode.PartialContent)
				m_Rep = rep;
			else*/
				m_Rep = rep;
			m_Client = client;
			try
			{
                Status = HttpListenerStatus.hsDoing;
				m_OrgStream = rep.GetResponseStream();
				if (m_OrgStream == null)
				{
                    OnError(-1);
					return;
				}

				if (m_OrgStream.CanTimeout)
					m_OrgStream.ReadTimeout = _cReadTimeOut;
                System.Threading.Interlocked.Exchange(ref m_MaxReadBytes, rep.ContentLength);
				//m_MaxReadBytes = rep.ContentLength;
				if (m_MaxReadBytes == -1) {
                    // 有些页面会返回-1，ContentLength会忽略 
                    // 但客户端需要最大长度否则有问题
                } else
                if (m_MaxReadBytes <= 0) {
                    OnEnd();
                    DoClose();
                    return;
                }


				//	UnityEngine.Debug.Log("OnResponse");
				m_OrgStream.BeginRead(m_Buf, 0, m_Buf.Length, m_ReadCallBack, this);
				// don't m_OrgStream.Close
			} catch
			{
                OnError(-1);
            }
		}

		protected virtual void Flush(int read)
		{}

		private void DoFlush(int read)
		{
			// 设置下载进度
            float down = 0;
            bool isDone = (IsIngoreMaxReadBytes && read == 0) ||
                          (!IsIngoreMaxReadBytes && ReadBytes + read >= MaxReadBytes);
            if (isDone)
                down = 1f;
            else if (MaxReadBytes > 0)
                down = (float)((ReadBytes + (long)read)) / ((float)MaxReadBytes);
            DownProcess = down;

            // read > 0通知才有意义
            if (read > 0)
                Flush(read);

            if (isDone)
                OnEnd();

            if (OnReadEvt != null) {
                OnReadEvt(this, ReadBytes + read);
            }
		}

		private static void OnRead(IAsyncResult result)
		{
			if (result == null) {
                return;
            }

            HttpClientResponse req = result.AsyncState as HttpClientResponse;
            if (req == null)
                return;

            try {
                if (req.m_Client != null)
                    req.m_Client.ResetReadTimeOut();
                int read = req.m_OrgStream.EndRead(result);
                if (read > 0) {
                    req.DoFlush(read);
                    System.Threading.Interlocked.Add(ref req.m_ReadBytes, read);
                  //  req.m_ReadBytes += read;
                    // MaxReadBytes为-1，说明忽略了ContentLength
                    if (req.IsIngoreMaxReadBytes || req.ReadBytes < req.MaxReadBytes)
                        req.m_OrgStream.BeginRead(req.m_Buf, 0, req.m_Buf.Length, m_ReadCallBack, req);
                    else {
                        req.DoClose();
                    }
                } else if (req.IsIngoreMaxReadBytes && read == 0) {
                    // 说明HTTP忽略了ContentLength
                    req.DoFlush(read);
                    req.DoClose();
                } else {
                    req.DoClose();
                }
            } catch {
                req.OnError(-1);
            }
		}

		public long ReadBytes
		{
			get
			{
				return m_ReadBytes;
			}
		}
		
		// 是否是忽略ContentLength
        public bool IsIngoreMaxReadBytes {
            get {
                return m_MaxReadBytes == -1;
            }
        }

		public long MaxReadBytes
		{
			get
			{
				return m_MaxReadBytes;
			}
		}
		
		public float DownProcess {
            get {
                lock (this) {
                    return m_DownProcess;
                }
            } private set {
                lock (this) {
                    m_DownProcess = value;
                }
            }
        }


		private static readonly int _cReadTimeOut = 10000;
		private static AsyncCallback m_ReadCallBack = new AsyncCallback(OnRead);
		private Stream m_OrgStream = null;
		private HttpWebResponse m_Rep = null;
		private HttpClient m_Client = null;
		private float m_DownProcess = 0;
		
		protected byte[] m_Buf = null;
		protected long m_ReadBytes = 0;
		protected long m_MaxReadBytes = 0;
	}

    // HttpClient类型：GET 和 POST类型
    public enum HttpClientType {
        httpGet,
        httpPost
    }

    // Http模块
    public class HttpClient: DisposeObject
	{
		public HttpClient()
		{}

		public HttpClient(string url, IHttpClientListener listener, float connectTimeOut, float readTimeOut = 5.0f,
            HttpClientType clientType = HttpClientType.httpGet, string postStr = "", bool isKeepAlive = true, List<X509Certificate> certs = null)
		{
			Init(url, listener, 0, connectTimeOut, readTimeOut, clientType, GeneratorPostBuf(clientType, postStr), isKeepAlive, certs);
		}

        public HttpClient(string url, IHttpClientListener listener, long filePos, float connectTimeOut, float readTimeOut = 5.0f)
		{
			Init(url, listener, filePos, connectTimeOut, readTimeOut);
		}

        private byte[] GeneratorPostBuf(HttpClientType clientType, string postStr) {
            if (clientType == HttpClientType.httpGet || string.IsNullOrEmpty(postStr))
                return null;
            byte[] ret = System.Text.Encoding.UTF8.GetBytes(postStr);
            return ret;
        }

        public void Init(string url, IHttpClientListener listener, long filePos, float connectTimeOut, float readTimeOut = 5.0f,
            HttpClientType clientType = HttpClientType.httpGet, byte[] postBuf = null, bool isKeepAlive = true, List<X509Certificate> certs = null)
		{
			m_Url = url;
			m_TimeOut = connectTimeOut;
            m_ReadTimeOut = readTimeOut;
            m_Listener = listener;
			m_FilePos = filePos;
            m_ClientType = clientType;
            m_PostBuf = postBuf;
            m_IsKeppAlive = isKeepAlive;
            m_Certs = certs;
            ResetReadTimeOut();
            ResetConnectTimeOut();

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

        public float ReadTimeout {
            get {
                return m_ReadTimeOut;
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

        private void OnRequest(IAsyncResult result) {
            HttpWebRequest req = result.AsyncState as HttpWebRequest;
            if (req == null)
                return;
            try {
                // ResetReadTimeOut();
                var stream = req.EndGetRequestStream(result);
                if (stream == null) {
                    if (m_Listener != null)
                        m_Listener.OnError(-1);
                    return;
                }

                // 发送消息头
                if (m_PostHeaderBytes != null && m_PostHeaderBytes.Length > 0) {
                    stream.Write(m_PostHeaderBytes, 0, m_PostHeaderBytes.Length);
                    m_PostHeaderBytes = null;
                    stream.Flush();
                }

                // 上传文件:
                if (m_Listener != null) {
                    // 暂时这样。。。
                    while (m_Listener.WritePostStream(stream)) {
                        // 这里要处理下，不然超时了，Post用的是ConnectTime定时器
                        ResetConnectTimeOut();
                        System.Threading.Thread.Sleep(1);
                    }
                }

                if (m_PostBuf != null && m_PostBuf.Length > 0) {
                    stream.Write(m_PostBuf, 0, m_PostBuf.Length);
                    // 置空
                    m_PostBuf = null;
                    stream.Flush();
                }

                if (m_BoundaryBytes != null && m_BoundaryBytes.Length > 0) {
                    stream.Write(m_BoundaryBytes, 0, m_BoundaryBytes.Length);
                    m_BoundaryBytes = null;
                    stream.Flush();
                }

                stream.Close();
                stream.Dispose();

                if (m_Listener != null) {
                    m_Listener.OnRequested();
                }
            } catch (Exception e) {
#if DEBUG
                UnityEngine.Debug.LogError(e.ToString());
#endif
                if (m_Listener != null)
                    m_Listener.OnError(-1);
            }
        }

        private void OnResponse(IAsyncResult result)
		{
			HttpWebRequest req = result.AsyncState as HttpWebRequest;
			if (req == null)
				return;
			try
			{
				ResetReadTimeOut();
				HttpWebResponse rep = req.EndGetResponse(result) as HttpWebResponse;
				if (rep == null)
				{
					if (m_Listener != null)
						m_Listener.OnError(-1);
					return;
				}
				if (rep.StatusCode != HttpStatusCode.OK && rep.StatusCode != HttpStatusCode.PartialContent)
				{
					rep.Close();
					if (m_Listener != null)
						m_Listener.OnError((int)rep.StatusCode);
					return;
				}
				
				if (m_Listener != null)
					m_Listener.OnResponse(rep, this);
				else
					rep.Close();

            }
            catch /*(Exception except)*/
            {
				#if DEBUG
			//	UnityEngine.Debug.LogErrorFormat("OnResponse Exception: {0}", except.ToString());
				#endif
				if (m_Listener != null)
					m_Listener.OnError(-1); 
			}
		}

		internal void Clear()
		{
			Close();
			m_Listener = null;
			m_FilePos = 0;
            m_PostBuf = null;
            m_PostHeaderBytes = null;
            m_BoundaryBytes = null;
            m_IsKeppAlive = false;
            m_Certs = null;
            // 重置为GET
            m_ClientType = HttpClientType.httpGet;
            m_Url = string.Empty;
			m_TimeOut = 5.0f;
            m_ReadTimeOut = 5.0f;
            ResetReadTimeOut();
            ResetConnectTimeOut();
        }

		internal void ResetReadTimeOut() {
            lock (this) {
                m_CurReadTime = m_ReadTimeOut;
            }
        }

        private void ResetConnectTimeOut() {
            lock (this) {
                m_CurConnectTime = m_TimeOut;
            }
        }

        private bool DecConnectTimeOut(float delta) {
            if (delta > 0) {
                lock (this) {
                    m_CurConnectTime -= delta;
                    return m_CurConnectTime <= float.Epsilon;
                }
            }

            return false;
        }

        private bool DecReadTimeOut(float delta) {
            if (delta > 0) {
                lock (this) {
                    m_CurReadTime -= delta;
                    return m_CurReadTime <= float.Epsilon;
                }
            }

            return false;
        }


        private void StopTimeOutTime()
		{
			if (m_TimeOutTimer != null)
			{
				m_TimeOutTimer.Dispose();
				m_TimeOutTimer = null;
			}
		}

        private void StopReadTimeOutTime() {
            if (m_ReadOutTimer != null) {
                m_ReadOutTimer.Dispose();
                m_ReadOutTimer = null;
            }
        }

        private void Close()
		{
			StopTimeOutTime();
            StopReadTimeOutTime();

            if (m_Listener != null)
				m_Listener.OnClose();

			Abort();
		}

		private void Abort()
		{
			try
			{
				if (m_Req != null)
				{
				//  UnityEngine.Debug.LogFormat("<color=red>DoAbort:</color><color=green> {0:D}</color>", (m_Listener as HttpClientResponse).GetHashCode());
					m_Req.Abort();
					m_Req = null;
				}
			} catch
			{
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

        // 主线程
        private void OnTimeOutTime(Timer obj, float timer) {

            if (m_ClientType == HttpClientType.httpPost) {
                if (m_Listener != null) {
                    var status = m_Listener.Status;
                    if (status == HttpListenerStatus.hsRequested) {
                        // 处理一下状态
                        ResetConnectTimeOut();
                        // 返回True表示需要继续监听回包，否则直接Dispose
                        if (m_Listener.MainThreadOnRequestEnd()) {
                            StartResponse();
                        } else {
                            Dispose();
                        }
                        return;
                    }
                }
            }

            if (DecConnectTimeOut(0.033f)) {
                // 408请求超时
                if (m_Listener != null) {
                    var status = m_Listener.Status;
                    if (status == HttpListenerStatus.hsWating ||
                        status == HttpListenerStatus.hsRequesting) {
                        if (m_Listener != null)
                            m_Listener.OnError(408);
                    } else {
                        //	lock (m_TimerLock)
                        {
                            StopTimeOutTime();
                            // 开始读取时间判断
                            StartReadTimeoutTime();
                        }
                        return;
                    }
                }
            }
            // httphelper is Dispose
            // Dispose();
        }

        // 主线程
        private void OnReadTimeoutTime(Timer obj, float timer) {
            // 减去固定时间
            if (DecReadTimeOut(0.033f)) {
                StopReadTimeOutTime();
                if (m_Listener != null) {
                    m_Listener.OnError(-1);
                }
            }
        }

        private string GetPostContentType(out string boundary) {
            string ret = GetPostDefaultContentType();
            boundary = string.Empty;
            if (m_Listener != null) {
                string r = m_Listener.GetPostFileName();
                if (!string.IsNullOrEmpty(r)) {
                    ret = GetPostUpFileContentType(ref boundary);
                }
            }
            return ret;
        }

        private long GetPostContentLength() {
            // m_PostBuf上传和m_Listener只会有一个，优先m_PostBuf
            long ret = m_PostBuf != null ? m_PostBuf.Length: 0;
            if (ret <= 0 && m_Listener != null) {
                long r = m_Listener.GetPostContentLength();
                if (r > 0)
                    ret = r;
            }
            return ret;
        }

        private static string GetBoundary(string NowTicks) {
            string ret = StringHelper.Format("----------{0}", NowTicks);
            return ret;
        }

        // 获得上传文件ContentType
        public static string GetPostUpFileContentType(ref string boundary) {
            if (string.IsNullOrEmpty(boundary))
                boundary = GetBoundary(DateTime.Now.Ticks.ToString("x"));
            string ret = StringHelper.Format("multipart/form-data; boundary={0}", boundary);
            return ret;
        }

        public static string GetPostDefaultContentType() {
            return "application/x-www-form-urlencoded";
        }

        // 上传文件会要用到的
        private string BuildPostMessageHeader(string boundary, string upFileName) {
            if (m_Listener == null)
                return string.Empty;
            string fileName = Path.GetFileName(upFileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            string contentType = m_Listener.GetContentType();
            if (string.IsNullOrEmpty(contentType))
                contentType = "application/octet-stream";
            string ret = StringHelper.Format("--{0} Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\" Content-Type: {3}  ",
                   boundary, name, fileName, contentType);
            return ret;
        }

        private byte[] m_PostHeaderBytes = null;
        private byte[] m_BoundaryBytes = null;
        private void BuildPostMessageHeader(string boundary) {
            if (m_Listener != null && m_Req != null && !string.IsNullOrEmpty(boundary)) {
                // NowTicks有值说明是UpFile
                string upFileName = m_Listener.GetPostFileName();
                if (!string.IsNullOrEmpty(upFileName)) {
                    string messageHeader = BuildPostMessageHeader(boundary, upFileName);
                    if (string.IsNullOrEmpty(messageHeader))
                        return;
                    
                    m_PostHeaderBytes = System.Text.Encoding.UTF8.GetBytes(messageHeader);
                    m_BoundaryBytes = System.Text.Encoding.UTF8.GetBytes(boundary);

                    m_Req.ContentLength = m_PostHeaderBytes.LongLength + m_Req.ContentLength + m_BoundaryBytes.LongLength;
                }
            }
        }

        private void Start()
		{
            m_Req = WebRequest.Create(m_Url) as HttpWebRequest;
            m_Req.AllowAutoRedirect = true;
            m_Req.KeepAlive = m_IsKeppAlive;
            // 增加证书
            if ((m_Certs != null) && (m_Certs.Count > 0)) {
                for (int i = 0; i < m_Certs.Count; ++i) {
                    var cert = m_Certs[i];
                    if (cert != null) {
                        m_Req.ClientCertificates.Add(cert);
                    }
                }
            }

            var serverPoint = m_Req.ServicePoint;
            if (serverPoint != null) {
                serverPoint.ConnectionLimit = 512;
                serverPoint.Expect100Continue = false;
            }

            m_Req.Timeout = (int)(m_TimeOut * 1000);
            m_Req.ReadWriteTimeout = (int)(m_ReadTimeOut * 1000);
            m_Req.Proxy = null;

            if (m_FilePos > 0) {
                m_Req.AddRange((int)m_FilePos);
            }

            // Post判断安全, 必须要有PostBuf
            long postContentLen = GetPostContentLength();
            if (postContentLen <= 0) {
                m_PostBuf = null;
                m_BoundaryBytes = null;
                m_PostHeaderBytes = null;
                m_ClientType = HttpClientType.httpGet;
            } else {
                m_ClientType = HttpClientType.httpPost;
            }

            if (m_ClientType == HttpClientType.httpGet) {
                m_Req.Method = "GET";
                StartResponse();
            } else {
                m_Req.Method = "POST";
                string boundary;
                m_Req.ContentType = GetPostContentType(out boundary);
                m_Req.ContentLength = postContentLen; // 还需要增加MessageHeader大小
                BuildPostMessageHeader(boundary);
                StartRequest();
            }

            StartTimeoutTime();
        }

        private void StartRequest() {
            AsyncCallback callBack = new AsyncCallback(OnRequest);
            m_Req.BeginGetRequestStream(callBack, m_Req);
            if (m_Listener != null)
                m_Listener.OnRequesting();
        }

        private void StartResponse() {
            AsyncCallback callBack = new AsyncCallback(OnResponse);
            m_Req.BeginGetResponse(callBack, m_Req);

            if (m_Listener != null)
                m_Listener.OnStart();
        }

        private void StartReadTimeoutTime() {
            if (m_ReadOutTimer != null)
                m_ReadOutTimer.Dispose();
            ResetReadTimeOut();
            m_ReadOutTimer = TimerMgr.Instance.CreateTimer(0, true, true);
            m_ReadOutTimer.AddListener(OnReadTimeoutTime);
        }

        private void StartTimeoutTime()
		{
			if (m_TimeOutTimer != null)
				m_TimeOutTimer.Dispose();
            ResetConnectTimeOut();
            m_TimeOutTimer = TimerMgr.Instance.CreateTimer(0, true, true);
			m_TimeOutTimer.AddListener(OnTimeOutTime);
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
            ServicePointManager.DefaultConnectionLimit = _cMaxConnectionLimit;
            ServicePointManager.Expect100Continue = false;
		}

        private static readonly  int _cMaxConnectionLimit = 512;

		private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)  
		{
			return true;
		}

		private string m_Url;
		private float m_TimeOut = 5.0f;
        private float m_ReadTimeOut = 5.0f;
        private float m_CurReadTime = 5.0f;
        private float m_CurConnectTime = 5.0f;
		private ITimer m_TimeOutTimer = null;
        private ITimer m_ReadOutTimer = null;
        private List<X509Certificate> m_Certs = null;
        // 默认采用GET模式
        private HttpClientType m_ClientType = HttpClientType.httpGet;
        //	private static System.Object m_TimerLock = new object();
        private HttpWebRequest m_Req = null;
		private IHttpClientListener m_Listener = null;
		private long m_FilePos = 0;
        private byte[] m_PostBuf = null;
        private bool m_IsKeppAlive = false;
        private LinkedListNode<HttpClient> m_LinkNode = null;

		private static bool m_IsServerPointInited = false;
	}

}
