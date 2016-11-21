using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public struct PkgSplitABDirInfo
{
	// 拆分路径
	public string dirPath;
	// 每几个拆分一个AB包
	public int splitCnt;
}

// 编译配置文件
public class BuildPkg
{
	// 读取配置
	public bool LoadFromFile (string fileName)
	{
//		string allPath = Path.GetFullPath (fileName);
		if (!File.Exists (fileName))
			return false;
			
		FileStream stream = new FileStream (fileName, FileMode.Open);

		if (stream.Length > 0) {
			byte[] buf = new byte[stream.Length];
			stream.Read (buf, 0, buf.Length);
			string str = System.Text.Encoding.ASCII.GetString (buf);
			if (!LoadFromString (str))
				return false;
		}

		stream.Close ();
		stream.Dispose ();

		return true;
	}

	private bool LoadFromString (string str)
	{
		Clear ();
		if (string.IsNullOrEmpty (str))
			return false;
		str = str.Trim ();
		if (string.IsNullOrEmpty (str))
			return false;

		// 读取Sections
		m_Copys = LoadSection (str, "Copys");
		m_Svns = LoadSection (str, "SVN");
		m_AssetBundles = LoadSection (str, "AssetBundles");

		// 分离的AB的目录
		string[] splitDirs = LoadSection (str, "SplitABDirs");
		if (splitDirs != null && splitDirs.Length > 0) {
			for (int i = 0; i < splitDirs.Length; ++i) {
				string item = splitDirs [i];
				if (string.IsNullOrEmpty (item))
					continue;
				string[] dirValue = item.Split ('=');
				if (dirValue == null || dirValue.Length < 2)
					continue;
				string dir = dirValue [0].Trim ();
				if (string.IsNullOrEmpty (dir))
					continue;
				int cnt;
				if (!int.TryParse (dirValue [1].Trim (), out cnt) || cnt <= 0)
					continue;
				if (m_SplitABDirList == null)
					m_SplitABDirList = new List<PkgSplitABDirInfo> ();
					
				PkgSplitABDirInfo info = new PkgSplitABDirInfo ();
				info.splitCnt = cnt;
				info.dirPath = dir;
				m_SplitABDirList.Add (info);
			}
		}

		return true;
	}

	private string[] LoadSection (string str, string section)
	{
		if (string.IsNullOrEmpty (str) || string.IsNullOrEmpty (section))
			return null;
		section = string.Format ("[{0}]", section);
		int idx = str.IndexOf (section, StringComparison.CurrentCultureIgnoreCase);
		if (idx < 0)
			return null;
		int startIdx = idx + section.Length;
		int endIdx = startIdx;
		endIdx = str.IndexOf ('[', endIdx);
		string ss;
		if (endIdx < 0)
			ss = str.Substring (startIdx);
		else
			ss = str.Substring (startIdx, endIdx - startIdx);
		ss = ss.Trim ();
		if (string.IsNullOrEmpty (ss))
			return null;

		char[] splitChar = new char[1];
		splitChar [0] = '\n';
		string[] ret = ss.Split (splitChar);

		for (int i = 0; i < ret.Length; ++i) {
			ret [i] = ret [i].Trim ();
		}

		return ret;
	}

	private void Clear ()
	{
		m_Copys = null;
		m_Svns = null;
		m_AssetBundles = null;
		if (m_SplitABDirList != null) {
			m_SplitABDirList.Clear ();
			m_SplitABDirList = null;
		}
	}

	public string[] Copys {
		get {
			return m_Copys;
		}
	}

	public string[] Svns {
		get {
			return m_Svns;
		}
	}

	public string[] AssetBundles {
		get {
			return m_AssetBundles;
		}
	}

	public List<PkgSplitABDirInfo> SplitABDirs {
		get {
			return m_SplitABDirList;
		}
	}

	private string[] m_Copys = null;
	private string[] m_Svns = null;
	private string[] m_AssetBundles = null;
	private List<PkgSplitABDirInfo> m_SplitABDirList = null;
}
