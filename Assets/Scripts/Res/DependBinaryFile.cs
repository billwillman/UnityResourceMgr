using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Utils;
using FlatBuffers;

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
	// 文件头
	public struct FileHeader
	{
		// 版本号
		public string version;
		// AB文件数量
		public int abFileCount;
		// 标记
		public int Flag;

        public long fileMapCount;
        public long fileMapOffset;

		internal void SaveToStream(Stream stream)
		{
			FilePathMgr.Instance.WriteString(stream, version);
			FilePathMgr.Instance.WriteInt(stream, abFileCount);
			FilePathMgr.Instance.WriteInt(stream, Flag);
            FilePathMgr.Instance.WriteLong (stream, fileMapCount);
            FilePathMgr.Instance.WriteLong (stream, fileMapOffset);
		}

        internal Offset<AssetBundleFlatBuffer.FileHeader> SaveToFlatBuffer(FlatBufferBuilder builder) {
            var versionOffset = builder.CreateString(version);
            AssetBundleFlatBuffer.FileHeader.StartFileHeader(builder);
            AssetBundleFlatBuffer.FileHeader.AddVersion(builder, versionOffset);
            AssetBundleFlatBuffer.FileHeader.AddAbFileCount(builder, abFileCount);
            AssetBundleFlatBuffer.FileHeader.AddFlag(builder, Flag);
            return AssetBundleFlatBuffer.FileHeader.EndFileHeader(builder);
        }

		public void LoadFromStream(Stream stream)
		{
			version = FilePathMgr.Instance.ReadString (stream);
			abFileCount = FilePathMgr.Instance.ReadInt (stream);
			Flag = FilePathMgr.Instance.ReadInt (stream);
            fileMapCount = FilePathMgr.Instance.ReadLong (stream);
            fileMapOffset = FilePathMgr.Instance.ReadLong (stream);
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
			FilePathMgr.Instance.WriteInt(stream, subFileCount);
			FilePathMgr.Instance.WriteInt(stream, dependFileCount);
			FilePathMgr.Instance.WriteBool(stream, isScene);
			FilePathMgr.Instance.WriteBool(stream, isMainAsset);
			FilePathMgr.Instance.WriteInt(stream, compressType);
			FilePathMgr.Instance.WriteString (stream, abFileName);
		}

        internal Offset<AssetBundleFlatBuffer.AssetBundleFileHeader> SaveToFlatBuffer(FlatBufferBuilder builder) {
            var abFileNameOffset = builder.CreateString(abFileName);
            AssetBundleFlatBuffer.AssetBundleFileHeader.StartAssetBundleFileHeader(builder);
            AssetBundleFlatBuffer.AssetBundleFileHeader.AddSubFileCount(builder, subFileCount);
            AssetBundleFlatBuffer.AssetBundleFileHeader.AddDependFileCount(builder, dependFileCount);
            AssetBundleFlatBuffer.AssetBundleFileHeader.AddIsScene(builder, isScene);
            AssetBundleFlatBuffer.AssetBundleFileHeader.AddIsMainAsset(builder, isMainAsset);
            AssetBundleFlatBuffer.AssetBundleFileHeader.AddCompressType(builder, compressType);
            AssetBundleFlatBuffer.AssetBundleFileHeader.AddAbFileName(builder, abFileNameOffset);
            return AssetBundleFlatBuffer.AssetBundleFileHeader.EndAssetBundleFileHeader(builder);
        }

		public void LoadFromStream(Stream stream)
		{
			subFileCount = FilePathMgr.Instance.ReadInt(stream);
			dependFileCount = FilePathMgr.Instance.ReadInt(stream);
			isScene = FilePathMgr.Instance.ReadBool(stream);
			isMainAsset = FilePathMgr.Instance.ReadBool(stream);
			compressType = FilePathMgr.Instance.ReadInt(stream);
			abFileName = FilePathMgr.Instance.ReadString (stream);
		}
	}

	public struct SubFileInfo
	{
		public string fileName;
        // shader名字(只有资源是Shader才有)
        public string shaderName;

		internal void SaveToStream(Stream stream)
		{
			FilePathMgr.Instance.WriteString(stream, fileName);
            FilePathMgr.Instance.WriteString(stream, shaderName);
		}

        internal Offset<AssetBundleFlatBuffer.SubFileInfo> SaveToFlatBuffer(FlatBufferBuilder builder) {
            var fileNameOffset = builder.CreateString(fileName);
            var shaderNameOffset = builder.CreateString(shaderName);
            AssetBundleFlatBuffer.SubFileInfo.StartSubFileInfo(builder);
            AssetBundleFlatBuffer.SubFileInfo.AddFileName(builder, fileNameOffset);
            AssetBundleFlatBuffer.SubFileInfo.AddShaderName(builder, shaderNameOffset);
            return AssetBundleFlatBuffer.SubFileInfo.EndSubFileInfo(builder);
        }

		public void LoadFromStream(Stream stream)
		{
			fileName = FilePathMgr.Instance.ReadString(stream);
            shaderName = FilePathMgr.Instance.ReadString(stream);
		}
	}

	public struct DependInfo
	{
		public string abFileName;
		public int refCount;

		internal void SaveToStream(Stream stream)
		{
			FilePathMgr.Instance.WriteString(stream, abFileName);
			FilePathMgr.Instance.WriteInt(stream, refCount);
		}

        internal Offset<AssetBundleFlatBuffer.DependInfo> SavetoFlatBuffer(FlatBufferBuilder builder) {
            var abFileNameOffset = builder.CreateString(abFileName);
            AssetBundleFlatBuffer.DependInfo.StartDependInfo(builder);
            AssetBundleFlatBuffer.DependInfo.AddAbFileName(builder, abFileNameOffset);
            AssetBundleFlatBuffer.DependInfo.AddRefCount(builder, refCount);
            return AssetBundleFlatBuffer.DependInfo.EndDependInfo(builder);
        }

        public void LoadFromStream(Stream stream)
		{
			abFileName = FilePathMgr.Instance.ReadString(stream);
			refCount = FilePathMgr.Instance.ReadInt(stream);
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

    public static void ExportFileHeader(Stream Stream, int abFileCount, int flag, long fileMapOffset = 0, long fileMapCount = 0)
	{
		FileHeader header = new FileHeader();
		header.version = _CurrVersion;
		header.abFileCount = abFileCount;
		header.Flag = flag;
        header.fileMapOffset = fileMapOffset;
        header.fileMapCount = fileMapCount;
		header.SaveToStream(Stream);
	}

    public static Offset<AssetBundleFlatBuffer.FileHeader> ExportFileHeader(FlatBufferBuilder builder, int abFileCount) {
        FileHeader header = new FileHeader();
        header.version = _CurrVersion;
        header.abFileCount = abFileCount;
        header.Flag = DependBinaryFile.FLAG_FLATBUFFER;
        var offset = header.SaveToFlatBuffer(builder);
        return offset;
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

    public static Offset<AssetBundleFlatBuffer.AssetBundleFileHeader> ExportToABFileHeader(FlatBufferBuilder builder, IDependBinary file, string bundleName) {
        ABFileHeader header = new ABFileHeader();
        header.compressType = file.CompressType;
        header.dependFileCount = file.DependFileCount;
        header.isMainAsset = file.IsMainAsset;
        header.isScene = file.IsScene;
        header.subFileCount = file.SubFileCount;
        header.abFileName = bundleName;
        var offset = header.SaveToFlatBuffer(builder);
        return offset;
    }

	public static void ExportToSubFile(Stream stream, string subFileName, string shaderName = "")
	{
		SubFileInfo info = new SubFileInfo ();
		info.fileName = subFileName;
        info.shaderName = shaderName;
        info.SaveToStream (stream);
	}

    public static Offset<AssetBundleFlatBuffer.SubFileInfo> ExportToSubFile(FlatBufferBuilder builder, string subFileName, string shaderName = "") {
        SubFileInfo info = new SubFileInfo();
        info.fileName = subFileName;
        info.shaderName = shaderName;
        var offset = info.SaveToFlatBuffer(builder);
        return offset;
    }

	public static void ExportToDependFile(Stream stream, string abFileName, int refCount)
	{
		DependInfo info = new DependInfo ();
		info.abFileName = abFileName;
		info.refCount = refCount;
		info.SaveToStream (stream);
	}

    public static Offset<AssetBundleFlatBuffer.DependInfo> ExportToDependFile(FlatBufferBuilder builder, string abFileName, int refCount) {
        DependInfo info = new DependInfo();
        info.abFileName = abFileName;
        info.refCount = refCount;
        return info.SavetoFlatBuffer(builder);
    }

#endif

	private static readonly string _CurrVersion = "_D01";
	public static readonly int FLAG_UNCOMPRESS = 0x0;
    public static readonly int FLAG_FLATBUFFER = 0x1;
}