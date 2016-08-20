using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
	
public interface IDependBinary
{
	string BundleFileName
	{
		get;
	}

	int CompressType
	{
		get;
	}

	bool IsMainAsset
	{
		get;
	}

	bool IsScene
	{
		get;
	}

	int SubFileCount
	{
		get;
	}

	int DependFileCount
	{
		get;
	}

	string GetSubFiles(int index);
	string GetDependFiles(int index);
}

#endif

// 依赖二进制文件格式
public class DependBinaryFile
{

	private static bool WriteInt(Stream stream, int value)
	{
		if (stream == null)
			return false;

		int b1 = value & 0xFF;
		int b2 = (value >> 8) & 0xFF;
		int b3 = (value >> 16) & 0xFF;
		int b4 = (value >> 24) & 0xFF;

		stream.WriteByte ((byte)b1);
		stream.WriteByte ((byte)b2);
		stream.WriteByte ((byte)b3);
		stream.WriteByte ((byte)b4);

		return true;
	}

	private static bool WriteBytes(Stream stream, byte[] bytes)
	{
		if (stream == null)
			return false;

		if (bytes == null || bytes.Length <= 0)
			return WriteInt(stream, 0);
		else if(!WriteInt(stream, bytes.Length))
			return false;
		stream.Write(bytes, 0, bytes.Length);
		return true;
	}

	private static byte[] ReadBytes(Stream stream)
	{
		int cnt = ReadInt (stream);
		if (cnt <= 0)
			return null;
		byte[] ret = new byte[cnt];
		int read = stream.Read (ret, 0, cnt);
		if (read != cnt) {
			byte[] cp = new byte[read];
			Buffer.BlockCopy (ret, 0, cp, 0, read);
			ret = cp;
		}
		return ret;
	}

	private static bool WriteString(Stream stream, string str)
	{
		if (stream == null)
			return false;

		if (string.IsNullOrEmpty(str))
			return WriteInt(stream, 0);

		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
		return WriteBytes(stream, bytes);
	}

	private static int ReadInt(Stream stream)
	{
		int b1 = stream.ReadByte();
		int b2 = stream.ReadByte();
		int b3 = stream.ReadByte ();
		int b4 = stream.ReadByte ();

		int ret = (b4 << 24) | (b3 << 16) | (b2 << 8) | b1;
		return ret;
	}

	private static string ReadString(Stream stream)
	{
		int cnt = ReadInt(stream);
		if (cnt <= 0)
			return string.Empty;

		byte[] bytes = new byte[cnt];
		cnt = stream.Read(bytes, 0, cnt);
		return System.Text.Encoding.UTF8.GetString(bytes, 0, cnt);
	}

	private static bool WriteBool(Stream stream, bool value)
	{
		if (stream == null)
			return false;
		
		int b = value ? 1 : 0;
		stream.WriteByte((byte)b);
		return true;
	}

	private static bool ReadBool(Stream stream)
	{
		int b = stream.ReadByte();
		return b != 0;
	}

	// 文件头
	public struct FileHeader
	{
		// 版本号
		public string version;
		// AB文件数量
		public int abFileCount;
		// 标记
		public int Flag;

		internal void SaveToStream(Stream stream)
		{
			WriteString(stream, version);
			WriteInt(stream, abFileCount);
			WriteInt(stream, Flag);
		}

		public void LoadFromStream(Stream stream)
		{
			version = ReadString (stream);
			abFileCount = ReadInt (stream);
			Flag = ReadInt (stream);
		}
	}

	public struct ABFileHeader
	{
		public int subFileCount;
		public int dependFileCount;
		public bool isScene;
		public bool isMainAsset;
		public int compressType;
		public string abFileName;

		internal void SaveToStream(Stream stream)
		{
			WriteInt(stream, subFileCount);
			WriteInt(stream, dependFileCount);
			WriteBool(stream, isScene);
			WriteBool(stream, isMainAsset);
			WriteInt(stream, compressType);
			WriteString (stream, abFileName);
		}

		public void LoadFromStream(Stream stream)
		{
			subFileCount = ReadInt(stream);
			dependFileCount = ReadInt(stream);
			isScene = ReadBool(stream);
			isMainAsset = ReadBool(stream);
			compressType = ReadInt(stream);
			abFileName = ReadString (stream);
		}
	}

	public struct SubFileInfo
	{
		public string fileName;

		internal void SaveToStream(Stream stream)
		{
			WriteString(stream, fileName);
		}

		public void LoadFromStream(Stream stream)
		{
			fileName = ReadString(stream);
		}
	}

	public struct DependInfo
	{
		public string abFileName;
		public int refCount;

		internal void SaveToStream(Stream stream)
		{
			WriteString(stream, abFileName);
			WriteInt(stream, refCount);
		}

		public void LoadFromStream(Stream stream)
		{
			abFileName = ReadString(stream);
			refCount = ReadInt(stream);
		}
	}

	public static FileHeader LoadFileHeader(Stream stream)
	{
		FileHeader header = new FileHeader ();
		header.LoadFromStream (stream);
		return header;
	}

	public static ABFileHeader LoadABFileHeader(Stream stream)
	{
		ABFileHeader header = new ABFileHeader ();
		header.LoadFromStream (stream);
		return header;
	}

	public static SubFileInfo LoadSubInfo(Stream stream)
	{
		SubFileInfo info = new SubFileInfo ();
		info.LoadFromStream (stream);
		return info;
	}

	public static DependInfo LoadDependInfo(Stream stream)
	{
		DependInfo info = new DependInfo ();
		info.LoadFromStream (stream);
		return info;
	}

	public static bool CheckFileHeader(FileHeader header)
	{
		return string.Compare (header.version, _CurrVersion) == 0;
	}

#if UNITY_EDITOR

	public static void ExportFileHeader(Stream Stream, int abFileCount, int flag)
	{
		FileHeader header = new FileHeader();
		header.version = _CurrVersion;
		header.abFileCount = abFileCount;
		header.Flag = flag;
		header.SaveToStream(Stream);
	}

	public static void ExportToABFileHeader(Stream stream, IDependBinary file, string bundleName)
	{
		ABFileHeader header = new ABFileHeader ();
		header.compressType = file.CompressType;
		header.dependFileCount = file.DependFileCount;
		header.isMainAsset = file.IsMainAsset;
		header.isScene = file.IsScene;
		header.subFileCount = file.SubFileCount;
		header.abFileName = bundleName;
		header.SaveToStream (stream);
	}

	public static void ExportToSubFile(Stream stream, string subFileName)
	{
		SubFileInfo info = new SubFileInfo ();
		info.fileName = subFileName;
		info.SaveToStream (stream);
	}

	public static void ExportToDependFile(Stream stream, string abFileName, int refCount)
	{
		DependInfo info = new DependInfo ();
		info.abFileName = abFileName;
		info.refCount = refCount;
		info.SaveToStream (stream);
	}

#endif

	private static readonly string _CurrVersion = "_D01";
	public static readonly int FLAG_UNCOMPRESS = 0x0;
}