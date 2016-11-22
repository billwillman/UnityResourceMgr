using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ResListFile
{
	public struct ResDiffInfo
	{
		public string fileName;
		public string fileContentMd5;
	}

	public struct ResInfo
	{
		public string fileContentMd5;
        // 文件大小
        public long fileSize;
        public bool isFirstDown;
	}

	public void Load(ResListFile other)
	{
		Clear();
		var iter = other.GetIter();
		while (iter.MoveNext())
		{
			m_FileMd5Map.Add(iter.Current.Key, iter.Current.Value);
			m_ContentMd5ToNameMd5Map.Add(iter.Current.Value.fileContentMd5, iter.Current.Key);
		}
		iter.Dispose();
	}

	public string FindFileNameMd5(string contentMd5)
	{
		string ret;
		if (!m_ContentMd5ToNameMd5Map.TryGetValue(contentMd5, out ret))
			ret = string.Empty;
		return ret;
	}

	public bool LoadFromFile(string fileName)
	{
		Clear();
		if (string.IsNullOrEmpty(fileName))
			return false;
		if (!File.Exists(fileName))
			return false;
		FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
		try
		{
			if (stream.Length > 0)
			{
				byte[] bytes = new byte[stream.Length];
				stream.Read(bytes, 0, bytes.Length);
				string str = System.Text.Encoding.ASCII.GetString(bytes);
				Load(str);
			}
		} finally
		{
			stream.Close();
			stream.Dispose();
			stream = null;
		}
		return true;
	}

	public void Load(string src)
	{
		Clear();
		if (string.IsNullOrEmpty(src))
			return;
		char[] splits = new char[1];
		splits[0] = '\n';
		string[] srcList = src.Split(splits, StringSplitOptions.RemoveEmptyEntries);
		if (srcList == null || srcList.Length <= 0)
			return;
		for (int i = 0; i < srcList.Length; ++i)
		{
			string item = srcList[i].Trim();
			if (string.IsNullOrEmpty(item))
				continue;
			splits[0] = '=';
			string[] keyValue = item.Split(splits, StringSplitOptions.RemoveEmptyEntries);
			if (keyValue == null || keyValue.Length < 2)
				continue;
			string key = keyValue[0].Trim();
			string value = keyValue[1].Trim();
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
				continue;
			if (m_FileMd5Map.ContainsKey(key))
			{
				Debug.LogErrorFormat("【ResListFile Load】File {0} is exists!", key);
				continue;
			}

            string[] values = value.Split(';');
            
			ResInfo info = new ResInfo();
			if (values == null || values.Length <= 1)
			{
				info.fileContentMd5 = value;
				info.isFirstDown = false;
			}
			else
			{
                string s = values[1];
                if (!bool.TryParse(s, out info.isFirstDown))
					info.isFirstDown = false;
                info.fileContentMd5 = values[0];

                if (values.Length > 2) {
                    if (!long.TryParse(values[2], out info.fileSize))
                        info.fileSize = 0;
                }
            }


			m_FileMd5Map.Add(key, info);
			m_ContentMd5ToNameMd5Map.Add(info.fileContentMd5, key);
		}
	}

	public bool AddFile(string key, string contentMd5, bool isFirstDown)
	{
		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(contentMd5))
			return false;
		key = key.Trim();
		contentMd5 = contentMd5.Trim();
		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(contentMd5))
			return false;
		if (m_FileMd5Map.ContainsKey(key))
		{
			ResInfo info = m_FileMd5Map[key];
			m_ContentMd5ToNameMd5Map.Remove(info.fileContentMd5);
			info.fileContentMd5 = contentMd5;
			info.isFirstDown = isFirstDown;
			m_FileMd5Map[key] = info;
			m_ContentMd5ToNameMd5Map.Add(info.fileContentMd5, key);

		} else
		{
			ResInfo info = new ResInfo();
			info.fileContentMd5 = contentMd5;
			info.isFirstDown = isFirstDown;
			m_FileMd5Map.Add(key, info);
			m_ContentMd5ToNameMd5Map.Add(info.fileContentMd5, key);
		}
		return true;
	}

	public static bool SaveToUpdateTxt(ResDiffInfo[] infos, string fileName)
	{
		if (string.IsNullOrEmpty(fileName) || infos == null || infos.Length <= 0)
			return false;

		FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
		try
		{
			int writeBytes = 0;
			for (int i = 0; i < infos.Length; ++i)
			{
				string s = string.Format("{0}=0;false\r\n", infos[i].fileContentMd5);
				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(s);
				if (bytes != null)
				{
					stream.Write(bytes, 0, bytes.Length);
					writeBytes += bytes.Length;
					if (writeBytes > 2048)
					{
						writeBytes = 0;
						stream.Flush();
					}
				}
			}
		} finally
		{
			stream.Close();
			stream.Dispose();
			stream = null;
		}

		return true;
	}

	public bool SaveToUpdateTxt(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
		try
		{
			Dictionary<string, ResInfo>.Enumerator iter = m_FileMd5Map.GetEnumerator();

			int writeBytes = 0;
			while (iter.MoveNext())
			{
				string s = string.Format("{0}=0;false\r\n", iter.Current.Value.fileContentMd5);
				byte[] dst = System.Text.Encoding.ASCII.GetBytes(s);
				stream.Write(dst, 0, dst.Length);
				writeBytes += dst.Length;
				if (writeBytes > 2048)
				{
					writeBytes = 0;
					stream.Flush();
				}
			}

			iter.Dispose();
		} finally
		{
			stream.Close();
			stream.Dispose();
			stream = null;
		}

		return true;
	}

	public bool SaveToFile(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
		try
		{
			Dictionary<string, ResInfo>.Enumerator iter = m_FileMd5Map.GetEnumerator();

			int writeBytes = 0;
			while (iter.MoveNext())
			{
				string keyValue = string.Format("{0}={1};{2}\r\n", iter.Current.Key, 
				                                iter.Current.Value.fileContentMd5,
				                                iter.Current.Value.isFirstDown.ToString());
				byte[] dst = System.Text.Encoding.ASCII.GetBytes(keyValue);
				stream.Write(dst, 0, dst.Length);
				writeBytes += dst.Length;
				if (writeBytes > 2048)
				{
					writeBytes = 0;
					stream.Flush();
				}
			}

			iter.Dispose();
		}
		finally
		{
			stream.Close();
			stream.Dispose();
		}

		return true;
	}

	public void Clear()
	{
		m_FileMd5Map.Clear();
		m_ContentMd5ToNameMd5Map.Clear();
	}

	public Dictionary<string, ResInfo>.Enumerator GetIter()
	{
		return m_FileMd5Map.GetEnumerator();
	}

	public string GetFileContentMd5(string key)
	{
		string ret;
		ResInfo info;
		if (!m_FileMd5Map.TryGetValue(key, out info))
			ret = string.Empty;
		else
			ret = info.fileContentMd5;
		return ret;
	}

	public void WritePathChangeDstDiffFiles(ResDiffInfo[] diffList)
	{
		if (diffList == null || diffList.Length <= 0)
			return;
		string writePath = Utils.FilePathMgr.Instance.WritePath;
		if (string.IsNullOrEmpty(writePath))
			return;

		for (int i = 0; i < diffList.Length; ++i)
		{
			string fileName = string.Format("{0}/{1}", writePath, diffList[i].fileContentMd5);
			string srcFileName = string.Format("{0}/{1}", writePath, diffList[i].fileName);
			if (File.Exists(fileName))
			{
				// change fileName
				File.Move(fileName, srcFileName);
			}
		}
	}

	public void WritePathRemoveSrcDiffFiles(ResDiffInfo[] diffList)
	{
		if (diffList == null || diffList.Length <= 0)
			return;
		string writePath = Utils.FilePathMgr.Instance.WritePath;
		if (string.IsNullOrEmpty(writePath))
			return;

		for (int i = 0; i < diffList.Length; ++i)
		{
			string fileName = string.Format("{0}/{1}", writePath, diffList[i].fileName);
			if (File.Exists(fileName))
				File.Delete(fileName);
		}
	}

	public void DeleteAllFiles()
	{
		string writePath = Utils.FilePathMgr.Instance.WritePath;
		if (string.IsNullOrEmpty(writePath))
			return;
		
		var iter = m_ContentMd5ToNameMd5Map.GetEnumerator();
		while (iter.MoveNext())
		{
			string fileName = string.Format("{0}/{1}", writePath, iter.Current.Key);
			if (File.Exists(fileName))
				File.Delete(fileName);
			fileName = string.Format("{0}/{1}", writePath, iter.Current.Value);
			if (File.Exists(fileName))
				File.Delete(fileName);
		}
		iter.Dispose();
		Clear();
	}

	public ResDiffInfo[] AllToDiffInfos()
	{
		List<ResDiffInfo> list = new List<ResDiffInfo>();
		var iter = m_FileMd5Map.GetEnumerator();
		while (iter.MoveNext())
		{
			ResDiffInfo info = new ResDiffInfo();
			info.fileContentMd5 = iter.Current.Value.fileContentMd5;
			info.fileName = iter.Current.Key;
			list.Add(info);
		}
		iter.Dispose();
		return list.ToArray();
	}

	public ResDiffInfo[] GetDiffInfos(ResListFile otherFile)
	{
		ResDiffInfo[] ret = null;

		if (otherFile != null)
		{
			/*
			string writePath = Utils.FilePathMgr.Instance.WritePath;
			if (string.IsNullOrEmpty(writePath))
				return ret;
			*/

			Dictionary<string, ResInfo>.Enumerator otherIter = otherFile.GetIter();
			List<ResDiffInfo> diffList = new List<ResDiffInfo>();
			while (otherIter.MoveNext())
			{
				bool isDiff = false;
				string srcMd5 = GetFileContentMd5(otherIter.Current.Key);
				if (!string.IsNullOrEmpty(srcMd5))
				{
					if (string.Compare(srcMd5, otherIter.Current.Value.fileContentMd5, StringComparison.CurrentCultureIgnoreCase) != 0)
					{
						isDiff = true;
					} 
					/*
					else
					{
						string fileName = string.Format("{0}/{1}", writePath, otherIter.Current.Key);
						if (!File.Exists(fileName))
						{
							isDiff = true;
						}
					}*/
				} else
					isDiff = true;

				if (isDiff)
				{
					ResDiffInfo diffInfo = new ResDiffInfo();
					diffInfo.fileName = otherIter.Current.Key;
					diffInfo.fileContentMd5 = otherIter.Current.Value.fileContentMd5;
					diffList.Add(diffInfo);
				}
			}
			otherIter.Dispose();
			ret = diffList.ToArray();
		}

		return ret;
	}

	private Dictionary<string, ResInfo> m_FileMd5Map = new Dictionary<string, ResInfo>();
	private Dictionary<string, string> m_ContentMd5ToNameMd5Map = new Dictionary<string, string>();
}

