

using System;
using System.Collections;
using System.Collections.Generic;
using NsHttpClient;

namespace AutoUpdate
{
	class DonwloadItem
	{
		public AutoUpdateCfgItem item
		{
			get;
			set;
		}

		public HttpClient http
		{
			get;
			set;
		}

		// 是否可以被使用
		public bool IsCanUse
		{
			get
			{
				return (http == null);
			}
		}

		public void SetNull()
		{
			http = null;
			item = new AutoUpdateCfgItem();
		}

		public void Reset()
		{
			if (http != null)
			{
				http.Dispose();
				http = null;
			}
			item = new AutoUpdateCfgItem();
		}
	}


	public class AutoUpdateFileDownloadState: AutoUpdateBaseState
	{
		private void Reset()
		{
			ClearList();
			AutoUpdateMgr.Instance.DownProcess = 0;
			m_IsError = false;
		}

		// 讲下载文件到等待吗目录
		void DoInit(AutoUpdateMgr target)
		{
			var f = target.LocalUpdateFile;
			if (f.Count <= 0)
			{
				ToNextState();
				return;
			}
			var items = f.ToArray();
			if (items == null || items.Length <= 0)
			{
				ToNextState();
				return;
			}

			if (m_WaitList == null)
				m_WaitList = new LinkedList<AutoUpdateCfgItem>();
			for (int i = 0; i < items.Length; ++i)
			{
				// 判断数量
				var item = items[i];
				if (item.isDone)
				{
					// 写回到下载
					double m1 = ((double)item.readBytes)/((double)(1024 * 1024));
					double curM1 = AutoUpdateMgr.Instance.CurDownM;
					curM1 += m1;
					AutoUpdateMgr.Instance.CurDownM = curM1;
					continue;
				}

				m_WaitList.AddLast(item);
			}

			DoUpdateDownProcess();

			if (m_WaitList.Count <= 0)
			{
				ToNextState();
				return;
			}

			int threadCount = AutoUpdateMgr.Instance.ThreadCount;
			if (threadCount <= 0)
				threadCount = 1;

			if (m_WaitList.Count < threadCount)
				threadCount = m_WaitList.Count;

			if (m_DownList == null)
				m_DownList = new LinkedList<DonwloadItem>();

			// 创建线程对象
			for (int i = 0; i < threadCount; ++i)
			{
				m_DownList.AddLast(new DonwloadItem());
			}
		}

		bool GetNextDownload(out AutoUpdateCfgItem item)
		{
			if (m_WaitList == null || m_WaitList.Count <= 0)
			{
				item = new AutoUpdateCfgItem();
				return false;
			}
			var first = m_WaitList.First;
			if (first == null)
			{
				item = new AutoUpdateCfgItem();
				return false;
			}
			item = first.Value;
			m_WaitList.RemoveFirst();
			return true;
		}

		public override  void Process(AutoUpdateMgr target)
		{
			DoUpdate();
		}

		void DoUpdate()
		{
			if (m_IsError)
				return;

			if (m_DownList == null || m_WaitList == null || m_WaitList.Count <= 0 || m_DownList.Count <= 0)
			{
				return;
			}

			var node = m_DownList.First;
			while (node != null)
			{
				var next = node.Next;
				if (node.Value.IsCanUse)
				{
					AutoUpdateCfgItem item;
					if (GetNextDownload(out item))
					{
						node.Value.item = item;
						// Get Http
						string url = string.Format("{0}/{1}/{2}", AutoUpdateMgr.Instance.ResServerAddr, 
							AutoUpdateMgr.Instance.CurrServeResrVersion,
							item.fileContentMd5);

						// 原来下载过直接跳过
						double m = ((double)item.readBytes)/((double)(1024 * 1024));
						double curM = AutoUpdateMgr.Instance.CurDownM;
						curM += m;
						AutoUpdateMgr.Instance.CurDownM = curM;

						DoUpdateDownProcess();

						//AutoUpdateMgr.Instance.CreateHttpFile(url, item.readBytes, OnHttpRead, OnHttpError);
						int bufSize = AutoUpdateMgr.Instance.HttpFileBufSize;
						string fileName = string.Format("{0}/{1}", AutoUpdateMgr.Instance.WritePath, item.fileContentMd5);
						var listener = new HttpClientFileStream(fileName, item.readBytes, bufSize);
						listener.UserData = node.Value;
						var http = HttpHelper.OpenUrl<HttpClientFileStream>(url, listener, OnItemHttpEnd, OnItemHttpProcess);
						node.Value.http = http;
					} else
					{
						m_DownList.Remove(node);
					}
				}

				node = next;
			}
		}

		bool IsAllHttpDone()
		{
			if (m_WaitList.Count > 0)
				return false;

			bool ret = true;
			if (m_DownList != null)
			{
				var node = m_DownList.First;
				while (node != null)
				{
					var next = node.Next;

					if (node.Value != null && !node.Value.IsCanUse)
					{
						ret = false;
						break;
					}

					node = next;
				}
			}

			return ret;
		}

		void OnItemHttpEnd(HttpClient client, HttpListenerStatus status)
		{
			DonwloadItem downItem = null;

			var listener = client.Listener as HttpClientFileStream;

			if (listener != null && listener.UserData != null)
			{
				downItem = listener.UserData as DonwloadItem;
				listener.UserData = null;
			}

			switch(status)
			{
			case HttpListenerStatus.hsDone:
				{
					if (downItem != null)
						downItem.SetNull();
					if (IsAllHttpDone())
						ToNextState();
					break;
				}
			case HttpListenerStatus.hsError:
				{
					if (downItem != null)
					{
						downItem.SetNull();
						ClearList();

						UnityEngine.Debug.LogErrorFormat("[downloadFileErr]{0} download: {1:D} isOk: {2}", downItem.item.fileContentMd5,
							downItem.item.readBytes, downItem.item.isDone.ToString());
						AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_FileDown, -1);

						m_IsError = true;
					}
					break;
				}
			}
		}

		void OnItemHttpProcess(HttpClient client)
		{
			var listener = client.Listener as HttpClientFileStream;
			if (listener == null)
				return;
			DonwloadItem downItem = listener.UserData as DonwloadItem;
			if (downItem == null)
				return;
			
			var item = downItem.item;
			long oldReadBytes = item.readBytes;
			item.readBytes = listener.StartPos + listener.ReadBytes;
			long delta = item.readBytes - oldReadBytes;
			if (delta > 0) {
				double curM = AutoUpdateMgr.Instance.CurDownM;
				curM += ((double)delta) / ((double)1024 * 1024);
				AutoUpdateMgr.Instance.CurDownM = curM;
			}
			if (listener.ReadBytes >= listener.MaxReadBytes) {
				item.isDone = true;
			}

			downItem.item = item;

			AutoUpdateMgr.Instance.DownloadUpdateToUpdateTxt(item);

			DoUpdateDownProcess();
		}

		public override void Enter(AutoUpdateMgr target)
		{
			target.HttpRelease();
			Reset();
			DoInit(target);
		}

		void DoUpdateDownProcess()
		{
			double cur = AutoUpdateMgr.Instance.CurDownM;
			double max = AutoUpdateMgr.Instance.TotalDownM;
			if (max <= float.Epsilon)
				AutoUpdateMgr.Instance.DownProcess = 0;
			else {
				AutoUpdateMgr.Instance.DownProcess = (float)(cur / max);
			}
		}
			
		void ToNextState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auFinished);
		}

		public override void Exit(AutoUpdateMgr target)
		{
			Reset();
		}

		void ClearList()
		{
			if (m_WaitList != null)
				m_WaitList.Clear();
			if (m_DownList != null)
			{
				var node = m_DownList.First;
				while (node != null)
				{
					var next = node.Next;
					if (node.Value != null)
						node.Value.Reset();
					node = next;
				}
				m_DownList.Clear();
			}
		}

		// 等待下载
		private LinkedList<AutoUpdateCfgItem> m_WaitList = null;
		// 正在下载
		private LinkedList<DonwloadItem> m_DownList = null;
		private bool m_IsError = false;
	}
}
