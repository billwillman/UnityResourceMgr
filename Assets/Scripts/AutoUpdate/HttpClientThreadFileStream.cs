/*
 * 多线程多文件下载
 * 目的：加速下载速度
*/

using System;
using System.Collections;
using System.Collections.Generic;
using NsHttpClient;

namespace AutoUpdate
{

	// 多线程多文件下载, 每个文件一个线程
	public class HttpClientThreadFileStream
	{

		protected class FileThreadInfo
		{
			public HttpClient client
			{
				get;
				set;
			}
			public int fileIdx
			{
				get;
				set;
			}

			public bool IsIdle
			{
				get
				{
					return (fileIdx < 0);
				}
			}

			public void Reset()
			{
				if (client != null)
				{
					client.Dispose();
					client = null;
				}

				fileIdx = -1;
			}
		}

		public HttpClientThreadFileStream(AutoUpdateCfgItem[] data, int maxThreadCnt = 5)
		{
			m_Data = data;

			if (maxThreadCnt > 0)
			{
				lock (m_Lock)
				{
					m_Clients = new FileThreadInfo[maxThreadCnt];
					for (int i = 0; i < m_Clients.Length; ++i)
					{
						FileThreadInfo info = new FileThreadInfo();
						info.Reset();
						m_Clients[i] = info;
					}
				}
			}
		}

		// 只是开启下载线程
		public bool Start()
		{
			Stop();
			RefreshDownBytes();
			if (m_Clients == null || m_Clients.Length <= 0 || m_Data == null || m_Data.Length <= 0)
				return false;

			FileIdx = -1;
			StartNextDown();

			return true;
		}

		// 清理所有
		public void Clear()
		{
			Stop();
			if (m_Data != null)
				m_Data = null;
			AutoUpdateMgr.Instance.CurDownM = 0;
		}

		// 只是关闭下载线程
		public void Stop()
		{
			lock (m_Lock)
			{
				if (m_Clients != null)
				{
					for (int i = 0; i < m_Clients.Length; ++i)
					{
						FileThreadInfo client = m_Clients[i];
						client.Reset();
					}
				}
			}

			FileIdx = -1;
			HasDownError = false;
		}

		void OnHttpRead(HttpClientResponse response, long totalRead)
		{
			FileThreadInfo info = response.UserData as FileThreadInfo;
			if (info != null && m_Data != null && info.fileIdx >= 0)
			{
				AutoUpdateCfgItem item = m_Data[info.fileIdx];
				long delta = totalRead - response.ReadBytes;
				if (delta > 0)
				{
					double curM = AutoUpdateMgr.Instance.CurDownM;
					curM += ((double)delta)/((double)1024 * 1024);
					AutoUpdateMgr.Instance.CurDownM = curM;
					item.readBytes += delta;
				}

				if (totalRead >= response.MaxReadBytes)
				{
					item.isDone = true;
				}

				m_Data[info.fileIdx] = item;

				lock (m_Lock)
				{
					AutoUpdateMgr.Instance.DownloadUpdateToUpdateTxt(item);
				}

				if (totalRead >= response.MaxReadBytes)
				{
					lock (m_Lock)
					{
						info.Reset();
					}

					StartNextDown();
				}
			}
		}

		void OnHttpError(HttpClientResponse response, int status)
		{
			FileThreadInfo info = response.UserData as FileThreadInfo;
			if (info != null && info.fileIdx >= 0)
			{
				lock (m_Lock)
				{
					if (m_Data != null && m_Data.Length > 0)
					{
						var item = m_Data[info.fileIdx];
						UnityEngine.Debug.LogErrorFormat("[downloadFileErr]{0} download: {1:D} isOk: {2}", item.fileContentMd5,
							item.readBytes, item.isDone.ToString());
					}

					info.Reset();
				}
			}

			HasDownError = true;
			StartNextDown();
		}

		private void CallEndEvent()
		{
			if (m_Data == null)
				return;
			
			int fileIdx = this.FileIdx;
			if (fileIdx + 1 < m_Data.Length)
				return;

			if (m_Clients != null)
			{
				for (int i = 0; i < m_Clients.Length; ++i)
				{
					var client = m_Clients[i];
					if (!client.IsIdle)
						return;
				}
			}

			if (HasDownError)
			{
				if (OnError != null)
					OnError();
			} else
			{
				if (OnFinished != null)
					OnFinished();
			}
		}

		// 开始当前位置下载
		private void StartNextDown()
		{
			bool isOver = true;
			lock (m_Lock)
			{
				if (m_Data == null || m_Data.Length <= 0)
					return;

				// 获得空闲线程
				FileThreadInfo idleThread = null;
				for (int i = 0; i < m_Clients.Length; ++i)
				{
					var info = m_Clients[i];
					if (info.IsIdle)
					{
						idleThread = info;
						break;
					}
				}

				if (idleThread == null)
					return;

				++m_FileIdx;
				while (m_FileIdx < m_Data.Length)
				{
					var item = m_Data[m_FileIdx];
					if (item.isDone)
					{
						// 下载大小在一开始已经计算过了，所以这里不计算
						++m_FileIdx;
						continue;
					}

					isOver = false;
					string url = string.Format("{0}/{1}/{2}", AutoUpdateMgr.Instance.ResServerAddr, 
						AutoUpdateMgr.Instance.CurrServeResrVersion,
						item.fileContentMd5);
					
					HttpClient client = AutoUpdateMgr.Instance.CreateMultHttpFile(url, item.readBytes, OnHttpRead, OnHttpError);
					if (client != null)
					{
						HttpClientResponse rep = client.Listener as HttpClientResponse;
						if (rep != null)
						{
							idleThread.client = client;
							idleThread.fileIdx = m_FileIdx;
							rep.UserData = idleThread;
						}
					}

					// 查找下一个空闲的线程
					idleThread = null;
					for (int i = 0; i < m_Clients.Length; ++i)
					{
						var info = m_Clients[i];
						if (info.IsIdle)
						{
							idleThread = info;
							break;
						}
					}

					if (idleThread == null)
						break;

					++m_FileIdx;
				}
			}

			if (isOver)
			{
				CallEndEvent();
			}
		}

		public Action OnFinished
		{
			get;
			set;
		}

		public Action OnError
		{
			get;
			set;
		}

		private void RefreshDownBytes()
		{
			if (m_Data == null || m_Data.Length <= 0)
			{
				AutoUpdateMgr.Instance.CurDownM = 0;
				return;
			}

			double downed = 0f;
			for (int i = 0; i < m_Data.Length; ++i)
			{
				AutoUpdateCfgItem info = m_Data[i];
				if (info.readBytes > 0)
					downed += ((double)info.readBytes)/((double)(1024 * 1024));
			}

			AutoUpdateMgr.Instance.CurDownM = downed;
		}

		protected int FileIdx
		{
			get
			{
				lock (m_Lock)
				{
					return m_FileIdx;
				}
			}

			set
			{
				lock (m_Lock)
				{
					m_FileIdx = value;	
				}
			}
		}

		protected bool HasDownError
		{
			get
			{
				lock (m_Lock)
				{
					return m_HasDownError;
				}
			}

			set 
			{
				lock (m_Lock)
				{
					m_HasDownError = value;
				}
			}
		}

		private AutoUpdateCfgItem[] m_Data = null;
		private FileThreadInfo[] m_Clients = null;
		private System.Object m_Lock = new object();
		private int m_FileIdx = -1;
		private bool m_HasDownError = false;
	}
}
