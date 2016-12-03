using System;
using System.Net;
using System.IO;

namespace NsHttpClient
{
	public class HttpClientStrResponse: HttpClientResponse
	{
		public HttpClientStrResponse(int bufSize = 1024): base(bufSize)
		{}
		
		protected override void Flush(int read)
		{
			string s = System.Text.Encoding.ASCII.GetString(m_Buf, 0, read);
			lock (m_TxtLock)
			{
				m_Txt += s;
			}
		}
		
		public string Txt
		{
			get
			{
				lock (m_TxtLock)
				{
					return m_Txt;
				}
					
			}
		}
		
		private string m_Txt = string.Empty;
		private System.Object  m_TxtLock = new object();
	}
}

