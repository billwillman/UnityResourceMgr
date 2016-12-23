using System;
using System.IO;
using UnityEngine;

namespace NsHttpClient
{
	public class HttpClientPicResponse: HttpClientResponse  {

		// 主线程调用
		public HttpClientPicResponse(int width, int height, int bufSize = 1024): base(bufSize)
		{
			m_Width = width;
			m_Height = height;
		}

		// 子线程调用的
		protected override void Flush(int read)
		{
			if (m_Stream == null)
				m_Stream = new MemoryStream();
			m_Stream.Write(m_Buf, 0, read);
		}

		protected override void End()
		{
			byte[] picBuf = null;
			if (m_Stream != null && m_Stream.Length > 0)
			{
				picBuf = m_Stream.ToArray();
			}

			lock (m_Lock)
			{
				m_PicBuf = picBuf;
			}
		}

		// 子线程调用的
		protected override void DoClose()
		{
			base.DoClose();
			if (m_Stream != null)
			{
				m_Stream.Close();
				m_Stream.Dispose();
				m_Stream = null;
			}
		}

		// 主线程调用
		public Texture2D GeneratorTexture()
		{
			lock (m_Lock)
			{
				if (m_Width <= 0 || m_Height <= 0 || m_PicBuf == null || m_PicBuf.Length <= 0)
					return null;

				Texture2D ret = new Texture2D(m_Width, m_Height);
				ret.LoadImage(m_PicBuf);
				return ret;
			}
		}

		private int m_Width = 0;
		private int m_Height = 0;
		private MemoryStream m_Stream;
		private byte[] m_PicBuf = null;
		private System.Object m_Lock = new object();
	}
}
