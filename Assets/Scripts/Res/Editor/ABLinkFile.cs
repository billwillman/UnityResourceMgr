using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Utils;

public class ABLinkFileCfg
{
	public ABLinkFileCfg()
	{}

	public ABLinkFileCfg(string fileName)
	{
		LoadFromFile(fileName);
	}

	// 從配置讀取
	public bool LoadFromFile(string fileName, bool isClear = false)
	{
		if (isClear)
			Clear();	
		if (!File.Exists(fileName))
			return false;

		bool ret = false;
		FileStream stream = new FileStream(fileName, FileMode.Open);
		if (stream.Length > 0)
		{
			byte[] buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			ret = Load(buf);
		}

		stream.Close();
		stream.Dispose();
		return ret;
	}

	private bool Load(Byte[] buf)
	{
		if (buf == null || buf.Length <= 0)
			return false;
		MemoryStream stream = new MemoryStream(buf);

		int itemCnt = FilePathMgr.Instance.ReadInt(stream);
		if (itemCnt <= 0)
			return true;

		for (int i = 0; i < itemCnt; ++i)
		{
			string orgFileName = FilePathMgr.Instance.ReadString(stream);
			string linkFileName = FilePathMgr.Instance.ReadString(stream);
			if (m_GroupDict == null)
				m_GroupDict = new Dictionary<string, string>();
			m_GroupDict.Add(orgFileName, linkFileName);
			AddDstDirCnt(linkFileName);
		}

		stream.Close();
		stream.Dispose();

		return true;
	}

	public void Clear()
	{
		if (m_GroupDict != null)
			m_GroupDict.Clear();
		if (m_DirFileCntMap != null)
			m_DirFileCntMap.Clear();
	}

	public Dictionary<string, string>.Enumerator GetIter()
	{
		if (m_GroupDict == null)
			return new Dictionary<string, string>.Enumerator();
		return m_GroupDict.GetEnumerator();
	}

	public bool ContainsLink(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		if (m_GroupDict == null || m_GroupDict.Count <= 0)
			return false;
		return m_GroupDict.ContainsKey(fileName);
	}

	public string GetRealFileName(string fileName)
	{
		if (m_GroupDict == null || string.IsNullOrEmpty(fileName))
			return string.Empty;
		string ret;
		if (m_GroupDict.TryGetValue(fileName, out ret))
			return ret;
		return string.Empty;
	}

	public bool AddLink(string srcFileName, string dstFileName)
	{
		if (string.IsNullOrEmpty(srcFileName) || string.IsNullOrEmpty(dstFileName))
			return false;

		if (m_GroupDict == null)
			m_GroupDict = new Dictionary<string, string>();
		m_GroupDict.Add(srcFileName, dstFileName);
		AddDstDirCnt(dstFileName);
		return true;
	}



	private void AddDstDirCnt(string dstFileName)
	{
		if (string.IsNullOrEmpty(dstFileName))
			return;
		string dstDir = Path.GetDirectoryName(dstFileName);
		if (string.IsNullOrEmpty(dstDir))
			return;

		if (m_DirFileCntMap == null)
			m_DirFileCntMap = new Dictionary<string, int>();

		if (m_DirFileCntMap.ContainsKey(dstDir))
			m_DirFileCntMap[dstDir] += 1;
		else
			m_DirFileCntMap.Add(dstDir, 1);
	}
		
	public Dictionary<string, int>.Enumerator GetDstDirCntIter()
	{
		if (m_DirFileCntMap == null)
			return new Dictionary<string, int>.Enumerator();
		return m_DirFileCntMap.GetEnumerator();
	}

	public bool GetDstDirCnt(string dstDir, out int cnt)
	{
		cnt = 0;
		if (string.IsNullOrEmpty(dstDir))
			return false;
		if (m_DirFileCntMap == null)
			return false;
		return m_DirFileCntMap.TryGetValue(dstDir, out cnt);
	}

	public bool SaveToFile(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return false;
		
		FileStream stream = new FileStream(fileName, FileMode.Create);
		bool ret = SaveToStream(stream, true);
		stream.Close();
		stream.Dispose();
		return ret;
	}

	public bool SaveToStream(Stream stream, bool isWriteCnt)
	{
		if (stream == null)
			return false;
		if (m_GroupDict == null || m_GroupDict.Count <= 0)
			return false;

		int itemCnt = m_GroupDict.Count;
		if (isWriteCnt)
			FilePathMgr.Instance.WriteInt(stream, itemCnt);
		var iter = m_GroupDict.GetEnumerator();
		while (iter.MoveNext())
		{
			FilePathMgr.Instance.WriteString(stream, iter.Current.Key);
			FilePathMgr.Instance.WriteString(stream, iter.Current.Value);
		}
		iter.Dispose();

		return true;
	}

	public bool Contains(string fileName)
	{
		if (m_GroupDict == null || m_GroupDict.Count <= 0)
			return false;
		return m_GroupDict.ContainsKey(fileName);
	}

	public int LinkCount
	{
		get
		{
			if (m_GroupDict == null)
				return 0;
			return m_GroupDict.Count;
		}
	}
		
	private Dictionary<string, string> m_GroupDict = null;
	private Dictionary<string, int> m_DirFileCntMap = null;
}