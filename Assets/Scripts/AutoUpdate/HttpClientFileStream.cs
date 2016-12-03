using System;
using System.Net;
using System.IO;

namespace NsHttpClient
{
	public class HttpClientFileStream: HttpClientResponse
	{
		public HttpClientFileStream (string fileName, long process = 0, int bufSize = 1024) : base (bufSize)
		{
			m_WriteFileName = fileName;
			m_Process = process;
			if (m_Process < 0)
				m_Process = 0;
		}

		protected override void DoClose ()
		{
			base.DoClose ();

			lock (m_StreamLock) {
				if (m_Stream != null) {
					m_Stream.Close ();
					m_Stream.Dispose ();
					m_Stream = null;
				}
			}
		}

		protected override void Flush (int read)
		{
			if (read > 0) {
				lock (m_StreamLock) {
					if (m_Stream == null && !string.IsNullOrEmpty (m_WriteFileName)) {
						FileMode mode;
						if (m_Process <= 0)
							mode = FileMode.Create;
						else
							mode = FileMode.OpenOrCreate;
						m_Stream = new FileStream (m_WriteFileName, mode, FileAccess.Write);
						if (m_Process > 0)
							m_Stream.Seek (m_Process, SeekOrigin.Begin);
					}

					if (m_Stream == null)
						return;

					m_Stream.Write (m_Buf, 0, read);
					m_Stream.Flush ();
				}
			}
		}

		private string m_WriteFileName = string.Empty;
		private FileStream m_Stream = null;
		private long m_Process = 0;
		private System.Object m_StreamLock = new object ();
	}
}