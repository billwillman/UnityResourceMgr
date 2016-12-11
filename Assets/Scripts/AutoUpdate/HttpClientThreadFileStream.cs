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

	// 外部需要提供的接口
	public interface IHttpClientThreadFileStream
	{
		AutoUpdateCfgItem[] FileList
		{
			get;
		}
	}

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
					return fileIdx < 0;
				}
			}
		}

		public HttpClientThreadFileStream(IHttpClientThreadFileStream data, int maxThreadCnt = 5)
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
						m_Clients[i] = info;
						info.client = null;
						info.fileIdx = -1;
					}
				}
			}
		}

		// 只是开启下载线程
		public bool Start()
		{
			Stop();
			RefreshDownBytes();
			if (m_Clients == null || m_Clients.Length <= 0 || m_Data == null || m_Data.FileList == null || m_Data.FileList.Length <= 0)
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
						if (client.client != null)
						{
							client.client.Dispose();
							client.client = null;
						}

						client.fileIdx = -1;
					}
				}
			}

			FileIdx = -1;
		}

		void OnHttpRead(HttpClientResponse response, long totalRead)
		{
			FileThreadInfo info = response.UserData as FileThreadInfo;
			if (info != null && info.fileIdx >= 0)
			{
				AutoUpdateCfgItem item = m_Data.FileList[info.fileIdx];
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

				m_Data.FileList[info.fileIdx] = item;

				lock (m_Lock)
				{
					AutoUpdateMgr.Instance.DownloadUpdateToUpdateTxt(item);
				}

				if (totalRead >= response.MaxReadBytes)
				{
					lock (m_Lock)
					{
						if (info.client != null)
						{
							info.client.Dispose();
							info.client = null;
						}

						info.fileIdx = -1;
					}

					StartNextDown();
				}
			}
		}

		void OnHttpError(HttpClientResponse response, int status)
		{
			

		}

		// 开始当前位置下载
		private void StartNextDown()
		{
			bool isOver = true;
			lock (m_Lock)
			{
				if (m_Data == null || m_Data.FileList == null || m_Data.FileList.Length <= 0)
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
				while (m_FileIdx < m_Data.FileList.Length)
				{
					var item = m_Data.FileList[m_FileIdx];
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

					++m_FileIdx;
				}
			}

			if (isOver)
			{
				// 告诉下载完毕
			}
		}

		private void RefreshDownBytes()
		{
			if (m_Data == null || m_Data.FileList == null || m_Data.FileList.Length <= 0)
			{
				AutoUpdateMgr.Instance.CurDownM = 0;
				return;
			}

			double downed = 0f;
			for (int i = 0; i < m_Data.FileList.Length; ++i)
			{
				AutoUpdateCfgItem info = m_Data.FileList[i];
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

		private IHttpClientThreadFileStream m_Data = null;
		private FileThreadInfo[] m_Clients = null;
		private System.Object m_Lock = new object();
		private int m_FileIdx = -1;
	}
}
