

using System;
using System.Collections;
using System.Collections.Generic;
using NsHttpClient;

namespace AutoUpdate
{
	public class AutoUpdateFileDownloadState: AutoUpdateBaseState
	{
		protected bool IsMultThreadMode
		{
			get
			{
				return AutoUpdateMgr.Instance.ThreadCount > 1;
			}
		}

		private void Reset()
		{
			if (!IsMultThreadMode)
			{
				m_Curr = -1;
			} else
			{
				if (m_ThreadClients != null)
				{
					m_ThreadClients.Clear();
					m_ThreadClients = null;
				}
			}
			AutoUpdateMgr.Instance.DownProcess = 0;
		}

		public override void Enter(AutoUpdateMgr target)
		{
			Reset();
			var f = target.LocalUpdateFile;
			if (f.Count <= 0)
			{
				ToNextState();
				return;
			}

			m_Items = f.ToArray();
			if (m_Items == null || m_Items.Length <= 0)
			{
				ToNextState();
				return;
			}

			if (!IsMultThreadMode)
			{
				m_Curr = 0;
				StartCurrDownload();
			} else
			{
				m_ThreadClients = new HttpClientThreadFileStream(m_Items, AutoUpdateMgr.Instance.ThreadCount);
				m_ThreadClients.OnError = OnMultThreadDownloadError;
				m_ThreadClients.OnFinished = OnMultThreadDownloadFinish;
				m_ThreadClients.Start();
			}
		}
			
		void OnHttpRead(HttpClientResponse response, long totalRead)
		{
			AutoUpdateCfgItem item = m_Items[m_Curr];

			long delta = totalRead - response.ReadBytes;
			if (delta > 0)
			{
				double curM = AutoUpdateMgr.Instance.CurDownM;
				curM += ((double)delta)/((double)1024 * 1024);
				AutoUpdateMgr.Instance.CurDownM = curM;
				item.readBytes += delta;
			}

			//item.readBytes = totalRead;
			if (totalRead >= response.MaxReadBytes)
			{
				item.isDone = true;
			}

			m_Items[m_Curr] = item;
			AutoUpdateMgr.Instance.DownloadUpdateToUpdateTxt(item);

            /*
			float currProcess = 0;
			if (response.MaxReadBytes > 0)
				currProcess = (float)totalRead/(float)response.MaxReadBytes;
			CalcDownProcess(currProcess);
			AutoUpdateMgr.Instance.DownProcess = currProcess;*/
            double curM = AutoUpdateMgr.Instance.CurDownM;
            double maxM = AutoUpdateMgr.Instance.TotalDownM;
            if (maxM <= float.Epsilon)
                AutoUpdateMgr.Instance.DownProcess = 0;
            else {
                AutoUpdateMgr.Instance.DownProcess = (float)(curM / maxM);
            }

            if (totalRead >= response.MaxReadBytes)
				StartNextDownload();
		}

        /*
		void CalcDownProcess(float currProcess)
		{
			if (m_Items == null || m_Items.Length <= 0)
			{
				AutoUpdateMgr.Instance.DownProcess = 0;
				return;
			}

			float maxProcess = m_Items.Length;
		
			float process = m_Curr + currProcess;
			AutoUpdateMgr.Instance.DownProcess = process/maxProcess;
		}*/

		void DebugFileError()
		{
			AutoUpdateCfgItem item = m_Items[m_Curr];
			UnityEngine.Debug.LogErrorFormat("[downloadFileErr]{0} download: {1:D} isOk: {2}", item.fileContentMd5,
			                                 item.readBytes, item.isDone.ToString());
		}

		void OnHttpError(HttpClientResponse response, int status)
		{
			DebugFileError();
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_FileDown, status);

			// StartNextDownload();
		}

		void StartNextDownload()
		{
			if (m_Curr + 1 >= m_Items.Length)
			{
				ToNextState();
				return;
			}
			++m_Curr;
			StartCurrDownload();
		}
		
		void StartCurrDownload()
		{
			if (m_Curr < 0 || m_Items == null)
				return;

			AutoUpdateCfgItem item;
			while (true)
			{
				if (m_Curr >= m_Items.Length)
				{
					ToNextState();
					return;
				}

				item = m_Items[m_Curr];
				if (item.isDone)
				{
					// 写回到下载
					double m1 = ((double)item.readBytes)/((double)(1024 * 1024));
					double curM1 = AutoUpdateMgr.Instance.CurDownM;
					curM1 += m1;
					AutoUpdateMgr.Instance.CurDownM = curM1;

					++m_Curr;
				}
				else
					break;
			}
			// Get Http
			string url = string.Format("{0}/{1}/{2}", AutoUpdateMgr.Instance.ResServerAddr, 
			                           AutoUpdateMgr.Instance.CurrServeResrVersion,
			                           item.fileContentMd5);

			// 原来下载过直接跳过
			double m = ((double)item.readBytes)/((double)(1024 * 1024));
			double curM = AutoUpdateMgr.Instance.CurDownM;
			curM += m;
			AutoUpdateMgr.Instance.CurDownM = curM;

			AutoUpdateMgr.Instance.CreateHttpFile(url, item.readBytes, OnHttpRead, OnHttpError);
		}

		void OnMultThreadDownloadFinish()
		{
			ToNextState();
		}

		void OnMultThreadDownloadError()
		{
			AutoUpdateMgr.Instance.Error(AutoUpdateErrorType.auError_FileDown, -1);
		}

		void ToNextState()
		{
			AutoUpdateMgr.Instance.ChangeState(AutoUpdateState.auFinished);
		}
			
		private AutoUpdateCfgItem[] m_Items = null;
		private int m_Curr = -1;
		private HttpClientThreadFileStream m_ThreadClients = null;
	}
}
