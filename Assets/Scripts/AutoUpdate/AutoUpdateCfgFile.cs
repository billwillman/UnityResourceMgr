#define _UpdateCfgBinary

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Utils;

namespace AutoUpdate
{

	public struct AutoUpdateCfgItem
	{
		public string fileContentMd5;
		public long readBytes;
		public bool isDone;

		public static AutoUpdateCfgItem LoadBinary(Stream stream)
		{
			AutoUpdateCfgItem ret = new AutoUpdateCfgItem();
			ret.fileContentMd5 = FilePathMgr.Instance.ReadString(stream);
			ret.readBytes = FilePathMgr.Instance.ReadLong(stream);
			ret.isDone = FilePathMgr.Instance.ReadBool(stream);
			return ret;
		}

		public bool SaveBinary(Stream stream)
		{
			if (stream == null)
				return false;
			bool ret = FilePathMgr.Instance.WriteString(stream, fileContentMd5);
			if (!ret)
				return ret;
			ret = FilePathMgr.Instance.WriteLong(stream, readBytes);
			if (!ret)
				return ret;
			ret = FilePathMgr.Instance.WriteBool(stream, isDone);
			if (!ret)
				return ret;
			return ret;
		}
	}

	// update.txt
	public class AutoUpdateCfgFile
	{
		private string m_SaveFileName = string.Empty;

		public void RemoveAllDowningFiles()
		{
			string writePath = AutoUpdateMgr.Instance.WritePath;
			if (string.IsNullOrEmpty(writePath))
				return;

			Dictionary<string, AutoUpdateCfgItem>.Enumerator iter = m_Dict.GetEnumerator();
			while (iter.MoveNext())
			{
				string fileName = string.Format("{0}/{1}", writePath, iter.Current.Value.fileContentMd5);
				if (File.Exists(fileName))
					File.Delete(fileName);
			}
			iter.Dispose();

			Clear();
		}
			
		public bool RemoveDowningZipFiles(string noDeleteZipFile)
		{
			if (!noDeleteZipFile.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase))
				noDeleteZipFile += ".zip";
			
			string writePath = AutoUpdateMgr.Instance.WritePath;
			if (string.IsNullOrEmpty(writePath))
				return false;

			List<string> delKeyList = null;
			Dictionary<string, AutoUpdateCfgItem>.Enumerator iter = m_Dict.GetEnumerator();
			while (iter.MoveNext())
			{
				if (string.Compare(iter.Current.Value.fileContentMd5, noDeleteZipFile) == 0)
					continue;
				
				string fileName = string.Format("{0}/{1}", writePath, iter.Current.Value.fileContentMd5);
				if (File.Exists(fileName))
					File.Delete(fileName);

				if (delKeyList == null)
					delKeyList = new List<string>();
				delKeyList.Add(iter.Current.Key);
			}
			iter.Dispose();

			bool isChg = false;
			if (delKeyList != null && delKeyList.Count > 0)
			{
				isChg = true;
				for (int i = 0; i < delKeyList.Count; ++i)
				{
					string key = delKeyList[i];
					m_Dict.Remove(key);
				}
			}

			return isChg;
		}

		public bool LoadFromFile(string fileName)
		{
			Clear();
			if (!File.Exists(fileName))
				return false;
			FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			try
			{
				#if _UpdateCfgBinary
				LoadBinary(stream);
				#else
				byte[] src = new byte[stream.Length];
				stream.Read(src, 0, src.Length);
				string str = System.Text.Encoding.ASCII.GetString(src);
				Load (str);
				#endif
			} finally
			{
				stream.Close();
				stream.Dispose();
				stream = null;
			}

			return true;
		}

		public void Load(ResListFile listFile)
		{
			Clear();
			if (listFile == null)
				return;
			var iter = listFile.GetIter();
			while (iter.MoveNext())
			{
				AutoUpdateCfgItem item = new AutoUpdateCfgItem();
				item.fileContentMd5 = iter.Current.Value.fileContentMd5;
				item.isDone = false;
				item.readBytes = 0;
				AddOrSet(item);
			}
			iter.Dispose();
		}

		public void AddOrSet(AutoUpdateCfgItem item)
		{
			if (string.IsNullOrEmpty(item.fileContentMd5))
				return;
			if (m_Dict.ContainsKey(item.fileContentMd5))
				m_Dict[item.fileContentMd5] = item;
			else
				m_Dict.Add(item.fileContentMd5, item);
		}

		public int Count
		{
			get
			{
				return m_Dict.Count;
			}
		}

		public Dictionary<string, AutoUpdateCfgItem>.Enumerator GetIter()
		{
			return m_Dict.GetEnumerator();
		}

		public void LoadBinary(Stream stream)
		{
			if (stream == null)
				return;
			int cnt = FilePathMgr.Instance.ReadInt(stream);
			for (int i = 0; i < cnt; ++i)
			{
				AutoUpdateCfgItem item = AutoUpdateCfgItem.LoadBinary(stream);
				AddOrSet(item);
			}
		}

		public void Load(string str)
		{
			Clear();
			if (string.IsNullOrEmpty(str))
				return;
			char[] splits = new char[1];
			splits[0] = '\n';
			string[] lines = str.Split(splits, StringSplitOptions.RemoveEmptyEntries);
			if (lines == null || lines.Length <= 0)
				return;
			for (int i = 0; i < lines.Length; ++i)
			{
				string line = lines[i].Trim();
				if (string.IsNullOrEmpty(line))
					continue;
				int idx = line.IndexOf('=');
				if (idx < 0)
					continue;
				string name = line.Substring(0, idx).Trim();
				if (string.IsNullOrEmpty(name))
					continue;
				string value = line.Substring(idx + 1).Trim();
				int subIdx = value.IndexOf(';');
				long readBytes;
				bool isDone;
				if (subIdx < 0)
				{
					if (!long.TryParse(value, out readBytes))
						readBytes = 0;
					isDone = false;
				} else
				{
					string rv = value.Substring(0, subIdx).Trim();
					if (!long.TryParse(rv, out readBytes))
						readBytes = 0;
					string bv = value.Substring(subIdx + 1).Trim();
					if (!bool.TryParse(bv, out isDone))
						isDone = false;
				}

				AutoUpdateCfgItem item = new AutoUpdateCfgItem();
				item.fileContentMd5 = name;
				item.readBytes = readBytes;
				item.isDone = isDone;

				AddOrSet(item);
			}
		}

		public bool DownloadUpdate(AutoUpdateCfgItem item)
		{
			if (m_Dict.ContainsKey(item.fileContentMd5))
			{
				m_Dict[item.fileContentMd5] = item;
				return true;
			}

			return false;
		}

		// 有改变返回true
		public bool UpdateToRemoveFiles(ResListFile.ResDiffInfo[] newDiffInfos)
		{
			if (newDiffInfos == null)
				return false;

			if (newDiffInfos.Length <= 0)
			{
				RemoveAllDowningFiles();
				Clear();
				return true;
			}

			bool ret = false;
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = 0; i < newDiffInfos.Length; ++i)
			{
				string contentMd5 = newDiffInfos[i].fileContentMd5;
				if (string.IsNullOrEmpty(contentMd5))
					continue;

				hashSet.Add(contentMd5);
				if (!m_Dict.ContainsKey(contentMd5))
				{
					AutoUpdateCfgItem item = new AutoUpdateCfgItem();
					item.fileContentMd5 = contentMd5;
					item.isDone = false;
					item.readBytes = 0;
					m_Dict.Add(contentMd5, item);
					ret = true;
				}
			}

			List<string> delMd5List = new List<string>();
			string writePath = AutoUpdateMgr.Instance.WritePath;
			if (!string.IsNullOrEmpty(writePath))
			{
				Dictionary<string, AutoUpdateCfgItem>.Enumerator iter = m_Dict.GetEnumerator();
				while (iter.MoveNext())
				{
					string contentMd5 = iter.Current.Value.fileContentMd5;
					if (hashSet.Contains(contentMd5))
						continue;

					delMd5List.Add(contentMd5);
				}
				iter.Dispose();
			}

			if (delMd5List.Count > 0)
			{
				ret = true;
				for (int i = 0; i < delMd5List.Count; ++i)
				{
					string contentMd5 = delMd5List[i];
					m_Dict.Remove(contentMd5);
					string fileName = string.Format("{0}/{1}", writePath, contentMd5);
					if (File.Exists(fileName))
						File.Delete(fileName);
				}
			}

			return ret;
		}

		public string SaveFileName
		{
			get
			{
				return m_SaveFileName;
			}

			set
			{
				m_SaveFileName = value;
			}
		}

		public void Clear()
		{
			m_Dict.Clear();
		}

		public bool FindItem(string fileContentMd5, out AutoUpdateCfgItem item)
		{
			item = new AutoUpdateCfgItem();
			if (string.IsNullOrEmpty(fileContentMd5))
				return false;
			if (!m_Dict.TryGetValue(fileContentMd5, out item))
				return false;
			return true;
		}

		public void SaveToLastFile()
		{
			if (string.IsNullOrEmpty(m_SaveFileName))
				return;
			FileStream stream = new FileStream(m_SaveFileName, FileMode.Create, FileAccess.Write);
			try
			{
				#if _UpdateCfgBinary
				int writeItemCnt = 0;
				Dictionary<string, AutoUpdateCfgItem>.Enumerator iter = m_Dict.GetEnumerator();
				FilePathMgr.Instance.WriteInt(stream, m_Dict.Count);
				while (iter.MoveNext())
				{
					iter.Current.Value.SaveBinary(stream);
					++writeItemCnt;
					if (writeItemCnt > 50)
					{
						writeItemCnt = 0;
						stream.Flush();
					}
				}
				iter.Dispose();
				#else
				int writeBytes = 0;
				Dictionary<string, AutoUpdateCfgItem>.Enumerator iter = m_Dict.GetEnumerator();
				while (iter.MoveNext())
				{
					string s = string.Format("{0}={1:D};{2}\r\n", iter.Current.Value.fileContentMd5,
					                         iter.Current.Value.readBytes, 
					                         iter.Current.Value.isDone.ToString());
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
				#endif
			} finally
			{
				stream.Close();
				stream.Dispose();
				stream = null;
			}
		}

		public AutoUpdateCfgItem[] ToArray()
		{
			List<AutoUpdateCfgItem> itemList = new List<AutoUpdateCfgItem>();
			var iter = m_Dict.GetEnumerator();
			while (iter.MoveNext())
			{
				itemList.Add(iter.Current.Value);
			}
			iter.Dispose();
			return itemList.ToArray();
		}

		private Dictionary<string, AutoUpdateCfgItem> m_Dict = new Dictionary<string, AutoUpdateCfgItem>();
	}
}
