/*----------------------------------------------------------------
// 模块名：AssetBundle 打包功能
// 创建者：zengyi
// 修改者列表：
// 创建日期：2015年9月29日
// 模块描述：
 *          5.x的打包方式，BuildPipeline.BuildAssetBundles打包
 *          5.x打包需要把脚本给出，而4.6.x不需要
//----------------------------------------------------------------*/

#define ASSETBUNDLE_ONLYRESOURCES
#define USE_UNITY5_X_BUILD
#define USE_HAS_EXT
#define USE_DEP_BINARY
#define USE_DEP_BINARY_AB
//#define USE_ZIPVER

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using AutoUpdate;
#if USE_FLATBUFFER
using FlatBuffers;
#endif
using Utils;
using System.Linq;
using System.Text.RegularExpressions;

public enum eBuildPlatform
{
	eBuildWindow = 0,
	eBuildMac,
	eBuildIOS,
	eBuildAndroid
}

// AssetBundle 文件打包类型
enum AssetBundleFileType
{
	abError = 0,// 错误
	abMainFile, // 一个文件一个打包(单文件模式)
	abDirFiles  // 一个目录所有文件一个打包(目录文件模式---多文件模式)
}

// AB信息
class AssetBunbleInfo: IDependBinary
{
    public struct DependFileInfo
    {
        public string fileName;
    }

	// 获得根据Assets目录的局部目录
	public static string GetLocalPath(string path)
	{
        return AssetBundleMgr.GetAssetRelativePath(path);
	}

	public bool IsBuilded {
		get;
		set;
	}

    public long FileOffset {
        get;
        set;
    }

	public int CompressType {
		get;
		set;
	}

	public bool IsMainAsset {
	
		get {
			return (FileType == AssetBundleFileType.abMainFile) || 
				((FileType == AssetBundleFileType.abDirFiles) && (SubFileCount == 1));
		}
	}


	public string BundleFileName {
		get {
			if (FileType == AssetBundleFileType.abError)
				return string.Empty;
			string ret = GetBundleFileName(Path, false, true);
			return ret;
		}
	}

	public string Md5BundleFileName(string outPath, bool isOnlyFileName = true)
    {
//		get
		{
			string fileName = BundleFileName;
			if (string.IsNullOrEmpty(fileName))
				return string.Empty;
			fileName = outPath + '/' + fileName;
			string ret = Md5(fileName, isOnlyFileName);
            if (string.IsNullOrEmpty(ret) && !isOnlyFileName) {
                // 原始文件名文件找不到再找一次MD5文件名，修正增量AB时候的问题
                string fileNameMd5 = Md5 (fileName, true);
                fileName = outPath + '/' + fileNameMd5;
                ret = Md5 (fileName, false);
            }
			return ret;
		}
    }

#if USE_UNITY5_X_BUILD
	public void RebuildDependFiles(AssetBundleManifest manifest)
	{
		if (manifest == null)
			return;
		string[] directDepnendFileNames = manifest.GetDirectDependencies(this.BundleFileName);
		List<DependFileInfo> list = DependABFileNameList;
		list.Clear();
		for (int i = 0; i < directDepnendFileNames.Length; ++i)
		{
			string fileName = System.IO.Path.GetFileNameWithoutExtension(directDepnendFileNames[i]);
			if (!string.IsNullOrEmpty(fileName))
			{
				DependFileInfo info = new DependFileInfo();
				info.fileName = fileName;
				list.Add(info);
			}
		}
	}
#endif

	public static string GetBundleFileName(string path, bool removeAssets, bool doReplace)
	{
		if (string.IsNullOrEmpty(path))
			return string.Empty;

		path = path.ToLower();
		// delete "Assets/"
		string localPath;

		if (removeAssets) {
			string startStr = "assets/";
			if (path.StartsWith (startStr))
				localPath = path.Substring (startStr.Length);
			else
				localPath = path;
		} else
			localPath = path;
		
		if (doReplace)
			localPath = localPath.Replace ('/', '$') + ".assets";
		//localPath = localPath.ToLower();
		return localPath;
	}

    private static MD5 m_Md5 = new MD5CryptoServiceProvider();
	// filePath Md5
	private static Dictionary<string, string> m_Md5FileMap = new Dictionary<string, string>();
	private static Dictionary<string, string> m_Md5FileMap2 = new Dictionary<string, string>();
	public static void ClearMd5FileMap()
	{
		m_Md5FileMap.Clear();
		m_Md5FileMap2.Clear();
	}
    // 返回文件名的MD5
    internal static string Md5(string filePath, bool isOnlyUseFileName = true)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        if (!isOnlyUseFileName && !File.Exists(filePath))
			return string.Empty;

		string ret;
		if(isOnlyUseFileName)
		{
			if (m_Md5FileMap.TryGetValue(filePath, out ret))
				return ret;
		} else
		{
			if (m_Md5FileMap2.TryGetValue(filePath, out ret))
				return ret;
		}

		ret = string.Empty;

		if (isOnlyUseFileName)
		{
			string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
			byte[] src = System.Text.Encoding.ASCII.GetBytes(fileName);
			byte[] hash = m_Md5.ComputeHash(src);
			for (int i = 0; i < hash.Length; i++)
			{
				ret += hash[i].ToString("X").PadLeft(2, '0');  
			}

			ret = ret.ToLower();
			
			m_Md5FileMap.Add(filePath, ret);
		} else
		{
			FileStream stream = new FileStream(filePath, FileMode.Open); 
			try
			{
				if (stream.Length <= 0)
					return string.Empty;
				byte[] src = new byte[stream.Length];
				stream.Seek(0, SeekOrigin.Begin);
				stream.Read(src, 0, src.Length);
        		byte[] hash = m_Md5.ComputeHash(src);
      			//  m_Md5.Clear();

        		for (int i = 0; i < hash.Length; i++)
        		{
            		ret += hash[i].ToString("X").PadLeft(2, '0');  
        		}

        		ret = ret.ToLower();

				m_Md5FileMap2.Add(filePath, ret);
			}
			finally
			{
				stream.Close();
				stream.Dispose();
			}
		}

        return ret;
    }

	public AssetBunbleInfo(string fullPath, string[] fileNames, bool isManualDepend = false)
	{
		Path = GetLocalPath(fullPath);
		if (fileNames == null || fileNames.Length <= 0 || string.IsNullOrEmpty (Path))
		{
			FileType = AssetBundleFileType.abError;
			FullPath = string.Empty;
			return;
		}

		FileType = AssetBundleFileType.abDirFiles;
		Path = Path.ToLower();
		FullPath = fullPath.ToLower();

		List<string> fileList = this.FileList;
		for (int i = 0; i < fileNames.Length; ++i)
		{
			string fileName = fileNames[i];
			if (!AssetBundleBuild.FileIsResource(fileName))
				continue;
			fileName = GetLocalPath(fileName);
			if (string.IsNullOrEmpty(fileName))
				continue;
			fileList.Add(fileName.ToLower());
		}

		if (isManualDepend)
			BuildDepends();
		
		CheckIsScene();

	//	Set_5_x_AssetBundleNames();

		IsBuilded = false;
	}

	public AssetBunbleInfo(string fullPath, bool isManualDepend = false)
	{
		Path = GetLocalPath(fullPath);

		if (string.IsNullOrEmpty (Path)) {
			FileType = AssetBundleFileType.abError;
			FullPath = string.Empty;
		}
		else {
			if (System.IO.Directory.Exists(fullPath))
				FileType = AssetBundleFileType.abDirFiles;
			else
			if (System.IO.File.Exists(fullPath))
				FileType = AssetBundleFileType.abMainFile;
			Path = Path.ToLower();
			FullPath = fullPath.ToLower();
		}

		BuildDirFiles ();

		if (isManualDepend)
			BuildDepends ();

		CheckIsScene ();

    //    Set_5_x_AssetBundleNames();

		IsBuilded = false;


	}

	private void CheckIsScene()
	{
		if (FileType == AssetBundleFileType.abError) {
			IsScene = false;
			return;
		}

		/*
		if (FileType == AssetBundleFileType.abMainFile) {
			// 判断是否是场景
			string ext = System.IO.Path.GetExtension (FullPath);
			IsScene = (string.Compare (ext, ".unity", true) == 0);
			return;
		}*/

		bool isSceneFiles = false;
		bool isRemoveScene = false;
		for (int i = 0; i < SubFileCount; ++i)
		{
			string fileName = GetSubFiles(i);
			string ext = System.IO.Path.GetExtension(fileName);
			bool b = (string.Compare (ext, ".unity", true) == 0);
			if ((i > 0) && (isSceneFiles != b))
			{
				// string errStr = string.Format("AssetBundle [{0}] don't has Scene and other type files", Path);
				// Debug.LogError(errStr);
				// FileType = AssetBundleFileType.abError;
				// return;
				isRemoveScene = true;
			}

			isSceneFiles = b;
		}

		if (isRemoveScene) {
			string errStr = string.Format("AssetBundle [{0}] don't has Scene and other type files(so remove Scene)", Path);
			Debug.LogWarning(errStr);

			isSceneFiles = false;
			if (mFileList != null)
			{
				List<string> fileList = new List<string>();
				for (int i = 0; i < mFileList.Count; ++i)
				{
					string fileName = mFileList[i];
					string ext = System.IO.Path.GetExtension(fileName);
					bool b = (string.Compare (ext, ".unity", true) == 0);
					if (!b)
						fileList.Add(fileName);
				}

				mFileList.Clear();
				mFileList.AddRange(fileList);
			}
		}

		IsScene = isSceneFiles;
	}

    // 获得ShaderName名字
    private string GetShaderName(string fileName) {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;
        string extName = System.IO.Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extName))
            return string.Empty;
        string ret = string.Empty;
        if (string.Compare(extName, ".shader", true) == 0) {
            fileName = "Assets/" + fileName;
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(fileName);
            if (shader != null) {
                ret = shader.name;
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }

        return ret;
    }

    public static string GetSubFileNameFormat(string subFileName)
    {
        string fileName = GetBundleFileName (subFileName, true, false);
        if (string.IsNullOrEmpty (fileName))
            return string.Empty;
        string resFileName = AssetBundleBuild.GetXmlFileName(fileName);
        if (string.IsNullOrEmpty (resFileName))
            return string.Empty;
        return resFileName;
    }

    public void ExportBinarySubFilesAndDependFiles(Stream stream, bool isMd5, string outPath) {
        if ((stream == null) || (FileType == AssetBundleFileType.abError))
            return;

        if (SubFileCount > 0) {
            for (int i = 0; i < SubFileCount; ++i) {
                string resFileName = GetSubFileNameFormat(GetSubFiles(i));
                if (string.IsNullOrEmpty (resFileName))
                    continue;
                string shaderName = GetShaderName(resFileName);
                DependBinaryFile.ExportToSubFile(stream, resFileName, shaderName);
            }
        }

        if (DependFileCount > 0) {
            for (int i = 0; i < DependFileCount; ++i) {
                string fileName = GetBundleFileName(GetDependFiles(i), false, true);
                if (string.IsNullOrEmpty(fileName))
                    continue;
                int depCnt = AssetBundleRefHelper.GetAssetBundleRefCount(this, fileName);
                if (isMd5) {
                    string filePath = outPath + '/' + fileName;
                    fileName = AssetBunbleInfo.Md5(filePath);
                }

                if (depCnt <= 0)
                    depCnt = 1;

                DependBinaryFile.ExportToDependFile(stream, fileName, depCnt);
            }
        }
    }

    public void ExportFileHeader(Stream stream, bool isMd5, string outPath) {
        if ((stream == null) || (FileType == AssetBundleFileType.abError))
            return;

        string bundleFileName;
        if (isMd5)
            bundleFileName = this.Md5BundleFileName(outPath);
        else
            bundleFileName = this.BundleFileName;
        if (string.IsNullOrEmpty(bundleFileName))
            return;

        DependBinaryFile.ExportToABFileHeader(stream, this, bundleFileName);
    }

	public void ExportBinary(Stream stream, bool isMd5, string outPath)
	{
        FileOffset = stream.Position;
        ExportFileHeader(stream, isMd5, outPath);
        ExportBinarySubFilesAndDependFiles(stream, isMd5, outPath);
    }

#if USE_FLATBUFFER
    public Offset<AssetBundleFlatBuffer.AssetBundleInfo> ExportFlatBuffer(FlatBufferBuilder builder, bool isMd5, string outPath) {
        string bundleFileName;
        if (isMd5)
            bundleFileName = this.Md5BundleFileName(outPath);
        else
            bundleFileName = this.BundleFileName;

        
        var abFileHeaderOffset = DependBinaryFile.ExportToABFileHeader(builder, this, bundleFileName);
        List<Offset<AssetBundleFlatBuffer.SubFileInfo>> subFileOffsetList = new List<Offset<AssetBundleFlatBuffer.SubFileInfo>>();
        if (SubFileCount > 0) {
            for (int i = 0; i < SubFileCount; ++i) {
                string fileName = GetBundleFileName(GetSubFiles(i), true, false);
                if (string.IsNullOrEmpty(fileName))
                    continue;
                string resFileName = AssetBundleBuild.GetXmlFileName(fileName);
                if (string.IsNullOrEmpty(resFileName))
                    continue;
                string shaderName = GetShaderName(resFileName);
                var offset = DependBinaryFile.ExportToSubFile(builder, resFileName, shaderName);
                subFileOffsetList.Add(offset);
            }
        }
        VectorOffset subFileVectorOffset = AssetBundleFlatBuffer.AssetBundleInfo.CreateSubFilesVector(builder, subFileOffsetList.ToArray());
        

        List<Offset<AssetBundleFlatBuffer.DependInfo>> dependInfoOffsetList = new List<Offset<AssetBundleFlatBuffer.DependInfo>>();
        if (DependFileCount > 0) {
            for (int i = 0; i < DependFileCount; ++i) {
                string fileName = GetBundleFileName(GetDependFiles(i), false, true);
                if (string.IsNullOrEmpty(fileName))
                    continue;
                int depCnt = AssetBundleRefHelper.GetAssetBundleRefCount(this, fileName);
                if (isMd5) {
                    string filePath = outPath + '/' + fileName;
                    fileName = AssetBunbleInfo.Md5(filePath);
                }

                if (depCnt <= 0)
                    depCnt = 1;

                var offset = DependBinaryFile.ExportToDependFile(builder, fileName, depCnt);
                dependInfoOffsetList.Add(offset);
            }
        }
        var dependInfoOffsetVector = AssetBundleFlatBuffer.AssetBundleInfo.CreateDependFilesVector(builder, dependInfoOffsetList.ToArray());

        AssetBundleFlatBuffer.AssetBundleInfo.StartAssetBundleInfo(builder);
        AssetBundleFlatBuffer.AssetBundleInfo.AddFileHeader(builder, abFileHeaderOffset);
        AssetBundleFlatBuffer.AssetBundleInfo.AddSubFiles(builder, subFileVectorOffset);
        AssetBundleFlatBuffer.AssetBundleInfo.AddDependFiles(builder, dependInfoOffsetVector);
        return AssetBundleFlatBuffer.AssetBundleInfo.EndAssetBundleInfo(builder);
    }
#endif

//	private static readonly bool _cIsOnlyFileNameMd5 = true;
	public void ExportXml(StringBuilder builder, bool isMd5, string outPath)
	{
		if ((builder == null) || (FileType == AssetBundleFileType.abError))
			return;

		string bundleFileName;
        if (isMd5)
			bundleFileName = this.Md5BundleFileName(outPath);
        else
            bundleFileName = this.BundleFileName;
		if (string.IsNullOrEmpty (bundleFileName))
			return;

		builder.AppendFormat("\t<AssetBundle fileName=\"{0}\" isScene=\"{1}\" isMainAsset=\"{2}\" compressType=\"{3}\">", 
		                     bundleFileName, System.Convert.ToString(IsScene), 
		                     System.Convert.ToString(IsMainAsset), System.Convert.ToString(CompressType));
		builder.AppendLine();

		if (SubFileCount > 0)
		{
			builder.Append("\t\t<SubFiles>");
			builder.AppendLine();
			for (int i = 0; i < SubFileCount; ++i) {
				string fileName = GetBundleFileName(GetSubFiles(i), true, false);
				if (string.IsNullOrEmpty(fileName))
					continue;
				//string name = System.IO.Path.ChangeExtension(fileName, "");
				//string ext = System.IO.Path.GetExtension(fileName);
				string resFileName = AssetBundleBuild.GetXmlFileName(fileName);
				if (string.IsNullOrEmpty(resFileName))
					continue;
                // builder.AppendFormat("\t\t\t<SubFile fileName=\"{0}\" hashCode=\"{1}\"/>", resFileName, System.Convert.ToString(Animator.StringToHash(resFileName)));

                // 判断如果 
                string shaderName = GetShaderName(resFileName);
                if (string.IsNullOrEmpty(shaderName))
                    builder.AppendFormat("\t\t\t<SubFile fileName=\"{0}\"/>", resFileName);
                else
                    builder.AppendFormat("\t\t\t<SubFile fileName=\"{0}\" shaderName=\"{1}\"/>", resFileName, shaderName);
				builder.AppendLine();
			}

			builder.Append("\t\t</SubFiles>");
			builder.AppendLine();
		}

		if (DependFileCount > 0) {
			builder.Append("\t\t<DependFiles>");
			builder.AppendLine();

			for (int i = 0; i < DependFileCount; ++i)
			{
				string fileName = GetBundleFileName(GetDependFiles(i), false, true);
				if (string.IsNullOrEmpty(fileName))
					continue;
				int depCnt = AssetBundleRefHelper.GetAssetBundleRefCount(this, fileName);
                if (isMd5)
				{
					string filePath = outPath + '/' + fileName;
					fileName = AssetBunbleInfo.Md5(filePath);
				}

                if (string.IsNullOrEmpty(fileName))
                    continue;

				if (depCnt > 1)
					builder.AppendFormat("\t\t\t<DependFile fileName=\"{0}\" refCount=\"{1}\"/>", fileName, depCnt);
				else
					builder.AppendFormat("\t\t\t<DependFile fileName=\"{0}\" />", fileName);
				builder.AppendLine();
			}

			builder.Append("\t\t</DependFiles>");
			builder.AppendLine();
		}

		builder.Append ("\t</AssetBundle>");
		builder.AppendLine ();
	}

	private void _AddDependHashSet(HashSet<string> hashSet)
	{
		for (int i = 0; i < DependFileCount; ++i) {
			string dependFileName = GetDependFiles(i);
			if (string.IsNullOrEmpty(dependFileName))
				continue;
			AssetBunbleInfo dependInfo = AssetBundleBuild.FindAssetBundle(dependFileName);
			if (dependInfo != null)
			{
				if (!hashSet.Contains(dependFileName))
				{
					hashSet.Add(dependFileName);
					dependInfo._AddDependHashSet(hashSet);
				}
			}
		}
	}

	private int GetAllDependCount()
	{
		if (FileType == AssetBundleFileType.abError)
			return 0;

		HashSet<string> dependSet = new HashSet<string> ();
		for (int i = 0; i < DependFileCount; ++i) {
			string dependFileName = GetDependFiles(i);
			if (string.IsNullOrEmpty(dependFileName))
				continue;
			AssetBunbleInfo dependInfo = AssetBundleBuild.FindAssetBundle(dependFileName);
			if (dependInfo == null)
				continue;
			dependSet.Add(dependFileName);
			dependInfo._AddDependHashSet(dependSet);
		}

		return dependSet.Count;
	}

	public void RefreshAllDependCount()
	{
		AllDependCount = GetAllDependCount ();
	}

	// 文件类型
	public AssetBundleFileType FileType {
		get;
		protected set;
	}

	public string Path {
		get;
		protected set;
	}

	public string FullPath
	{
		get; protected set;
	}

	// 是否是场景
	public bool IsScene
	{
		get;
		protected set;
	}

	public int DependFileCount
	{
		get {
			if (mDependABFileNameList == null)
				return 0;
			return mDependABFileNameList.Count;
		}
	}

	public int AllDependCount {
		get 
		{
			if (mAllDependCount <= 0)
				return DependFileCount;
			return mAllDependCount;
		}
		protected set
		{
			mAllDependCount = value;
		}
	}

	public int SubFileCount
	{
		get {
			if (FileType == AssetBundleFileType.abMainFile)
				return 1;

			if (mFileList == null)
				return 0;
			return mFileList.Count;
		}
	}

	public string GetDependFiles(int index)
	{
		if ((mDependABFileNameList == null) || (index < 0) || (index >= mDependABFileNameList.Count))
			return string.Empty;
		return mDependABFileNameList [index].fileName;
	}

	public string GetSubFiles(int index)
	{
		if (FileType == AssetBundleFileType.abMainFile) {
			if (index == 0)
				return this.Path;
			else
				return string.Empty;
		}

		if ((mFileList == null) || (index < 0) || (index >= mFileList.Count))
			return string.Empty;
		return mFileList [index];
	}

	public string[] GetSubFiles()
	{
		if (FileType == AssetBundleFileType.abMainFile) {
			string[] ret = new string[1];
			ret[0] = this.Path;
			return ret;
		}

		if ((mFileList == null) || (mFileList.Count <= 0))
			return null;
		return mFileList.ToArray ();
	}

	public void Print()
	{
		Debug.Log ("===================================================");
		Debug.Log ("{");
		Debug.Log ("\t[相对目录] " + Path);
		Debug.Log ("\t[绝对目录] " + FullPath);

		Debug.Log ("\t[文件数量] " + System.Convert.ToString (SubFileCount));
		for (int i = 0; i < SubFileCount; ++i) {
			string fileName = GetSubFiles(i);
			Debug.Log("\t\t[子文件名] " + fileName);
		}

		Debug.Log (string.Format("\t[依赖数量: {0}] [总依赖数量: {1}] ", System.Convert.ToString (DependFileCount),
		                         									   System.Convert.ToString (AllDependCount)));
		for (int i = 0; i < DependFileCount; ++i) {
			string depend = GetDependFiles(i);
			Debug.Log("\t\t[依赖名] " + depend);
		}
		Debug.Log ("}");
		Debug.Log ("===================================================");
	}

    public int ScriptFileCount
    {
        get
        {
            if (mScriptFileNameList == null)
                return 0;
            return mScriptFileNameList.Count;
        }
    }

    public string GetScriptFileName(int index)
    {
        if (mScriptFileNameList == null)
            return string.Empty;
        if ((index < 0) || (index >= mScriptFileNameList.Count))
            return string.Empty;
        return mScriptFileNameList[index];
    }

    public string[] GetScriptFileNames()
    {
        if (mScriptFileNameList == null)
            return null;
        return mScriptFileNameList.ToArray();
    }

	// 排序函数
	public static int OnSort(AssetBunbleInfo info1, AssetBunbleInfo info2)
	{
		int dependCnt1 = info1.AllDependCount;
		int dependCnt2 = info2.AllDependCount;

		if ((dependCnt1 == 0) && (dependCnt2 > 0))
			return -1;

		if ((dependCnt2 == 0) && (dependCnt1 > 0))
			return 1;

		if (dependCnt1 < dependCnt2)
			return -1;
		if (dependCnt1 > dependCnt2)
			return 1;
		else
			return 0;
	}

	// 包含的文件
	protected void BuildDirFiles()
	{
		if (FileType != AssetBundleFileType.abDirFiles)
			return;

		string path = FullPath;
		if (string.IsNullOrEmpty (path))
			return;

		string[] files = System.IO.Directory.GetFiles (path);
		if ((files != null) && (files.Length > 0)) {
			for (int i = 0; i < files.Length; ++i)
			{
				string fileName = files[i];
				if (!AssetBundleBuild.FileIsResource(fileName))
					continue;
				string localPath = GetLocalPath(fileName);
				if (!string.IsNullOrEmpty(localPath))
				{
					localPath = localPath.ToLower();
					FileList.Add(localPath);
				}
			}
		}

		// is contain _Dir??
		string[] dirs = System.IO.Directory.GetDirectories (path);

		if (dirs != null)
		{
			for (int i = 0; i < dirs.Length; ++i)
			{
				string dir = dirs[i];
				string local = System.IO.Path.GetFileName(dir);
				if (local.StartsWith(AssetBundleBuild._NotUsed))
				{
					List<string> resFiles = AssetBundleBuild.GetAllSubResFiles(dir);
					if ((resFiles != null) && (resFiles.Count > 0))
					{
						for (int j = 0; j < resFiles.Count; ++j)
						{
							string localPath = GetLocalPath(resFiles[j]);
							localPath = localPath.ToLower();
							FileList.Add(localPath);
						}
					}
				}
			}
		}
	}


	protected bool ExistsFile(string localPath)
	{
		for (int i = 0; i < SubFileCount; ++i) {
			if (string.Compare(GetSubFiles(i), localPath, true) == 0)
				return true;
		}

		return false;
	}

    protected void Set_5_x_AssetBundleNames()
    {
#if USE_UNITY5_X_BUILD
        for (int i = 0; i < SubFileCount; ++i)
        {
            string subFileName = GetSubFiles(i);
            if (!string.IsNullOrEmpty(subFileName))
            {
                AssetImporter importer = AssetImporter.GetAtPath(subFileName);
                if (importer != null)
                {
					//string name = System.IO.Path.GetFileNameWithoutExtension(this.BundleFileName);
					string name = this.BundleFileName;
					if (string.Compare(importer.assetBundleName, name) != 0)
					{
						importer.assetBundleName = name;
						EditorUtility.UnloadUnusedAssetsImmediate();
					//	importer.assetBundleVariant = "assets";
					//	importer.SaveAndReimport();
					}

					AssetBundleBuild.AddShowTagProcess(name);
                }
            }
        }

#endif
    }

	protected void BuildDepends()
	{
		if (FileType == AssetBundleFileType.abError)
			return;

		string[] fileNames = null;
		if (FileType == AssetBundleFileType.abMainFile) {
			// 一个文件模式
			fileNames = new string[1];
			fileNames[0] = Path;
		} else
		if (FileType == AssetBundleFileType.abDirFiles) {
			// 整个目录文件模式
			fileNames = FileList.ToArray();
		}

		if ((fileNames == null) || (fileNames.Length <= 0))
			return;

		string[] dependFiles = AssetDatabase.GetDependencies (fileNames);
		if ((dependFiles != null) && (dependFiles.Length > 0)) {
			for (int i = 0; i < dependFiles.Length; ++i)
			{
				string fileName = dependFiles[i];

                if (AssetBundleBuild.FileIsScript(fileName))
                {
                    // 如果是脚本
                    if (!ScriptFileNameList.Contains(fileName))
                        ScriptFileNameList.Add(fileName);
                    continue;
                }

				if (!AssetBundleBuild.FileIsResource(fileName))
					continue;
#if ASSETBUNDLE_ONLYRESOURCES
                if (AssetBundleBuild.IsOtherResourcesDir(fileName))
                    continue;
#endif
                if (ExistsFile(fileName))
				    continue;

				fileName = GetDependFileName(fileName);
				if (string.IsNullOrEmpty(fileName))
					continue;
				if (!ExistsDepend(fileName))
				{
					DependFileInfo info = new DependFileInfo();
					info.fileName = fileName;
					DependABFileNameList.Add(info);	
				}

			}
		}
	}

	protected string GetDependFileName(string localFileName)
	{
		if (string.IsNullOrEmpty (localFileName))
			return string.Empty;
		string dirName = System.IO.Path.GetDirectoryName(localFileName);
		string searchStr = "/" + AssetBundleBuild._NotUsed;
		int idx = dirName.IndexOf (searchStr);
		bool isNotUsed = (idx >= 0);
		if (isNotUsed) {
			// 判断前面是否有一个@文件夹

			string ret = dirName.Substring(0, idx);
			string preDirName = System.IO.Path.GetFileName(ret);
			bool isOnly = !preDirName.StartsWith(AssetBundleBuild._MainFileSplit);
			if (isOnly)
				return localFileName.ToLower();
			return ret.ToLower();
		}

		string localDirName = System.IO.Path.GetFileName(dirName);
		bool isGroupOnly = !localDirName.StartsWith (AssetBundleBuild._MainFileSplit);
		if (!isGroupOnly)
			return dirName.ToLower(); 
		return localFileName.ToLower();
	}

	/*
	 * 老的规则：@ 表示每个为单独AB
	protected string GetDependFileName(string localFileName)
	{
		if (string.IsNullOrEmpty (localFileName))
			return string.Empty;
		string dirName = System.IO.Path.GetDirectoryName(localFileName);
		string localDirName = System.IO.Path.GetFileName(dirName);
		bool isOnly = localDirName.StartsWith (AssetBundleBuild._MainFileSplit);
		if (isOnly)
			return localFileName;

		// has _ Dir??
		string searchStr = "/" + AssetBundleBuild._NotUsed;
		int idx = dirName.IndexOf(searchStr);
		if (idx >= 0) {
			string ret = dirName.Substring(0, idx);
			// 判断前一个文件夹是否是@文件夹，如果不是，则保持原样
			string preDirName = System.IO.Path.GetFileName(ret);
			bool isPreOnly = preDirName.StartsWith(AssetBundleBuild._MainFileSplit);
			if (isPreOnly)
				return localFileName;
			return ret;
		}

		return dirName;
	}*/


	protected bool ExistsDepend(string localPath)
	{
		if (mDependABFileNameList == null)
			return false;

		for (int i = 0; i < mDependABFileNameList.Count; ++i) {
			if (string.Compare(mDependABFileNameList[i].fileName, localPath, true) == 0)
				return true;
		}

		return false;
	}

    protected List<AssetBunbleInfo.DependFileInfo> DependABFileNameList
	{
		get {
			if (mDependABFileNameList == null)
                mDependABFileNameList = new List<AssetBunbleInfo.DependFileInfo>();
			return mDependABFileNameList;
		}
	}

    // 直接依赖的脚本文件名列表
    protected List<string> ScriptFileNameList
    {
        get
        {
            if (mScriptFileNameList == null)
                mScriptFileNameList = new List<string>();
            return mScriptFileNameList;
        }
    }

	protected List<string> FileList
	{
		get {
			if (mFileList == null)
				mFileList = new List<string>();
			return mFileList;
		}
	}


	// 依赖的文件名列表(Local Path)
	private List<DependFileInfo> mDependABFileNameList = null;
	// 包含的文件，如果是独立的包，则为NULL
	private List<string> mFileList = null;
	private int mAllDependCount = 0;
    private List<string> mScriptFileNameList = null;
}

// AB Ref Helper
static class AssetBundleRefHelper
{
	public static void ClearFileMetaMap()
	{
		mFileMetaMap.Clear();
	}

	public static int GetAssetBundleRefCount(string srcABFileName, string dependABFileName)
	{
		if (string.IsNullOrEmpty(srcABFileName))
			return 0;
		srcABFileName = srcABFileName.Replace('$', '/');
		string bundleExt = ".assets";
		if (srcABFileName.EndsWith(bundleExt))
			srcABFileName = srcABFileName.Substring(0, srcABFileName.Length - bundleExt.Length);
		AssetBunbleInfo srcInfo = AssetBundleBuild.FindAssetBundle(srcABFileName);
		if (srcInfo == null)
			return 0;
		return GetAssetBundleRefCount(srcInfo, dependABFileName);
	} 

	public static int GetAssetBundleRefCount(AssetBunbleInfo srcInfo, string dependABFileName)
	{
		if (srcInfo == null || string.IsNullOrEmpty(dependABFileName))
			return 0;
		dependABFileName = dependABFileName.Replace('$', '/');
		string bundleExt = ".assets";
		if (dependABFileName.EndsWith(bundleExt))
			dependABFileName = dependABFileName.Substring(0, dependABFileName.Length - bundleExt.Length);
		AssetBunbleInfo depInfo = AssetBundleBuild.FindAssetBundle(dependABFileName);
		if (depInfo == null)
			return 0;
		int ret = 0;
		if (srcInfo.SubFileCount > 0 && depInfo.SubFileCount > 0)
		{
			for (int i = 0; i < srcInfo.SubFileCount; ++i)
			{
				string srcSubFile = srcInfo.GetSubFiles(i);
				if (string.IsNullOrEmpty(srcSubFile))
					continue;
				string srcExt = Path.GetExtension(srcSubFile);
				bool isSceneFile = string.Compare(srcExt, ".unity") == 0;
				string yaml = GetYamlStr(srcSubFile);
				if (string.IsNullOrEmpty(yaml))
					continue;

				for (int j = 0; j < depInfo.SubFileCount; ++j)
				{
					string depSubFile = depInfo.GetSubFiles(j);
					if (string.IsNullOrEmpty(depSubFile))
						continue;
					string guid = GetMetaFileGuid(depSubFile);
					if (string.IsNullOrEmpty(guid))
						continue;
					int refCnt = GetDependRefCount(yaml, guid);
					if (refCnt > 0)
					{
						if (isSceneFile)
							ret += 1;
						else
							ret += refCnt;
					}
				}
			}
		}

		return ret;
	}

	private static int GetDependRefCount(string srcYaml, string dependGuid)
	{
		if (string.IsNullOrEmpty(srcYaml) || string.IsNullOrEmpty(dependGuid))
			return 0;
		int searchIdx = 0;
		int ret = 0;
		while (searchIdx >= 0)
		{
			searchIdx = srcYaml.IndexOf(dependGuid, searchIdx);
			if (searchIdx >= 0)
			{
				++ret;
				searchIdx += dependGuid.Length;
			}
		}

		return ret;
	}

	private static string[] _cYamlFileExts = {".unity", ".prefab", ".mat", ".controller", ".mask",
		".flare", ".renderTexture", ".mixer", ".giparams", ".anim", ".overrideController",
		".physicMaterial", ".physicsMaterial2D", ".guiskin", ".fontsettings", ".shadervariants",
		".cubemap"};

	private static bool IsYamlFile(string fileName)
	{
		if (EditorSettings.serializationMode != SerializationMode.ForceText)
			return false;
		
		string ext = Path.GetExtension(fileName);
		for (int i = 0; i < _cYamlFileExts.Length; ++i)
		{
			if (string.Compare(ext, _cYamlFileExts[i]) == 0)
				return true;
		}

		return false;
	}

	// fileName is not AssetBundle FileName
	private static string GetYamlStr(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return string.Empty;
		fileName = Path.GetFullPath(fileName);
		if (!File.Exists(fileName))
			return string.Empty;
	
		if (!IsYamlFile(fileName))
			return string.Empty;

		try
		{
			FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

			byte[] buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);

			stream.Close();
			stream.Dispose();

			string str = System.Text.Encoding.ASCII.GetString(buf);
			return str;
		} catch
		{
			return string.Empty;
		}
	}

	// fileName is not AssetBundle FileName
	private static string GetMetaFileGuid(string fileName)
	{
		string ret = string.Empty;
		if (mFileMetaMap.TryGetValue(fileName, out ret))
			return ret;
		string metaExt = ".meta";
		string metaFileName = fileName;
		if (!metaFileName.EndsWith(metaExt, true, System.Globalization.CultureInfo.CurrentCulture))
			metaFileName += metaExt;
		metaFileName = System.IO.Path.GetFullPath(metaFileName);
		if (!File.Exists(metaFileName))
			return ret;
		
		try
		{
			FileStream stream = new FileStream(metaFileName, FileMode.Open, FileAccess.Read);
			byte[] buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			
			stream.Close();
			stream.Dispose();
			
			string str = System.Text.Encoding.ASCII.GetString(buf);
			string guidPre = "guid:";
			int startIdx = str.IndexOf(guidPre);
			if (startIdx >= 0)
			{
				startIdx += guidPre.Length;
				int endIdx = str.IndexOf('\n', startIdx);
				if (endIdx >= 0)
				{
					ret = str.Substring(startIdx, endIdx - startIdx);
					ret = ret.Trim();
					
					mFileMetaMap.Add(fileName, ret);
				}
			}
		} catch
		{
			// nothing
		}
		
		return ret;
	}
	// fileName, Guid
	private static Dictionary<string, string> mFileMetaMap = new Dictionary<string, string>(); 
}

// AB Tree
class AssetBundleMgr
{

	public List<PkgSplitABDirInfo> ExternSplitABDirs
	{
		get;
		set;
	}

	public AssetBunbleInfo FindAssetBundle(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		AssetBunbleInfo ret;
		if (!mAssetBundleMap.TryGetValue (fileName, out ret))
			ret = null;
		return ret;
	}

	public int MaxTagFileCount
	{
		get;
		private set;
	}

	public int CurTagIdx
	{
		get;
		set;
	}

	private bool GetSplitABCnt(string dir, out int cnt)
	{
		cnt = 0;
		if (string.IsNullOrEmpty(dir))
			return false;
		string dirName = Path.GetFileName(dir);
		if (dirName.StartsWith("["))
		{
			int idx = dirName.IndexOf(']');
			if (idx <= 1)
				return false;
			string numStr = dirName.Substring(1, idx - 1);
			int num;
			if (int.TryParse(numStr, out num) && num > 0)
			{
				cnt = num;
				return true;
			}
		}

		// 外部分离目录
		if (ExternSplitABDirs != null)
		{
			for (int i = 0; i < ExternSplitABDirs.Count; ++i)
			{
				PkgSplitABDirInfo info = ExternSplitABDirs[i];
				if (string.IsNullOrEmpty(info.dirPath) || info.splitCnt <= 0)
					continue;
				if (string.Compare(info.dirPath, dir, true) == 0)
				{
					cnt = info.splitCnt;
					return true;
				}
			}
		}

		return false;
	}

	private bool IsSplitABDir(string dir)
	{
		int num;
		return GetSplitABCnt(dir, out num);
	}

	private void RemoveUnVaildLinkFile(ABLinkFileCfg cfg)
	{
		if (cfg == null || cfg.LinkCount <= 0)
			return;

		List<string> delKeyList = null;
		var iter = cfg.GetIter();
		while (iter.MoveNext())
		{
			string fileName = iter.Current.Key;
			fileName = Path.GetFullPath(fileName);
			if (!File.Exists(fileName))
			{
				if (delKeyList == null)
					delKeyList = new List<string>();
				delKeyList.Add(iter.Current.Key);
			}
		}
		iter.Dispose();

		if (delKeyList != null && delKeyList.Count > 0)
		{
			for (int i = 0; i < delKeyList.Count; ++i)
			{
				cfg.RemoveKey(delKeyList[i]);
			}

			string abSplitCfgFileName = Path.GetFullPath("buildABSplit.cfg");
			cfg.SaveToFile(abSplitCfgFileName);
		}

		EditorUtility.UnloadUnusedAssetsImmediate();
	}

	private void BuildSplitABDir(string splitDir, ABLinkFileCfg cfg, bool isManualDepend = false)
	{
		if (cfg == null || string.IsNullOrEmpty(splitDir))
			return;
		string[] files = Directory.GetFiles(splitDir, "*.*", SearchOption.TopDirectoryOnly);
		if (files == null || files.Length <= 0)
			return;

		RemoveUnVaildLinkFile(cfg);

		int maxCnt;
		if (!GetSplitABCnt(splitDir, out maxCnt) || maxCnt <= 0)
			return;

		List<string> resFiles = new List<string>();
		for (int i = 0; i < files.Length; ++i)
		{
			string fileName = files[i];
			if (AssetBundleBuild.FileIsResource(fileName))
				resFiles.Add(fileName);
		}

		if (resFiles.Count <= 0)
			return;

		// 查找最合适的
		int idx = splitDir.IndexOf(']');
		string subDir = splitDir.Substring(idx + 1).Trim();
		if (string.IsNullOrEmpty(subDir))
			return;

		splitDir = AssetBunbleInfo.GetLocalPath(splitDir);

		int curIdx = 0;
		while (true)
		{
			string dstDir = string.Format("{0}/@{1}{2:D}", splitDir, subDir, curIdx);
			int curCnt;
			if (cfg.GetDstDirCnt(dstDir, out curCnt))
			{
				if (curCnt < maxCnt)
					break;
			} else
				break;

			++curIdx;
		}

        string[] resAllFiles = SmartSort(resFiles);
        resFiles.Clear();
        resFiles.AddRange(resAllFiles);

        for (int i = 0; i < resFiles.Count; ++i)
		{
			string srcFileName = AssetBunbleInfo.GetLocalPath(resFiles[i]);
			if (cfg.ContainsLink(srcFileName))
				continue;

			string dstDir = string.Format("{0}/@{1}{2:D}", splitDir, subDir, curIdx);
			int curCnt;

			while (true)
			{
				if (!cfg.GetDstDirCnt(dstDir, out curCnt))
				{
					curCnt = 0;
					break;
				}
				if (curCnt + 1 > maxCnt)
				{
					++curIdx;
					dstDir = string.Format("{0}/@{1}{2:D}", splitDir, subDir, curIdx);
				} else
					break;
			}


			string dstFileName = string.Format("{0}/{1}", dstDir, Path.GetFileName(srcFileName));
			cfg.AddLink(srcFileName, dstFileName);
			++curCnt;
		}

        Dictionary<string, List<string>> dirFileMap = new Dictionary<string, List<string>>();
		var iter = cfg.GetIter();
		while (iter.MoveNext())
		{
			string dstFileName = iter.Current.Value;
			string dstDir = Path.GetDirectoryName(dstFileName);
			if (dirFileMap.ContainsKey(dstDir))
				dirFileMap[dstDir].Add(iter.Current.Key);
			else
			{
				List<string> list = new List<string>();
				list.Add(iter.Current.Key);
				dirFileMap.Add(dstDir, list);
			}
		}
		iter.Dispose();

		var dirIter = dirFileMap.GetEnumerator();
		while (dirIter.MoveNext())
		{
            string path = AssetBunbleInfo.GetLocalPath(dirIter.Current.Key).ToLower();

            if (!path.Contains(splitDir.ToLower())) {
                // 优化：不包含的不会处理
                continue;
            }

            AssetBunbleInfo outAB;
            if (mAssetBundleMap.TryGetValue(path, out outAB)) {
                // 这边特殊一点，需要删除并刷新
                if (outAB != null)
                    mAssetBundleList.Remove(outAB);
                mAssetBundleMap.Remove(path);
            }
            var list = dirIter.Current.Value;
            // 排个序
            //list.Sort();
            //string[] fileNames = list.ToArray();
            string[] fileNames = SmartSort(list);
            string fullPath = Path.GetFullPath(path);
			AssetBunbleInfo ab = new AssetBunbleInfo(fullPath, fileNames, isManualDepend);
			mAssetBundleMap.Add(path, ab);
			mAssetBundleList.Add(ab);
		}
		dirIter.Dispose();
	}

    // ------------按照文件名方式排序
        //static Regex digitRegex = new Regex(@"\d+");
        static string[] SmartSort(IEnumerable<string> files) {
            /*
            //这里只传文件名，以避免不必要的开销，不同的文件夹的文件没有智能排序的必要
            var maxLength = files.Max(file => digitRegex.Matches(file).Cast<Match>().Max(num => num.Length));

            var query = from file in files
                        let sortFile = digitRegex.Replace(file, m => m.Value.PadLeft(maxLength, '0'))
                        orderby sortFile
                        select file;

            return query.ToArray();*/
            string[] ret = files.OrderBy(x => x.Length).ThenBy(x => x).ToArray();
            return ret;
        }
        //-----------------------------

    private void BuildSplitABDirs(HashSet<string> splitABDirs, bool isManualDepend = false)
	{
		if (splitABDirs == null || splitABDirs.Count <= 0)
			return;

		string abSplitCfgFileName = Path.GetFullPath("buildABSplit.cfg");
		ABLinkFileCfg abLinkCfg = new ABLinkFileCfg();
		if (File.Exists(abSplitCfgFileName))
		{
			abLinkCfg.LoadFromFile(abSplitCfgFileName);
		}
		var abSplitIter = splitABDirs.GetEnumerator();
		while (abSplitIter.MoveNext())
		{
			BuildSplitABDir(abSplitIter.Current, abLinkCfg, isManualDepend);
			EditorUtility.UnloadUnusedAssetsImmediate();
		}
		abSplitIter.Dispose();

		abLinkCfg.SaveToFile(abSplitCfgFileName);
	}

#if USE_UNITY5_X_BUILD

	private void CheckRongYuRes(AssetBunbleInfo info, HashSet<string> fileList = null)
	{
		if (info == null)
			return;
		for (int i = 0; i < info.SubFileCount; ++i)
		{
			string subFileName = info.GetSubFiles(i);
			string[] depFileList = AssetDatabase.GetDependencies(subFileName, false);
			if (depFileList == null || depFileList.Length <= 0)
				continue;
			for (int j = 0; j < depFileList.Length; ++j)
			{
				string depFileName = depFileList[j];

				if (!AssetBundleBuild.FileIsResource(depFileName))
					continue;

				bool isFound = false;
				for (int k = 0; k < info.SubFileCount; ++k)
				{
					if (string.Compare(depFileName, info.GetSubFiles(k), true) == 0)
					{
						isFound = true;
						break;
					}
				}

				if (isFound)
					continue;

				AssetImporter importer = AssetImporter.GetAtPath(depFileName);
				if (importer == null)
					continue;

				isFound = !string.IsNullOrEmpty(importer.assetBundleName);

				if (!isFound)
				{
					// 打印出来
					Debug.LogFormat("<color=yellow>[{0}]</color><color=white>依赖被额外包含</color><color=red>{1}</color>", 
						info.BundleFileName, depFileName);

					if (fileList != null) {
						string key = string.Format("{0}=>has contains: {1}", subFileName, depFileName);
                            if (!fileList.Contains(key))
                                fileList.Add(key);
                    }
				}
			}
		}
	}

	// 打印未被打包但引用的资源
	private void CheckRongYuRes(string exportLogFile = "")
	{
		if (mAssetBundleList == null || mAssetBundleList.Count <= 0)
			return;
		bool isExportLogFile = !string.IsNullOrEmpty(exportLogFile);
		HashSet<string> fileList = null;
		if (isExportLogFile)
			fileList = new HashSet<string>();
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info == null)
				continue;
			CheckRongYuRes(info, fileList);
			EditorUtility.UnloadUnusedAssetsImmediate();
		}

		if (fileList != null && fileList.Count > 0)
		{
			// 输出文件到配置
			exportLogFile = Path.GetFullPath(exportLogFile);
			FileStream stream = new FileStream(exportLogFile, FileMode.Create);
			try
			{
				int writeBytes = 0;
				var iter = fileList.GetEnumerator();
				while (iter.MoveNext())
				{
					if (!string.IsNullOrEmpty(iter.Current))
					{
						string s = string.Format("{0}\r\n", iter.Current);
						byte[] buf = System.Text.Encoding.ASCII.GetBytes(s);
						if (buf != null && buf.Length > 0)
						{
							stream.Write(buf, 0, buf.Length);
							writeBytes += buf.Length;
							if (writeBytes > 2048)
							{
								stream.Flush();
								writeBytes = 0;
							}
						}
					}
				}
				iter.Dispose();
			} finally
			{
				stream.Close();
				stream.Dispose();
			}
		}
	}

#endif

	// 生成
	public void BuildDirs(List<string> dirList, bool isManualDepned = false)
	{
#if !USE_UNITY5_X_BUILD
		isManualDepned = true;
#endif
		Clear ();
		if ((dirList == null) || (dirList.Count <= 0))
			return;

		MaxTagFileCount = dirList.Count;
		List<string> abFiles = new List<string> ();
		HashSet<string> NotUsedDirHash = new HashSet<string> ();
        string notUsedSplit = "/" + AssetBundleBuild._NotUsed;
		// 需要分割的目錄
		HashSet<string> splitABDirs = new HashSet<string>();
		for (int i = 0; i < dirList.Count; ++i) {
			string dir = dirList[i];

			if (NotUsedDirHash.Contains(dir))
				continue;

			if (IsSplitABDir(dir))
			{
				// 分割的對象
				if (!splitABDirs.Contains(dir))
				{
					string[] files = System.IO.Directory.GetFiles(dir);
					if (files != null && files.Length > 0)
					{
						for (int j = 0; j < files.Length; ++j)
						{
							string fileName = files[j];
							if (AssetBundleBuild.FileIsResource(fileName))
							{
								splitABDirs.Add(dir);
								break;
							}
						}
					}
				}
				continue;
			}

            string abDir = dir;
            int notUsedIdx = abDir.IndexOf(notUsedSplit);
            if (notUsedIdx > 0)
            {
                abDir = abDir.Substring(0, notUsedIdx);
            }

			bool isMainFileMode = false;
		//	string localFileName = Path.GetFileName(dir);
			string localFileName = Path.GetFileName(abDir);
			bool isOnly = !localFileName.StartsWith(AssetBundleBuild._MainFileSplit);
			if (isOnly)
			{
				// 说明是单独文件模式
				isMainFileMode = true;
				string[] files = System.IO.Directory.GetFiles(dir);
				if (files != null)
				{
					for (int j = 0; j < files.Length; ++j)
					{
						string fileName = files[j];
						if (AssetBundleBuild.FileIsResource(fileName))
						{
						//	fileName = AssetBundleBuild.GetXmlFileName(fileName);
							abFiles.Add(fileName);
						}
					}
				}
			} else
			{
                abFiles.Add(abDir);
			}
			// 判断目录
			CheckDirsNotUsed(dir, NotUsedDirHash, abFiles, isMainFileMode);
		}
#if USE_UNITY5_X_BUILD
		AssetDatabase.RemoveUnusedAssetBundleNames();
		EditorUtility.UnloadUnusedAssetsImmediate();
#endif
		// 创建AssetBundleInfo
		for (int i = 0; i < abFiles.Count; ++i)
		{
			AssetBunbleInfo info = new AssetBunbleInfo(abFiles[i], isManualDepned);
			if (info.FileType != AssetBundleFileType.abError)
			{
                if (mAssetBundleMap.ContainsKey(info.Path))
                    continue;
				mAssetBundleList.Add(info);
				mAssetBundleMap.Add(info.Path, info);
			}
		}

		// 加入Split的AB LINK
		BuildSplitABDirs(splitABDirs, isManualDepned);

        EditorUtility.ClearProgressBar();

		if (isManualDepned)
		{
        	RefreshAllDependCount ();

			mAssetBundleList.Sort (AssetBunbleInfo.OnSort);
		}
#if USE_UNITY5_X_BUILD
		EditorUtility.ClearProgressBar();
		AssetDatabase.Refresh();
		//AssetDatabase.RemoveUnusedAssetBundleNames();
	    //AssetDatabase.Refresh();
#endif
	}

	private void CheckDirsNotUsed(string dir, HashSet<string> NotUsedDirHash, List<string> abFiles, bool isMainFileMode)
	{
		string[] dirs = System.IO.Directory.GetDirectories(dir);
		if (dirs != null)
		{
			for (int j = 0; j < dirs.Length; ++j)
			{
				string path = dirs[j];
				string local = System.IO.Path.GetFileName(path);
				if (local.StartsWith(AssetBundleBuild._NotUsed))
				{
					//if (!AssetBundleBuild.DirExistResource(path))
					//	continue;
					NotUsedDirHash.Add(path);
					
					if (isMainFileMode)
					{
						// add Files
						List<string> subFiles = AssetBundleBuild.GetAllSubResFiles(path);
						if ((subFiles != null) && (subFiles.Count > 0))
							abFiles.AddRange(subFiles);
					}
				}
			}
		}
	}

	private void RefreshAllDependCount()
	{
		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info == null)
				continue;
			info.RefreshAllDependCount();
		}
	}

	public void Clear()
	{
		CurTagIdx = 0;
		MaxTagFileCount = 0;
		mAssetBundleMap.Clear ();
		mAssetBundleList.Clear ();
	}

	public void Print()
	{
		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info != null)
			{
				info.Print();
			}
		}
	}

	private bool GetBuildTarget(eBuildPlatform platform, ref BuildTarget target)
	{
		switch(platform) {
		case eBuildPlatform.eBuildAndroid:
		{
			target = BuildTarget.Android;
			break;
		}
			
		case eBuildPlatform.eBuildWindow:
		{
			target = BuildTarget.StandaloneWindows;
			break;
		}
		case eBuildPlatform.eBuildMac:
		{
			target = BuildTarget.StandaloneOSXIntel;
			break;
		}
		case eBuildPlatform.eBuildIOS:
		{
			target = BuildTarget.iOS;
			break;
		}
		default:
			return false;
		}

		return true;
	}

    public void LocalAssetBundlesCopyToOtherProj(string outPrjRootPath, eBuildPlatform platform) {
            if (string.IsNullOrEmpty(outPrjRootPath))
                return;

            // 删除原来的数据
            string outPath = string.Format("{0}/Assets/StreamingAssets", outPrjRootPath);
            outPath = CreateAssetBundleDir(platform, outPath);
            if (System.IO.Directory.Exists(outPath)) {
                string[] localFiles = Directory.GetFiles(outPath, "*.*", SearchOption.TopDirectoryOnly);
                if (localFiles != null) {
                    for (int i = 0; i < localFiles.Length; ++i) {
                        string localFile = localFiles[i];
                        File.Delete(localFile);
                    }
                }
            }

            

            string localABPath = CreateAssetBundleDir(platform, string.Empty);
            string[] files = Directory.GetFiles(localABPath, "*.*", SearchOption.TopDirectoryOnly);
            if (files != null && files.Length > 0) {
                for (int i = 0; i < files.Length; ++i) {
                    string file = files[i];
                    string fileName = Path.GetFileName(file);
                    string dstFile = string.Format("{0}/{1}", outPath, fileName);
                    File.Copy(file, dstFile, true);
                }
            }
        }

    private string CreateAssetBundleDir(eBuildPlatform platform, string exportDir)
	{

		string outPath;
		bool isExternal = false;
		if (!string.IsNullOrEmpty(exportDir))
		{
			isExternal = true;
			outPath = exportDir;
		}
		else
			outPath = "Assets/StreamingAssets";

		if (!Directory.Exists(outPath))
		{
			if (isExternal)
				Directory.CreateDirectory(outPath);
			else
				AssetDatabase.CreateFolder("Assets", "StreamingAssets");
		}

		switch(platform) {
		case eBuildPlatform.eBuildAndroid:
		{
			outPath += "/Android";
			if (!Directory.Exists(outPath)) 
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "Android");
			}
			break;
		}
		
		case eBuildPlatform.eBuildWindow:
		{
			outPath += "/Windows";
			if (!Directory.Exists(outPath))
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "Windows");
			}
			break;
		}
		case eBuildPlatform.eBuildMac:
		{
			outPath += "/Mac";
			if (!Directory.Exists(outPath))
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "Mac");
			}
			break;
		}
		case eBuildPlatform.eBuildIOS:
		{
			outPath += "/IOS";
			if (!Directory.Exists(outPath))
			{
				if (isExternal)
					Directory.CreateDirectory(outPath);
				else
					AssetDatabase.CreateFolder("Assets/StreamingAssets", "IOS");
			}
			break;
		}
		default:
			return string.Empty;
		}

		return outPath;
	}
	
#if USE_UNITY5_X_BUILD
	private string m_TempExportDir;
	private int m_TempCompressType;
	private BuildTarget m_TempBuildTarget;
	private bool m_TempIsAppendForce;
	
	private void OnBuildTargetChanged()
	{
		EditorUserBuildSettings.activeBuildTargetChanged -= OnBuildTargetChanged;
		ProcessBuild_5_x(m_TempExportDir, m_TempCompressType, m_TempBuildTarget, m_TempIsAppendForce);
	}

    private UnityEditor.AssetBundleBuild[] BuildAssetBundleBuildArrayFrommAssetBundleList(bool isUseAssetBundleXml = false) {
        List<UnityEditor.AssetBundleBuild> buildList = new List<UnityEditor.AssetBundleBuild>();
        for (int i = 0; i < mAssetBundleList.Count; ++i) {
            AssetBunbleInfo info = mAssetBundleList[i];
            if (info != null) {
                UnityEditor.AssetBundleBuild build = new UnityEditor.AssetBundleBuild();
                build.assetBundleName = info.BundleFileName;
                build.assetBundleVariant = string.Empty;
                List<string> subFileList = new List<string>();
                for (int j = 0; j < info.SubFileCount; ++j) {
                    subFileList.Add(info.GetSubFiles(j));
                }
                build.assetNames = subFileList.ToArray();
                buildList.Add(build);
            }
        }

        if (isUseAssetBundleXml) {
            UnityEditor.AssetBundleBuild build = new UnityEditor.AssetBundleBuild();
            build.assetBundleName = "AssetBundles.xml";
            build.assetBundleVariant = string.Empty;
            build.assetNames = new string[] { "Assets/AssetBundles.xml" };
            buildList.Add(build);
        }

        return buildList.ToArray();
    }

    private AssetBundleManifest CallBuild_5_x_API(string exportDir, int compressType, BuildTarget target, bool isReBuild = true, 
        bool isBuildAssetBundleXml = false)
	{
		BuildAssetBundleOptions buildOpts = /*BuildAssetBundleOptions.DisableWriteTypeTree |*/
			BuildAssetBundleOptions.DeterministicAssetBundle;

		/*	
		if (target != BuildTarget.StandaloneLinux && target != BuildTarget.StandaloneLinux64 &&
			target != BuildTarget.StandaloneLinuxUniversal && target != BuildTarget.StandaloneOSXIntel &&
			target != BuildTarget.StandaloneOSXIntel64 && target != BuildTarget.StandaloneOSXUniversal &&
			target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64)
			buildOpts |= BuildAssetBundleOptions.DisableWriteTypeTree;
			*/
		
		if (isReBuild)
			buildOpts |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
		if (compressType == 0)
			buildOpts |= BuildAssetBundleOptions.UncompressedAssetBundle;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
        else if (compressType == 2)
			buildOpts |= BuildAssetBundleOptions.ChunkBasedCompression;
#endif


        // AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(exportDir, buildOpts, target);
        UnityEditor.AssetBundleBuild[] abs = BuildAssetBundleBuildArrayFrommAssetBundleList(isBuildAssetBundleXml);
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(exportDir, abs, buildOpts, target);    
    
        return manifest;
	}
	
	void ProcessBuild_5_x(string exportDir, int compressType, BuildTarget target, bool isForceAppend)
	{
		AssetBundleManifest manifest = CallBuild_5_x_API(exportDir, compressType, target, !isForceAppend);
		
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && (!info.IsBuilded) && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
			{
				info.IsBuilded = true;
				info.CompressType = compressType;
				info.RebuildDependFiles(manifest);
			}
		}

		EditorUtility.UnloadUnusedAssetsImmediate();
	}
	
#endif

	private void BuildAssetBundlesInfo_5_x(eBuildPlatform platform, string exportDir, int compressType, bool isForceAppend)
	{	
#if USE_UNITY5_X_BUILD
		if (string.IsNullOrEmpty(exportDir))
			return;
		BuildTarget target = BuildTarget.Android;
		if (!GetBuildTarget(platform, ref target))
			return;
		
		if (EditorUserBuildSettings.activeBuildTarget != target)
		{
			EditorUserBuildSettings.activeBuildTargetChanged += OnBuildTargetChanged;
			m_TempExportDir = exportDir;
			m_TempCompressType = compressType;
			m_TempBuildTarget = target;
			m_TempIsAppendForce = isForceAppend;
			EditorUserBuildSettings.SwitchActiveBuildTarget(target);
			return;
		}

		ProcessBuild_5_x(exportDir, compressType, target, isForceAppend);
		
#endif
	}

    public static string GetAssetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return string.Empty;
        fullPath = fullPath.Replace("\\", "/");
        int index = fullPath.IndexOf("Assets/", StringComparison.CurrentCultureIgnoreCase);
        if (index < 0)
            return fullPath;
        string ret = fullPath.Substring(index);
        return ret;
    }

	private void BuildAssetBundleInfo(AssetBunbleInfo info, eBuildPlatform platform, string exportDir, int compressType)
	{
#if USE_UNITY5_X_BUILD
#else
		if ((info == null) || (info.IsBuilded) || string.IsNullOrEmpty(exportDir) || (info.FileType == AssetBundleFileType.abError) || (info.SubFileCount <= 0))
			return;
		BuildTarget target = BuildTarget.Android;
		if (!GetBuildTarget (platform, ref target))
			return;

		if (info.DependFileCount > 0) {
			// check DepndFile
			for (int i = 0; i < info.DependFileCount; ++i)
			{
				string fileName = info.GetDependFiles(i);
				if (!string.IsNullOrEmpty(fileName))
				{
					AssetBunbleInfo depInfo;
					if ((!mAssetBundleMap.TryGetValue(fileName, out depInfo)) || (depInfo == null))
					{
						string errStr = string.Format("AssetBundle [{0}] depend file: {1} is not exists", info.Path, fileName);
						Debug.LogError(errStr);
						return;
					}
				    
					if ((!depInfo.IsBuilded) && (depInfo.AllDependCount != info.AllDependCount))
					{
						string errStr = string.Format("AssetBundle [{0}] depend file: {1} is not build first", info.Path, fileName);
						Debug.LogError(errStr);
						return;
					}

				}
			}
		}

		// Create AssetBundle
		string localOutFileName = info.BundleFileName;
		string outFileName = string.Format("{0}/{1}", exportDir, localOutFileName);
		if (info.IsScene) {
			string[] fileArr = info.GetSubFiles ();
			if (fileArr == null) {
				string errStr = string.Format ("AssetBundle [{0}] Subfiles is empty", info.Path);
				Debug.LogError (errStr);
				return;
			}

			BuildOptions buildOpts = BuildOptions.BuildAdditionalStreamedScenes;
			if (compressType != 1)
				buildOpts |= BuildOptions.UncompressedAssetBundle;
			
			//BuildPipeline.BuildPlayer(fileArr, outFileName, target, buildOpts);
			BuildPipeline.BuildStreamedSceneAssetBundle (fileArr, outFileName, target, buildOpts); 
			info.IsBuilded = true;
			info.CompressType = compressType;
			return;
		} else {
			// not BuildAssetBundleOptions.CollectDependencies
			BuildAssetBundleOptions buildOpts = BuildAssetBundleOptions.CompleteAssets |
												BuildAssetBundleOptions.DisableWriteTypeTree |
												BuildAssetBundleOptions.DeterministicAssetBundle;
			if (compressType != 1)
				buildOpts |= BuildAssetBundleOptions.UncompressedAssetBundle;

			if (info.IsMainAsset)
			{
				string mainFileName = info.GetSubFiles(0);

				UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(mainFileName);
				if (mainAsset == null)
				{
					string errStr = string.Format ("AssetBundle [{0}] Subfiles has null UnityObject", info.Path);
					Debug.LogError (errStr);
					return;
				}

				bool ret = BuildPipeline.BuildAssetBundle(mainAsset, null, outFileName, buildOpts, target);
				if (!ret)
				{
					string errStr = string.Format ("AssetBundle [{0}] build not ok", info.Path);
					Debug.LogError (errStr);
					return;
				}

				info.IsBuilded = true;
				info.CompressType = compressType;
			} else
			if (info.FileType == AssetBundleFileType.abDirFiles)
			{
				List<UnityEngine.Object> assetObjs = new List<UnityEngine.Object>();

				for (int i = 0; i < info.SubFileCount; ++i)
				{
					string subFileName = info.GetSubFiles(i);
					if (string.IsNullOrEmpty(subFileName))
						continue;
					Type t = AssetBundleBuild.GetResourceExtType(subFileName);

					if (t == null)
					{
						string errStr = string.Format ("AssetBundle [{0}] Subfile [{1}] has null Type", info.Path, subFileName);
						Debug.LogError (errStr);
						return;
					}

					UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(subFileName, t);
					if (asset == null)
					{
						string errStr = string.Format ("AssetBundle [{0}] Subfile [{1}] has null UnityObject", info.Path, subFileName);
						Debug.LogError (errStr);
						return;
					}

					assetObjs.Add(asset);
				}

				bool ret = BuildPipeline.BuildAssetBundle(null, assetObjs.ToArray(), outFileName, buildOpts, target);
				if (!ret)
				{
					string errStr = string.Format ("AssetBundle [{0}] build not ok", info.Path);
					Debug.LogError (errStr);
					return;
				}

				info.IsBuilded = true;
				info.CompressType = compressType;
			}
		}
#endif
	}

	private void ResetAssetBundleInfo ()
	{
		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if (info != null)
			{
				info.IsBuilded = false;
				info.CompressType = 0;
			}
		}
	}

	private void ExportXmlStr(StringBuilder builder, bool isMd5, string outPath)
	{
		if (builder == null)
			return;

		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded)
                info.ExportXml(builder, isMd5, outPath);
		}
	}

#if USE_FLATBUFFER
    // 存放FlatBuffer里
    private void ExportFlatBuffers(string exportPath, bool isMd5) {
        if (string.IsNullOrEmpty(exportPath))
            return;
        string fullPath = Path.GetFullPath(exportPath);
        if (string.IsNullOrEmpty(fullPath))
            return;

#if USE_DEP_BINARY_AB
        string fileName = "Assets/AssetBundles.xml";
#else
		string fileName = string.Format ("{0}/AssetBundles.xml", fullPath);
#endif
        if (System.IO.File.Exists(fileName)) {
            System.IO.File.Delete(fileName);
        }

        FlatBufferBuilder flatBuilder = new FlatBufferBuilder(1);

        int abFileCount = mAssetBundleList == null ? 0 : mAssetBundleList.Count;
        var fileHeaderOffset = DependBinaryFile.ExportFileHeader(flatBuilder, abFileCount);

        List<Offset<AssetBundleFlatBuffer.AssetBundleInfo>> assetBundleFlatBufferList = new List<Offset<AssetBundleFlatBuffer.AssetBundleInfo>>();
        for (int i = 0; i < mAssetBundleList.Count; ++i) {
            AssetBunbleInfo info = mAssetBundleList[i];
            if ((info != null) && info.IsBuilded) {
                var offset = info.ExportFlatBuffer(flatBuilder, isMd5, fullPath);
                assetBundleFlatBufferList.Add(offset);
            }
        }
        var assetBundlesVector = AssetBundleFlatBuffer.AssetBundleTree.CreateAssetBundlesVector(flatBuilder, assetBundleFlatBufferList.ToArray());


        AssetBundleFlatBuffer.AssetBundleTree.StartAssetBundleTree(flatBuilder);
        AssetBundleFlatBuffer.AssetBundleTree.AddFileHeader(flatBuilder, fileHeaderOffset);
        AssetBundleFlatBuffer.AssetBundleTree.AddAssetBundles(flatBuilder, assetBundlesVector);
        var assetBundleTreeOffset = AssetBundleFlatBuffer.AssetBundleTree.EndAssetBundleTree(flatBuilder);
        AssetBundleFlatBuffer.AssetBundleTree.FinishAssetBundleTreeBuffer(flatBuilder, assetBundleTreeOffset);

        byte[] buffer = flatBuilder.SizedByteArray();
        if (buffer != null) {
            FileStream stream = new FileStream(fileName, FileMode.Create);
            stream.Write(buffer, 0, buffer.Length);
            stream.Close();
            stream.Dispose();
        }
    }
#endif

	// 导出二进制
	private void ExportBinarys(string exportPath, bool isMd5)
	{
		if (string.IsNullOrEmpty (exportPath))
			return;
		string fullPath = Path.GetFullPath (exportPath);
		if (string.IsNullOrEmpty (fullPath))
			return;
#if USE_DEP_BINARY_AB
		string fileName = "Assets/AssetBundles.xml";
#else
		string fileName = string.Format ("{0}/AssetBundles.xml", fullPath);
#endif
		if (System.IO.File.Exists (fileName)) {
			System.IO.File.Delete(fileName);
		}

		FileStream stream = new FileStream (fileName, FileMode.Create);

		int abFileCount = mAssetBundleList == null ? 0: mAssetBundleList.Count;
		DependBinaryFile.ExportFileHeader(stream, abFileCount, DependBinaryFile.FLAG_UNCOMPRESS);

		for (int i = 0; i < mAssetBundleList.Count; ++i) {
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded)
				info.ExportBinary(stream, isMd5, fullPath);
		}

      //  long abFileMapOffset = stream.Position;
      //  long fileMapCnt = AssetBundleMapToBinaryFile (stream);

      //  stream.Seek (0, SeekOrigin.Begin);
      //  DependBinaryFile.ExportFileHeader (stream, abFileCount, DependBinaryFile.FLAG_UNCOMPRESS, abFileMapOffset, fileMapCnt);


		stream.Close ();
		stream.Dispose ();
	}

    /*
    private long AssetBundleMapToBinaryFile(Stream stream)
    {
        long cnt = 0;
        for (int i = 0; i < mAssetBundleList.Count; ++i) {
            var info = mAssetBundleList [i];
            if (info != null) {
                for (int j = 0; j < info.SubFileCount; ++j) {
                    string fileName = info.GetSubFiles (j);
                    if (!string.IsNullOrEmpty (fileName)) {
                        fileName = AssetBunbleInfo.GetSubFileNameFormat(fileName);
                        if (string.IsNullOrEmpty(fileName))
                            continue;
                        FilePathMgr.Instance.WriteString (stream, fileName);
                        ++cnt;
                        FilePathMgr.Instance.WriteLong (stream, info.FileOffset);
                    }
                }
            }
        }

        return cnt;
    }*/

	// export xml
	private void ExportXml(string exportPath, bool isMd5 = false)
	{
		if (string.IsNullOrEmpty (exportPath))
			return;
		string fullPath = Path.GetFullPath (exportPath);
		if (string.IsNullOrEmpty (fullPath))
			return;
		string fileName = string.Format ("{0}/AssetBundles.xml", fullPath);
		if (System.IO.File.Exists (fileName)) {
			System.IO.File.Delete(fileName);
		}

		FileStream stream = new FileStream (fileName, FileMode.Create);

		StringBuilder builder = new StringBuilder ();
		builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
		builder.AppendLine ();
		builder.Append ("<AssetBundles>");
		builder.AppendLine ();
		ExportXmlStr (builder, isMd5, fullPath);
		builder.Append("</AssetBundles>");
		string str = builder.ToString ();
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes (str);
		stream.Write (bytes, 0, bytes.Length);
		stream.Dispose ();
	}

    public void CopyBundleManifestFiles_5_x(string rootPath, string outPath)
    {
#if USE_UNITY5_X_BUILD
        if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(outPath))
            return;

        if (!Directory.Exists(outPath))
            return;

        string[] files = Directory.GetFiles(rootPath, "*.manifest", SearchOption.TopDirectoryOnly);
        if (files == null || files.Length <= 0)
            return;

        for (int i = 0; i < files.Length; ++i)
        {
            string fileName = Path.GetFileName(files[i]);
            fileName = string.Format("{0}/{1}", outPath, fileName);
            File.Copy(files[i], fileName, true);
        }
#endif
    }

    public void RemoveBundleManifestFiles_5_x(string outPath)
	{
#if USE_UNITY5_X_BUILD
		string[] files = Directory.GetFiles(outPath, "*.manifest", SearchOption.TopDirectoryOnly);
		for (int i = 0; i < files.Length; ++i)
		{
			File.Delete(files[i]);
		}
#endif
	}

	public static string GetUnityEditorPath()
	{
#if UNITY_EDITOR_WIN
			string pathList = System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(pathList))
				return string.Empty;

			char[] split = new char[1];
			split[0] = ';';
			string[] paths = pathList.Split(split, StringSplitOptions.RemoveEmptyEntries);
			if (paths == null || paths.Length <= 0)
				return string.Empty;
			for (int i = 0; i < paths.Length; ++i) {
				string p = paths[i];
				if (string.IsNullOrEmpty(p))
					continue;
				int unityIdx = p.IndexOf("Unity", StringComparison.CurrentCultureIgnoreCase);
				if (unityIdx < 0)
					continue;
				p = p.Replace('\\', '/');
				int editorIdx = p.IndexOf("/Editor", StringComparison.CurrentCultureIgnoreCase);
				if (editorIdx < 0 || editorIdx <= unityIdx)
					continue;
				return p;
			}
#endif
			return string.Empty;
	}

	public bool BuildCSharpProject(string ProjFileName, string buildExe)
	{
#if UNITY_EDITOR_WIN
		if (string.IsNullOrEmpty(ProjFileName) || string.IsNullOrEmpty(buildExe))
			return false;
		if (!File.Exists(ProjFileName))
		{
			Debug.LogErrorFormat("【编译】不存在文件：", ProjFileName);
			return false;
		}

		string unityEditorPath = GetUnityEditorPath();
		if (string.IsNullOrEmpty(unityEditorPath))
		{
			Debug.LogError("请增加UnityEditor环境变量Path");
			return false;
		}

		unityEditorPath = unityEditorPath.Replace('/', '\\');

		string preCmd = string.Format("start /D \"{0}\\Data\\MonoBleedingEdge\\bin\" /B", unityEditorPath);
		//string preCmd = "start /B";

		 ProjFileName = ProjFileName.Replace('/' , '\\');
		 buildExe = buildExe.Replace('/', '\\');
		// DefineConstants=""
		string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
		string cmdDefines = string.Empty;
		if (defines != null && defines.Length > 0)
		{
			defines = defines.Replace(';', ',');
			// 处理一下没有用的编译指令
			cmdDefines = string.Format(" /p:DefineConstants=\"{0}\"", defines);
		}
		string cmd = string.Format("{0} {1} {2} /p:Configuration=Release{3}", preCmd, buildExe, ProjFileName, cmdDefines);
		AssetBundleBuild.RunCmd(cmd);
		return true;
#else
		return false;
#endif
	}

	public void BuildCSharpProjectUpdateFile(string streamAssetsPath, string outPath, string version)
	{
		/*
		string unityEditorPath = GetUnityEditorPath();
		if (string.IsNullOrEmpty(unityEditorPath))
			return;

		string buildExe = unityEditorPath + "/Data/MonoBleedingEdge/lib/mono/unity/xbuild.exe";
		*/
	//	string buildExe = "xbuild.bat";

		string rootPath = System.IO.Directory.GetCurrentDirectory();
		rootPath = rootPath.Replace('\\', '/');
		string projFileName = rootPath + "/Assembly-CSharp.csproj";
	//	if (!BuildCSharpProject(projFileName, buildExe))
	//		return;

		string resDir = outPath + '/' + version;
		resDir = System.IO.Path.GetFullPath(resDir);

		if (!System.IO.Directory.Exists(resDir))
			System.IO.Directory.CreateDirectory(resDir);

		resDir = resDir.Replace('\\', '/');

		string[] csharpFiles = new string[1];
		csharpFiles[0] = projFileName;

		string fileListFileName1 = streamAssetsPath + "/fileList.txt";
		string fileListFileName2 = resDir + "/fileList.txt";

		List<string> externFiles = new List<string>();
		List<bool> firstDowns = new List<bool>();
		for (int i = 0; i < csharpFiles.Length; ++i)
		{
			string s = csharpFiles[i];

			string dllFileName = System.IO.Path.GetFileNameWithoutExtension(s) + ".dll";
			string f = string.Format("{0}/Temp/bin/Release/{1}", rootPath, dllFileName);

			if (File.Exists(f))
			{
				externFiles.Add(f);
				firstDowns.Add(true);

				string dllMd5 = AssetBunbleInfo.Md5(f, false);
				string dstDllFileName = resDir + "/" + dllMd5 + ".dll";
				File.Copy(f, dstDllFileName);
			}
		}

		string srcFileName = streamAssetsPath + "/AssetBundles.xml";
		if (File.Exists(srcFileName))
		{
			externFiles.Add(srcFileName);
			firstDowns.Add(false);
			string md5Str = AssetBunbleInfo.Md5(srcFileName, false);
			string dstFileName = resDir + "/" + md5Str + ".xml";
			File.Copy(srcFileName, dstFileName);
		}

		string[] fileArr = externFiles.ToArray();
		bool[] firstDownArr = firstDowns.ToArray();
		ExternMd5WriteToFileList(fileArr, fileListFileName1, firstDownArr);
		ExternMd5WriteToFileList(fileArr, fileListFileName2, firstDownArr);
	}

	private void CSharpDllCopyTo(string srcDll, string dstDll)
	{
		if (string.IsNullOrEmpty(srcDll) || string.IsNullOrEmpty(dstDll))
			return;

	}

	private void ExternMd5WriteToFileList(string[] files, string fileListFileName, bool[] isFirstDowns)
	{
		if (files == null || files.Length <= 0 || string.IsNullOrEmpty(fileListFileName))
			return;

		if (System.IO.File.Exists(fileListFileName))
		{
			FileStream fileStream = new FileStream(fileListFileName, FileMode.Open, FileAccess.Read);
			if (fileStream.Length > 0)
			{
				byte[] src = new byte[fileStream.Length];
				
				fileStream.Read(src, 0, src.Length);
				fileStream.Close();
				fileStream.Dispose();
				
				string s = System.Text.Encoding.ASCII.GetString(src);
				s = s.Trim();
				if (!string.IsNullOrEmpty(s))
				{
					ResListFile resFile = new ResListFile();
					resFile.Load(s);
					bool isNews = false;
					for (int i = 0; i < files.Length; ++i)
					{
						string f = Path.GetFileName(files[i]);
						f = f.Trim();
						if (string.IsNullOrEmpty(f))
							continue;
						// string key = AssetBunbleInfo.Md5(csharpFiles[i], true);
						string ext = Path.GetExtension(files[i]);
						string value = AssetBunbleInfo.Md5(files[i], false) + ext;

						bool isFirstDown = false;
						if (isFirstDowns != null && i < isFirstDowns.Length)
							isFirstDown = isFirstDowns[i]; 

						long fileSize = 0;
						if (File.Exists(files[i]))
						{
							FileInfo fileInfo = new FileInfo(files[i]);
							fileSize = fileInfo.Length;
						}

						if (!resFile.AddFile(f, value, isFirstDown, fileSize))
							Debug.LogErrorFormat("【BuildCSharpProjectUpdateFile】 file {0} error!", f);
						else
							isNews = true;
					}
					
					if (isNews)
						resFile.SaveToFile(fileListFileName);
				}
			} else
			{
				fileStream.Close();
				fileStream.Dispose();
			}
		}

	}

	private static MD5 m_ZipMd5 = new MD5CryptoServiceProvider();
	private void BuildVersionZips(string outPath, string version)
	{
		outPath = Path.GetFullPath(outPath);
		string resDir = outPath + '/' + version;
		string fileListFileName1 = resDir + "/version.txt";
		string versionFileName1 = outPath + "/version.txt";

		string ver;
		string zz;
		string myFileListMd5;

		if (!AutoUpdateMgr.GetResVerByFileName(fileListFileName1, out ver, out myFileListMd5, out zz))
			return;

		fileListFileName1 = string.Format("{0}/{1}.txt", resDir, myFileListMd5);

		// 生成个版本ZIP包
		// 写入ZIP版本位置(只写在更新的version里)
		if (!File.Exists(fileListFileName1))
			return;
		ResListFile myResFileList = new ResListFile();
		if (!myResFileList.LoadFromFile(fileListFileName1))
			return;

		string zipVerMd5 = string.Empty;
		string zipVerFileName = outPath + "/zipVer.txt";
		FileStream zipVerFileStream = new FileStream(zipVerFileName, FileMode.Create);
		try
		{
			string zipStr = string.Empty;
			string[] subDirs = Directory.GetDirectories(outPath);
			if (subDirs != null)
			{
				for (int i = 0; i < subDirs.Length; ++i)
				{
					string subDir = subDirs[i];
					string subName = Path.GetFileName(subDir);
					if (string.Compare(version, subName) == 0)
						continue;

					string ff = string.Format("{0}/version.txt", subDir);
					if (!File.Exists(ff))
						continue;

					string oflMd5;
					string ozz;
					if (!AutoUpdateMgr.GetResVerByFileName(ff, out ver, out oflMd5, out ozz))
						continue;

					string ofl = string.Format("{0}/{1}.txt", subDir, oflMd5);
					if (!File.Exists(ofl))
						continue;

					ResListFile otherFileList = new ResListFile();
					if (!otherFileList.LoadFromFile(ofl))
						continue;

					// 要生成两个ZIP包
					string zipFileName = string.Format("{0}/{1}.zip", outPath, ZipTools.GetZipFileName(version, subName));
					if (File.Exists(zipFileName))
						File.Delete(zipFileName);

					zipFileName = string.Format("{0}/{1}.zip", outPath, ZipTools.GetZipFileName(subName, version));
					if (File.Exists(zipFileName))
						File.Delete(zipFileName);

					if (ZipTools.BuildVersionZip(outPath, version, subName, myResFileList, otherFileList, oflMd5))
					{
						string zipName = ZipTools.GetZipFileName(version, subName);
						string fileName = string.Format("{0}/{1}.zip", outPath, zipName);

						// 根据ZIP内容生成MD5
						string md5Str = string.Empty;
						FileStream zipFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
						try
						{
							byte[] hash = m_ZipMd5.ComputeHash(zipFileStream);
							for (int j = 0; j < hash.Length; j++)
							{
								md5Str += hash[j].ToString("X").PadLeft(2, '0');  
							}

							md5Str = md5Str.ToLower();
						} finally
						{
							zipFileStream.Close();
							zipFileStream.Dispose();
						}

						if (!string.IsNullOrEmpty(md5Str))
						{
							if (string.IsNullOrEmpty(zipStr))
								zipStr = string.Format("{0}={1}.zip", zipName, md5Str);
							else
								zipStr += string.Format("\r\n{0}={1}.zip", zipName, md5Str);
						}
					}

					if (ZipTools.BuildVersionZip(outPath, subName, version, otherFileList, myResFileList, myFileListMd5))
					{
						string zipName = ZipTools.GetZipFileName(subName, version);
						string fileName = string.Format("{0}/{1}.zip", outPath, zipName);

						// 根据ZIP内容生成MD5
						string md5Str = string.Empty;
						FileStream zipFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
						try
						{
							byte[] hash = m_ZipMd5.ComputeHash(zipFileStream);
							for (int j = 0; j < hash.Length; j++)
							{
								md5Str += hash[j].ToString("X").PadLeft(2, '0');  
							}

							md5Str = md5Str.ToLower();
						} finally
						{
							zipFileStream.Close();
							zipFileStream.Dispose();
						}

						if (!string.IsNullOrEmpty(md5Str))
						{
							if (string.IsNullOrEmpty(zipStr))
								zipStr = string.Format("{0}={1}.zip", zipName, md5Str);
							else
								zipStr += string.Format("\r\n{0}={1}.zip", zipName, md5Str);
						}
					}
				}
					
				if (!string.IsNullOrEmpty(zipStr))
				{
					byte[] zipBytes = System.Text.Encoding.ASCII.GetBytes(zipStr);
					zipVerFileStream.Write(zipBytes, 0, zipBytes.Length);
					byte[] hash = m_ZipMd5.ComputeHash(zipBytes);
					for (int i = 0; i < hash.Length; i++)
					{
						zipVerMd5 += hash[i].ToString("X").PadLeft(2, '0');  
					}
					zipVerMd5 = zipVerMd5.ToLower();

					string zipVerStr = string.Format("\r\nzip={0}", zipVerMd5);
					byte[] zipVerBytes = System.Text.Encoding.ASCII.GetBytes(zipVerStr);

					// 写入version.txt
					FileStream fileStream = new FileStream(versionFileName1, FileMode.Open, FileAccess.Write);
					try
					{
						fileStream.Seek(0, SeekOrigin.End);
						fileStream.Write(zipVerBytes, 0, zipVerBytes.Length);
					} finally
					{
						fileStream.Close();
						fileStream.Dispose();
					}

				}
			}

		} finally
		{
			zipVerFileStream.Close();
			zipVerFileStream.Dispose();
		}

		if (!string.IsNullOrEmpty(zipVerMd5))
		{
			if (File.Exists(zipVerFileName))
			{
				string zipVerMd5FileName = string.Format("{0}/{1}.txt", outPath, zipVerMd5);
				if (File.Exists(zipVerMd5FileName))
					File.Delete(zipVerMd5FileName);
				File.Move(zipVerFileName, zipVerMd5FileName);
				File.Delete(zipVerFileName);
			}
		}
	}

    private void CreateManifestResUpdateFiles(string streamAssetsPath, string outPath, string version) {
        string resDir = outPath + '/' + '.' + "manifest_" + version;
        resDir = System.IO.Path.GetFullPath(resDir);
        if (System.IO.Directory.Exists(resDir)) {
            string[] fileNames = System.IO.Directory.GetFiles(resDir, "*.*", SearchOption.TopDirectoryOnly);
            if (fileNames != null) {
                for (int i = 0; i < fileNames.Length; ++i) {
                    System.IO.File.Delete(fileNames[i]);
                }
            }
        }

        if (!System.IO.Directory.Exists(resDir))
            System.IO.Directory.CreateDirectory(resDir);

        for (int i = 0; i < mAssetBundleList.Count; ++i) {
            AssetBunbleInfo info = mAssetBundleList[i];
            if ((info != null) && info.IsBuilded && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError)) {
                string manifestFileName = info.BundleFileName;
                manifestFileName = streamAssetsPath + '/' + manifestFileName + ".manifest";
                if (File.Exists(manifestFileName)) {
                    string targetManifestFileName = Path.GetFileName(manifestFileName);
                    targetManifestFileName = string.Format("{0}/{1}", resDir, targetManifestFileName);
                    File.Copy(manifestFileName, targetManifestFileName, true);
                }
            }
        }
    }
	
	// 设置UNITY里面的版本号
    static public void SetUnityPackageVersion(string apkVersion) {
        PlayerSettings.bundleVersion = apkVersion;
        // 立即保存设置
#if UNITY_5_6
        AssetDatabase.SaveAssets();
#else
        EditorApplication.SaveAssets();
#endif
    }

	private void CreateBundleResUpdateFiles(string streamAssetsPath, string outPath, string version, bool isRemoveVersionDir)
	{
		string resDir = outPath + '/' + version;
		resDir = System.IO.Path.GetFullPath(resDir);

		if (isRemoveVersionDir)
		{
			if (System.IO.Directory.Exists(resDir))
			{
				string[] fileNames = System.IO.Directory.GetFiles(resDir, "*.*", SearchOption.TopDirectoryOnly);
				if (fileNames != null)
				{
					for(int i = 0; i < fileNames.Length; ++i)
					{
						System.IO.File.Delete(fileNames[i]);
					}
				}
			}
		}

		if (!System.IO.Directory.Exists(resDir))
			System.IO.Directory.CreateDirectory(resDir);

		List<string> fileList = new List<string>();
		for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
			{
				string md5FileName = info.Md5BundleFileName(streamAssetsPath);
				string newMd5FileName = info.Md5BundleFileName(streamAssetsPath, false);
				string bundleFileName = info.BundleFileName;
				if (string.IsNullOrEmpty(bundleFileName) || string.IsNullOrEmpty(md5FileName) || string.IsNullOrEmpty(newMd5FileName))
					continue;
				string fileCompareStr = string.Format("{0}={1}", md5FileName, newMd5FileName);

				bundleFileName = streamAssetsPath + '/' + bundleFileName;
                if (!File.Exists(bundleFileName))
                    bundleFileName = streamAssetsPath + '/' + md5FileName;
				newMd5FileName = resDir + '/' + newMd5FileName;
				if (File.Exists(bundleFileName))
				{
					FileInfo fileInfo = new FileInfo(bundleFileName);
					long fileSize = fileInfo.Length;
					fileCompareStr += string.Format(";{0}", fileSize.ToString());

					fileList.Add(fileCompareStr);
					if (!File.Exists(newMd5FileName))
					{
						File.Copy(bundleFileName, newMd5FileName);
					} else
					{
						Debug.LogErrorFormat("Bundle To Md5: [srcFile: {0}] => [dstFile: {1}] is exists", 
						                       info.BundleFileName, newMd5FileName);
					}
				}
			}
		}

		// write fileList file
		string fileListFileName = streamAssetsPath + "/fileList.txt";
		string fileListFileName1 = resDir + "/fileList.txt";
		FileStream fileStream = new FileStream(fileListFileName, FileMode.Create, FileAccess.Write);
		try
		{
			int writeBytes = 0;
			for (int i = 0; i < fileList.Count; ++i)
			{
				string flieListStr = fileList[i];
				if (!string.IsNullOrEmpty(flieListStr))
				{
					flieListStr += "\r\n";
					byte[] fileListBytes = System.Text.Encoding.ASCII.GetBytes(flieListStr);
					if (fileListBytes != null && fileListBytes.Length > 0)
					{
						fileStream.Write(fileListBytes, 0, fileListBytes.Length);
						writeBytes += fileListBytes.Length;
						if (writeBytes > 2048)
						{
							writeBytes = 0;
							fileStream.Flush();
						}
					}
				}
			}
		} 
		finally
		{
			fileStream.Close();
			fileStream.Dispose();
		}

		File.Copy(fileListFileName, fileListFileName1, true);

		// write version file
		string versionFileName = streamAssetsPath + "/version.txt";
		string versionFileName2 = resDir + "/version.txt";
		string versionFileName1 = Path.GetFullPath(outPath + "/version.txt");
		fileStream = new FileStream(versionFileName, FileMode.Create, FileAccess.Write);
		try
		{
			string fileListMd5 = AssetBunbleInfo.Md5(fileListFileName, false);
			string versionStr = string.Format("res={0}\r\nfileList={1}", version, fileListMd5);
			byte[] versionBytes = System.Text.Encoding.ASCII.GetBytes(versionStr);
			fileStream.Write(versionBytes, 0, versionBytes.Length);
		}
		finally
		{
			fileStream.Close();
			fileStream.Dispose();
		}

		File.Copy(versionFileName, versionFileName1, true);
		File.Copy(versionFileName, versionFileName2, true);

		// 写入package信息
	}

	private void ChangeFileListFileNameToMd5(string outPath)
	{
		outPath = Path.GetFullPath(outPath);
		outPath = outPath.Replace('\\', '/');

		// 修改FileList文件名为MD5，来自version.txt
		string versionFileName = string.Format("{0}/version.txt", outPath);
		if (File.Exists(versionFileName))
		{
			// 读取version.txt中fileList的MD5信息
			FileStream versionFileStream = new FileStream(versionFileName, FileMode.Open, FileAccess.Read);
			try
			{
				if (versionFileStream.Length > 0)
				{
					byte[] bytes = new byte[versionFileStream.Length];
					if (bytes != null && bytes.Length > 0)
					{
						versionFileStream.Read(bytes, 0, bytes.Length);
						string str = System.Text.Encoding.ASCII.GetString(bytes);
						if (!string.IsNullOrEmpty(str))
						{
							string[] lines = str.Split('\n');
							if (lines != null && lines.Length > 0)
							{
								string fileListMd5 = string.Empty;
								for (int i = 0; i < lines.Length; ++i)
								{
									string s = lines[i].Trim();
									if (string.IsNullOrEmpty(s))
										continue;
									if (s.StartsWith("fileList="))
									{
										fileListMd5 = s.Substring(9);
										break;
									}
								}

								if (!string.IsNullOrEmpty(fileListMd5))
								{
									string oldFileListFileName = string.Format("{0}/fileList.txt", outPath);
									if (File.Exists(oldFileListFileName))
									{
										string newFileListFileName = string.Format("{0}/{1}.txt", outPath, fileListMd5);
										File.Copy(oldFileListFileName, newFileListFileName, true);
										File.Delete(oldFileListFileName);
									}
								}
							}
						}
					}
				}
			} finally
			{
				versionFileStream.Close();
				versionFileStream.Dispose();
			}
		}
	}

//	private static readonly bool _cIsOnlyFileNameMd5 = true;
	private void ChangeBundleFileNameToMd5(string outPath)
	{
        // MD5对应文件
        Dictionary<string, string> md5FindFileName = new Dictionary<string, string>();

        // Temp script
        for (int i = 0; i < mAssetBundleList.Count; ++i)
		{
			AssetBunbleInfo info = mAssetBundleList[i];
			if ((info != null) && info.IsBuilded && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
			{
				string oldFileName = outPath + '/' + info.BundleFileName;
				string md5FileName = info.Md5BundleFileName(outPath);
				if (string.IsNullOrEmpty(md5FileName))
					continue;
				string newFileName = outPath + '/' + md5FileName;
				if (File.Exists(oldFileName))
				{
					if (File.Exists(newFileName))
						File.Delete(newFileName);

					FileInfo fileInfo = new FileInfo(oldFileName);
					fileInfo.MoveTo(newFileName);
                    md5FindFileName.Add(Path.GetFileName(newFileName), Path.GetFileName(oldFileName));
                    /*
					if (!File.Exists(newFileName))
					{
						FileInfo fileInfo = new FileInfo(oldFileName);
						fileInfo.MoveTo(newFileName);
					} else
					{
						File.Delete(oldFileName);

						Debug.LogWarningFormat("Bundle To Md5: [srcFile: {0}] => [dstFile: {1}] is exists", 
						                       info.BundleFileName, md5FileName);
					}*/
                }
			}
		}

        // Save Md5
        var iter = md5FindFileName.GetEnumerator();
        FileStream md5FindStream = new FileStream("Assets/md5Find.txt", FileMode.Create);
        while (iter.MoveNext()) {
            string s = string.Format("{0}={1}\r\n", iter.Current.Key, iter.Current.Value);
            byte[] buf = System.Text.Encoding.ASCII.GetBytes(s);
            if (buf != null && buf.Length > 0) {
                md5FindStream.Write(buf, 0, buf.Length);
                md5FindStream.Flush();
            }
        }
        iter.Dispose();
        md5FindStream.Close();
        md5FindStream.Dispose();

    }

    private void RemoveOrgProjStreamingAssets(eBuildPlatform platform)
    {
        string dstRoot = "Assets/StreamingAssets";
        switch (platform) {
        case eBuildPlatform.eBuildWindow:
            dstRoot += "/Windows";
            break;
        case eBuildPlatform.eBuildMac:
            dstRoot += "/Mac";
            break;
        case eBuildPlatform.eBuildAndroid:
            dstRoot += "/Android";
            break;
        case eBuildPlatform.eBuildIOS:
            dstRoot += "/IOS";
            break;
        default:
            return;
        }

        if (!Directory.Exists(dstRoot))
            Directory.CreateDirectory(dstRoot);
        else {
            // 1.删除本项目中平台的AB和manifest文件
            string[] files = Directory.GetFiles(dstRoot, "*.*", SearchOption.TopDirectoryOnly);
            if (files != null && files.Length > 0) {
                for (int i = 0; i < files.Length; ++i) {
                    string localFileName = Path.GetFullPath(files[i]);
                    File.Delete(localFileName);
                }
            }
        }
    }

    // 根据buildVersion.txt准备增量打包信息(增量用)
    private void CopyVersionForceAppendFiles(eBuildPlatform platform, string outPath) {
#if USE_UNITY5_X_BUILD
        if (string.IsNullOrEmpty(outPath))
            return;

        RemoveOrgProjStreamingAssets(platform);

        string dstRoot = "Assets/StreamingAssets";
        switch (platform) {
        case eBuildPlatform.eBuildWindow:
            dstRoot += "/Windows";
            break;
        case eBuildPlatform.eBuildMac:
            dstRoot += "/Mac";
            break;
        case eBuildPlatform.eBuildAndroid:
            dstRoot += "/Android";
            break;
        case eBuildPlatform.eBuildIOS:
            dstRoot += "/IOS";
            break;
        default:
            return;
        }

        string curretnVersion, lastVersion;
        AssetBundleBuild.GetPackageVersion(platform, out curretnVersion, out lastVersion);



        // 2.读取需要差异化的版本
        string rootDir = outPath;
        string fileName = Path.GetFullPath(string.Format("{0}/{1}/version.txt", rootDir, lastVersion));
        if (!File.Exists(fileName))
            return;

        string ver, zipMd5, fileListMd5;
        if (!AutoUpdateMgr.GetResVerByFileName(fileName, out ver, out fileListMd5, out zipMd5))
            return;
        fileName = Path.GetFullPath(string.Format("{0}/{1}/{2}.txt", rootDir, lastVersion, fileListMd5));
        if (!File.Exists(fileName))
            return;

        ResListFile resFile = new ResListFile();
        if (!resFile.LoadFromFile(fileName))
            return;
        // 3.转换拷贝名字
        var fileContentIter = resFile.GetFileContentMd5Iter();
        while (fileContentIter.MoveNext()) {
            fileName = Path.GetFullPath(string.Format("{0}/{1}/{2}", rootDir, lastVersion, fileContentIter.Current.Key));
            if (File.Exists(fileName)) {
                string dstFileName = string.Format("{0}/{1}", dstRoot, fileContentIter.Current.Value);
                File.Copy(fileName, dstFileName, true);
            }
        }
        fileContentIter.Dispose();

        // Copy Mainfest
        rootDir = string.Format("{0}/.manifest_{1}", rootDir, lastVersion);
        if (Directory.Exists(rootDir)) {
            CopyBundleManifestFiles_5_x(rootDir, dstRoot);
        }
		
		AssetDatabase.Refresh();
#endif
    }

    // 5.x打包方法
    private void BuildAssetBundles_5_x(eBuildPlatform platform, int compressType, string outPath, bool isMd5, bool isForceAppend)
    {
#if USE_UNITY5_X_BUILD
        // 5.x不再需要收集依赖PUSH和POP
        Caching.CleanCache();
        string abOutPath;
        if (isForceAppend || string.IsNullOrEmpty(outPath))
             abOutPath = null;
        else
            abOutPath = outPath + "/Assets/StreamingAssets";
        string exportDir = CreateAssetBundleDir(platform, outPath);
        if (mAssetBundleList.Count > 0)
        {

#if USE_DEP_BINARY && USE_DEP_BINARY_AB
			AssetImporter xmlImport = AssetImporter.GetAtPath("Assets/AssetBundles.xml");
			if (xmlImport != null)
			{
				if (!string.IsNullOrEmpty(xmlImport.assetBundleName))
				{
					xmlImport.assetBundleName = string.Empty;
					xmlImport.SaveAndReimport();
				}
			}
#endif
            /*
            for (int i = 0; i < mAssetBundleList.Count; ++i)
            {
                AssetBunbleInfo info = mAssetBundleList[i];
                if ((info != null) && (!info.IsBuilded) && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError))
                    BuildAssetBundleInfo_5_x(info, platform, exportDir, compressType);
            }*/

            if (isForceAppend)
                CopyVersionForceAppendFiles(platform, "outPath");
            else
                RemoveOrgProjStreamingAssets(platform);

            BuildAssetBundlesInfo_5_x(platform, exportDir, compressType, isForceAppend);

            // 是否存在冗余资源，如果有打印出来
            // 不在使用Importer.AssetBundleName，所以冗余也不通过这个打印了
            //CheckRongYuRes("Err_RongYu.txt");
            // 冗余检查请用 ArtCheck/美术冗余资源检查

#if USE_DEP_BINARY
            // 二进制格式
#if USE_FLATBUFFER
            ExportFlatBuffers(exportDir, isMd5);
#else
            ExportBinarys(exportDir, isMd5);
#endif

#else
            // export xml
            ExportXml(exportDir, isMd5);
#endif

            AssetDatabase.Refresh();

#if USE_DEP_BINARY && USE_DEP_BINARY_AB
			BuildTarget target = BuildTarget.Android;
			if (GetBuildTarget(platform, ref target))
			{
				if (xmlImport == null)
					xmlImport = AssetImporter.GetAtPath("Assets/AssetBundles.xml");
				if (xmlImport != null)
				{
                    //	xmlImport.assetBundleName = "AssetBundles.xml";
                    //	xmlImport.SaveAndReimport();
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
                    CallBuild_5_x_API(exportDir, compressType, target, false, true);
#else
					CallBuild_5_x_API(exportDir, 0, target,  false, true);
#endif

					AssetDatabase.Refresh();

					string xmlSrcFile = string.Format("{0}/assetbundles.xml", exportDir);
					if (File.Exists(xmlSrcFile))
					{
						string xmlDstFile = string.Format("{0}/AssetBundles.xml", exportDir);
						File.Move(xmlSrcFile, xmlDstFile);
						AssetDatabase.Refresh();
					}
				}
			}

#endif

			if (isMd5)
			{
				ProcessVersionRes(exportDir, platform);
				ChangeBundleFileNameToMd5(exportDir);
			}

            // 删除manifest
            RemoveBundleManifestFiles_5_x(exportDir);

            AssetDatabase.Refresh();

            ResetAssetBundleInfo();
        }
#endif
    }

    // 删除原始目录的FileList不存在资源
    private void RemoveFilelistFileNameMd5NoContainsRes(eBuildPlatform platform, string streamingAssets)
    {
        if (string.IsNullOrEmpty (streamingAssets))
            return;
        
        if (!Directory.Exists (streamingAssets))
            return;
        
        if (!Directory.Exists (streamingAssets))
            return;
        string fileListFileName = Path.GetFullPath(string.Format("{0}/fileList.txt", streamingAssets));
        if (!File.Exists(fileListFileName))
            return;
        ResListFile resFile = new ResListFile();
        if (!resFile.LoadFromFile(fileListFileName))
            return;

        fileListFileName = Path.GetFileName(fileListFileName);

        string[] files = Directory.GetFiles(streamingAssets, "*.*", SearchOption.TopDirectoryOnly);
        // 删除冗余的AB和manifest文件
        if (files != null && files.Length > 0) {
            for (int i = 0; i < files.Length; ++i) {
                string localFileName = Path.GetFileName(files[i]);
                if (string.Compare (localFileName, "AssetBundles.xml", true) == 0)
                    continue;
                string extName = Path.GetExtension (localFileName);
                bool isMainifest = string.Compare (extName, ".mainifest", true) == 0;
                bool isOrgAssets = string.Compare (extName, ".assets", true) == 0;
                bool isMeta = string.Compare (extName, ".meta", true) == 0;
                if (isMeta)
                    continue;

                if (!isMainifest || isOrgAssets) {
                    string localFileNameMd5;
                    if (isOrgAssets)
                        localFileNameMd5 = AssetBunbleInfo.Md5 (localFileName);
                    else
                        localFileNameMd5 = localFileName;
                    if (string.Compare (localFileName, fileListFileName) != 0 &&
                        string.Compare (localFileName, "version.txt", true) != 0 &&
                        string.IsNullOrEmpty (resFile.GetFileContentMd5 (localFileNameMd5))) {
                        File.Delete (files [i]);
                    }
                } else {
                    string localName = Path.GetFileNameWithoutExtension (localFileName);
                    string localMd5FileName = AssetBunbleInfo.Md5 (localFileName, true);
                    if (string.IsNullOrEmpty (resFile.GetFileContentMd5 (localMd5FileName)))
                        File.Delete (files [i]);
                }
            }
        }

    }

    // 删除不包含fileList的资源
    private void RemoveFileListContentMd5NoContainsRes(string rootDir, string version) {
        if (string.IsNullOrEmpty(rootDir) || string.IsNullOrEmpty(version))
            return;
        
        string fileName = Path.GetFullPath(string.Format("{0}/{1}/version.txt", rootDir, version));
        if (!File.Exists(fileName))
            return;
        string ver, fileListMd5, zipMd5;
        if (!AutoUpdateMgr.GetResVerByFileName(fileName, out ver, out fileListMd5, out zipMd5))
            return;

        
        string fileListMd5FileName = Path.GetFullPath(string.Format("{0}/{1}/{2}.txt", rootDir, version, fileListMd5));
        if (!File.Exists(fileListMd5FileName))
            return;
        ResListFile resFile = new ResListFile();
        if (!resFile.LoadFromFile(fileListMd5FileName))
            return;

        fileListMd5FileName = Path.GetFileName(fileListMd5FileName);
        string dir = Path.GetFullPath(string.Format("{0}/{1}", rootDir, version));
        string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
        if (files != null && files.Length > 0) {
            for (int i = 0; i < files.Length; ++i) {
                string localFileName = Path.GetFileName(files[i]);
                if (string.Compare(localFileName, fileListMd5FileName) != 0 && 
                    string.IsNullOrEmpty(resFile.FindFileNameMd5(localFileName)) &&
                    string.Compare(localFileName, "version.txt", true) != 0) {
                    // 删除文件
                    File.Delete(files[i]);
                }
            }
        }        
    }

	private void ProcessVersionRes(string streamAssetsPath, eBuildPlatform platform)
	{
	//	if (platform == eBuildPlatform.eBuildAndroid || platform == eBuildPlatform.eBuildIOS ||
	//	    platform == eBuildPlatform.eBuildWindow)
		{
            string versionDir;
            string lastVersion;
            AssetBundleBuild.GetPackageVersion(platform, out versionDir, out lastVersion);
            // Create Bunlde到outPut目录
			CreateBundleResUpdateFiles(streamAssetsPath, "outPath", versionDir, true);
            // Copy Manifest到outPut目录
            CreateManifestResUpdateFiles(streamAssetsPath, "outPath", versionDir);
            // Copy CSharp的Dll
            BuildCSharpProjectUpdateFile(streamAssetsPath, "outPath", versionDir);
			// 修改fileList的文件名为MD5
			string fileListRootPath = string.Format("outPath/{0}", versionDir);
			ChangeFileListFileNameToMd5(fileListRootPath);
            // 删除先打包目录下的冗余AB，以减少大小
            RemoveFilelistFileNameMd5NoContainsRes (platform, streamAssetsPath);
            // 删除不存在的内容MD5文件名文件
            RemoveFileListContentMd5NoContainsRes("outPath", versionDir);
#if USE_ZIPVER
			BuildVersionZips("outPath", versionDir);
#endif
        }
    }

	// isCompress
	public void BuildAssetBundles(eBuildPlatform platform, int compressType, bool isMd5 = false, string outPath = null, 
								  bool isForceAppend = false)
	{
		AssetBundleRefHelper.ClearFileMetaMap();
		AssetBunbleInfo.ClearMd5FileMap();
#if USE_UNITY5_X_BUILD
        // 5.x版本采用新打包
        string appVersion = Application.unityVersion;
        if (appVersion.StartsWith("5."))
        {
			BuildAssetBundles_5_x(platform, compressType, outPath, isMd5, isForceAppend);
            return;
        }
#else

		Caching.CleanCache ();

		string exportDir = CreateAssetBundleDir (platform);
		int dependLevel = -1;
		int pushCnt = 0;
		if (mAssetBundleList.Count > 0) {
			for (int i = 0; i < mAssetBundleList.Count; ++i) {
				AssetBunbleInfo info = mAssetBundleList [i];
				if ((info != null) && (!info.IsBuilded) && (info.SubFileCount > 0) && (info.FileType != AssetBundleFileType.abError)) {
					bool isPush = (dependLevel < info.DependFileCount);

					if (isPush)
					{
						BuildPipeline.PushAssetDependencies();
						++pushCnt;
					}

					dependLevel = info.DependFileCount;

					BuildAssetBundleInfo (info, platform, exportDir, compressType);
				}
			}

			for (int i = 0; i < pushCnt; ++i)
			{
				BuildPipeline.PopAssetDependencies();
			}

#if USE_DEP_BINARY
#if USE_FLATBUFFER
            ExportFlatBuffer(exportDir, isMd5);
#else
			// 二进制格式
			ExportBinarys(exportDir, isMd5);
#endif
#else
			// export xml
			ExportXml(exportDir, isMd5);
#endif

			if (isMd5)
			{
				ProcessVersionRes(exportDir, platform);
				ChangeBundleFileNameToMd5(exportDir);
			}

			AssetDatabase.Refresh ();

			ResetAssetBundleInfo ();
		}
#endif
    }

	private void ProcessPackage(BuildTarget platform, string outputFileName, bool isNew, bool canProfilter, bool isDebug, bool isDevelop)
	{
		var scenes = EditorBuildSettings.scenes;
		List<string> sceneNameList = new List<string> ();
		for (int i = 0; i < scenes.Length; ++i) {
			var scene = scenes[i];
			if (scene != null)
			{
				if (scene.enabled)
				{
					//string sceneName = Path.GetFileNameWithoutExtension(scene.path);
					if (System.IO.File.Exists(scene.path))
					{
						//Debug.Log("Apk scenePath: " + scene.path);
						if (!string.IsNullOrEmpty(scene.path))
							sceneNameList.Add(scene.path);
					}
				}
			}
		}
		
		if (sceneNameList.Count <= 0)
			return;
		
		string[] levelNames = sceneNameList.ToArray ();
		
		BuildOptions opts;
		if (isNew)
			opts = BuildOptions.None;
		else
			opts = BuildOptions.AcceptExternalModificationsToPlayer;
		
		if (canProfilter)
			opts |= BuildOptions.ConnectWithProfiler;
		if (isDebug)
			opts |= BuildOptions.AllowDebugging;
		if (isDevelop)
			opts |= BuildOptions.Development;
		
		BuildPipeline.BuildPlayer (levelNames, outputFileName, platform, opts); 
	}

	private bool m_TempIsNew;
	private bool m_TempCanProfilter;
	private bool m_TempIsDebug;
	private bool m_TempIsDevep;
	private string m_TempOutput;
	private BuildTarget m_TempTarget;

	private void OnBuildPackagePlatformChanged()
	{
		EditorUserBuildSettings.activeBuildTargetChanged -= OnBuildPackagePlatformChanged;
		ProcessPackage(m_TempTarget, m_TempOutput, m_TempIsNew, m_TempCanProfilter, m_TempIsDebug, m_TempIsDevep);
	}

	public bool BuildPackage(eBuildPlatform buildPlatform, string outputFileName, bool isNew, bool canProfilter = false, bool isDebug = false, bool isDevelop = false)
	{
		if (string.IsNullOrEmpty (outputFileName))
			return false;

		BuildTarget platform = BuildTarget.Android;
		if (!GetBuildTarget (buildPlatform, ref platform))
			return false;

		EditorUserBuildSettings.allowDebugging = isDebug;
		EditorUserBuildSettings.development = isDevelop;
		EditorUserBuildSettings.connectProfiler = canProfilter;
		if (EditorUserBuildSettings.activeBuildTarget != platform)
		{
			m_TempIsNew = isNew;
			m_TempCanProfilter = canProfilter;
			m_TempIsDebug = isDebug;
			m_TempIsDevep = isDevelop;
			m_TempOutput = outputFileName;
			m_TempTarget = platform;
			EditorUserBuildSettings.activeBuildTargetChanged += OnBuildPackagePlatformChanged;
			EditorUserBuildSettings.SwitchActiveBuildTarget (platform);
			return true;
		}

		ProcessPackage(platform, outputFileName, isNew, canProfilter, isDebug, isDevelop);
		return true;
	}

	private Dictionary<string, AssetBunbleInfo> mAssetBundleMap = new Dictionary<string, AssetBunbleInfo>();
	// 排序，按照有木有依赖来排序
	private List<AssetBunbleInfo> mAssetBundleList = new List<AssetBunbleInfo>();
}

[ExecuteInEditMode]
public static class AssetBundleBuild
{
    private static string cAssetsResourcesPath = "Assets/Resources/";
	// 支持的资源文件格式
	private static readonly string[] ResourceExts = {".prefab", ".fbx",
													 ".png", ".jpg", ".dds", ".gif", ".psd", ".tga", ".bmp",
													 ".txt", ".bytes", ".xml", ".csv", ".json",
													 ".controller", ".shader", ".anim", ".unity", ".mat",
													 ".wav", ".mp3", ".ogg",
													 ".ttf", ".otf",
													 ".shadervariants", ".asset"};
	
	private static readonly string[] ResourceXmlExts = {".prefab", ".fbx",
														".tex", ".tex",  ".tex", ".tex", ".tex", ".tex", ".tex",
														".bytes", ".bytes", ".bytes", ".bytes", ".bytes",
														".controller", ".shader", ".anim", ".unity", ".mat",
														".audio", ".audio", ".audio",
													    ".ttf", ".otf",
														".shaderVar", ".asset"};

	private static readonly Type[] ResourceExtTypes = {
														typeof(UnityEngine.GameObject), typeof(UnityEngine.GameObject),
														typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture), typeof(UnityEngine.Texture),
														typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset), typeof(UnityEngine.TextAsset),
														typeof(UnityEngine.Object), typeof(UnityEngine.Shader), typeof(UnityEngine.AnimationClip), null, typeof(UnityEngine.Material),
														typeof(UnityEngine.AudioClip), typeof(UnityEngine.AudioClip), typeof(UnityEngine.AudioClip),
														typeof(UnityEngine.Font), typeof(UnityEngine.Font),
														typeof(UnityEngine.ShaderVariantCollection), typeof(UnityEngine.ScriptableObject)
	};

	private static readonly string[] _DirSplit = {"\\"};

	// 目录中所有子文件为一个AB
	public static readonly string _MainFileSplit = "@";
	// _ 表示这个文件夹被忽略
	public static readonly string _NotUsed = "_";
	// Resources目录应该被忽略(未实现)

	public static string GetXmlExt(string ext)
	{
		if (string.IsNullOrEmpty (ext))
			return string.Empty;

		for (int i = 0; i < ResourceExts.Length; ++i) {
			if (string.Compare(ext, ResourceExts[i], true) == 0)
			{
#if USE_HAS_EXT
				return ResourceExts[i];
#else
				return ResourceXmlExts[i];
#endif
			}
		}

		return string.Empty;
	}

	public static string GetXmlFileName(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return string.Empty;
		string ext = Path.GetExtension (fileName);
		string newExt = GetXmlExt (ext);
		if (string.IsNullOrEmpty (newExt))
			return string.Empty;
		if (string.Compare (newExt, ".unity", true) == 0)
			fileName = Path.GetFileName (fileName);
		return Path.ChangeExtension (fileName, newExt);
	}

	public static Type GetResourceExtType(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return null;
		string ext = Path.GetExtension (fileName);
		if (string.IsNullOrEmpty (ext))
			return null;

		for (int i = 0; i < ResourceExts.Length; ++i) {
			if (string.Compare(ext, ResourceExts[i], true) == 0)
			{
				return ResourceExtTypes[i];
			}
		}

		return null;
	}


	private static void GetAllSubResFiles(string fullPath, List<string> fileList)
	{
		if ((fileList == null) || (string.IsNullOrEmpty(fullPath)))
			return;
		
		string[] files = System.IO.Directory.GetFiles (fullPath);
		if ((files != null) && (files.Length > 0)) {
			for (int i = 0; i < files.Length; ++i)
			{
				string fileName = files[i];
				if (FileIsResource(fileName))
					fileList.Add(fileName);
			}
		}
		
		string[] dirs = System.IO.Directory.GetDirectories (fullPath);
		if (dirs != null) {
			for (int i = 0; i < dirs.Length; ++i)
			{
				GetAllSubResFiles(dirs[i], fileList);
			}
		}
	}
	
	public static List<string> GetAllSubResFiles(string fullPath)
	{
		List<string> fileList = new List<string> ();
		GetAllSubResFiles (fullPath, fileList);
		return fileList;
	}

	public static List<string> GetAllLocalSubDirs(string rootPath)
	{
		if (string.IsNullOrEmpty (rootPath))
			return null;
		string fullRootPath = System.IO.Path.GetFullPath (rootPath);
		if (string.IsNullOrEmpty (fullRootPath))
			return null;

		string[] dirs = System.IO.Directory.GetDirectories (fullRootPath);
		if ((dirs == null) || (dirs.Length <= 0))
			return null;
		List<string> ret = new List<string> ();

		for (int i = 0; i < dirs.Length; ++i) {
			string dir = AssetBunbleInfo.GetLocalPath(dirs[i]);
			ret.Add (dir);
		}
		for (int i = 0; i < dirs.Length; ++i) {
			string dir = dirs[i];
			List<string> list = GetAllLocalSubDirs(dir);
			if (list != null)
				ret.AddRange(list);
		}

		return ret;
	}

	private static bool IsVaildSceneResource(string fileName)
	{
		bool ret = false;

		if (string.IsNullOrEmpty (fileName))
			return ret;

		string localFileName = AssetBunbleInfo.GetLocalPath (fileName);
		if (string.IsNullOrEmpty (localFileName))
			return ret;

		var scenes = EditorBuildSettings.scenes;
		if (scenes == null)
			return ret;

		var iter = scenes.GetEnumerator ();
		while (iter.MoveNext()) {
			EditorBuildSettingsScene scene = iter.Current as EditorBuildSettingsScene;
			if ((scene != null) && scene.enabled)
			{
				if (string.Compare(scene.path, localFileName, true) == 0)
				{
					ret = true;
					break;
				}
			}
		}

		return ret;
	}

    // 是否在Assets/Resources内
    public static bool IsInAssetsResourcesDir(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        int idx = fileName.IndexOf(cAssetsResourcesPath);
        return (idx >= 0);
    }

    // 是否在其他的Resources目录
    public static bool IsOtherResourcesDir(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        if (IsInAssetsResourcesDir(fileName))
            return false;
        int idx = fileName.IndexOf("/Resources/");
        return (idx > 0);
    }

    // 文件是否是脚本
    public static bool FileIsScript(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        string ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
            return false;
        if (string.Compare(ext, ".cs", true) == 0)
            return true;
        return false;
    }

	public static bool FileIsResource(string fileName)
	{
		if (string.IsNullOrEmpty (fileName))
			return false;
		string ext = Path.GetExtension (fileName);
		if (string.IsNullOrEmpty (ext))
			return false;
		for (int i = 0; i < ResourceExts.Length; ++i) {
			if (string.Compare(ext, ResourceExts[i], true) == 0)
			{
				if ((ResourceExts[i] == ".fbx") || (ResourceExts[i] == ".controller"))
				{
					// ingore xxx@idle.fbx
					string name = Path.GetFileNameWithoutExtension(fileName);
					if (name.IndexOf('@') >= 0)
						return false;
				} else
				if (ResourceExts[i] == ".unity")
				{
					if (!IsVaildSceneResource(fileName))
						return false;
				}
				return true;
			}
		}

		return false;
	}

	// 根据目录判断是否有资源文件
	public static bool DirExistResource(string path)
	{
		if (string.IsNullOrEmpty (path))
			return false;
		string fullPath = Path.GetFullPath (path);
		if (string.IsNullOrEmpty (fullPath))
			return false;

		string[] files = System.IO.Directory.GetFiles (fullPath);
		if ((files == null) || (files.Length <= 0))
			return false;
		for (int i = 0; i < files.Length; ++i) {
			string ext = System.IO.Path.GetExtension(files[i]);
			if (string.IsNullOrEmpty(ext))
				continue;
			for (int j = 0; j < ResourceExts.Length; ++j)
			{
				if (string.Compare(ext, ResourceExts[j], true) == 0)
				{
					if ((ResourceExts[j] == ".fbx") || (ResourceExts[j] == ".controller"))
					{
						// ingore xxx@idle.fbx
						string name = Path.GetFileNameWithoutExtension(files[i]);
						if (name.IndexOf('@') >= 0)
							return false;
					} else
					if (ResourceExts[j] == ".unity")
					{
						if (!IsVaildSceneResource(files[i]))
							return false;
					}
					return true;
				}
			}
		}

		return false;
	}


	// 获得Assets下面有资源的文件夹
	private static void BuildResAllDirPath(string rootPath, HashSet<string> dirHash)
	{
		if (string.IsNullOrEmpty (rootPath) || (dirHash == null))
			return;

		string dirName = System.IO.Path.GetFileName (rootPath);
		if (dirName.StartsWith (_NotUsed)) {
			// add parentlist
			string parentPath = System.IO.Path.GetDirectoryName(rootPath);
			if (!AssetBundleBuild.DirExistResource(parentPath))
			{
				if (!dirHash.Contains(parentPath))
					dirHash.Add(parentPath);
			}
			return;
		}

		string fullPath = Path.GetFullPath (rootPath);
		if (string.IsNullOrEmpty (fullPath))
			return;
		if (DirExistResource(fullPath))
			dirHash.Add (fullPath);
		// 获得fullPath目录下的所有子文件夹
		string[] dirs = System.IO.Directory.GetDirectories (fullPath);
		if ((dirs != null) && (dirs.Length > 0)) {
			for (int i = 0; i < dirs.Length; ++i)
			{
				BuildResAllDirPath(dirs[i], dirHash);
			}
		}
	}

	// 目录排序
	private static int OnDirSort(string dir1, string dir2)
	{
		// 获得 \\ 次数
		string[] str1 = dir1.Split (_DirSplit, StringSplitOptions.None);
		string[] str2 = dir2.Split (_DirSplit, StringSplitOptions.None);
		if (((str1 == null) && (str2 == null)) || ((str1.Length <= 0) && (str2.Length <= 0)) || (str1.Length == str2.Length))
			return 0;

		if ((str1 == null) || (str1.Length <= 0))
			return 1;

		if ((str2 == null) || (str2.Length <= 0))
			return -1;

		if (str1.Length < str2.Length)
			return 1;

		return -1;
	}

	/*
	private static List<string> GetResAllDirPath(string rootPath)
	{
		HashSet<string> dirSet = new HashSet<string> ();
		BuildResAllDirPath (rootPath, dirSet);
		// 排序
		List<string> ret = new List<string> ();
		var iter = dirSet.GetEnumerator ();
		while (iter.MoveNext()) {
			ret.Add(iter.Current);
		}

		ret.Sort (OnDirSort);
		return ret;
	}*/

	private static List<string> GetResAllDirPath(List<string> rootDir)
		{
			if (rootDir == null || rootDir.Count <= 0)
				return null;
			List<string> ret = null;
			for (int i = 0; i < rootDir.Count; ++i) {
				List<string> list = AssetBundleBuild.GetAllLocalSubDirs (rootDir [i]);
				if (list != null && list.Count > 0)
				{
					if (ret == null)
						ret = new List<string>();
					ret.AddRange (list);
				}

				if (DirExistResource(rootDir[i]))
				{
					if (ret == null)
						ret = new List<string>();
					ret.Add(rootDir[i]);
				}
			}

			if (ret != null)
				ret.Sort(OnDirSort);

			return ret;
		}

	private static List<string> GetResAllDirPath()
	{
#if ASSETBUNDLE_ONLYRESOURCES
        List<string> ret = AssetBundleBuild.GetAllLocalSubDirs(cAssetsResourcesPath);
        if (DirExistResource(cAssetsResourcesPath))
        {
            if (ret == null)
                ret = new List<string>();
            ret.Add(cAssetsResourcesPath);
        }

        if (ret != null)
            ret.Sort(OnDirSort);
        return ret;
#else
		List<string> searchs = AssetBundleBuild.GetAllLocalSubDirs ("Assets/"); 
		//string[] searchs = AssetDatabase.GetAllAssetPaths ();//AssetDatabase.FindAssets ("Assets/Resources");
		if (searchs == null)
			return null;
		string searchStr = "/Resources/";
		HashSet<string> rootPathHash = new HashSet<string> ();
		for (int i = 0; i < searchs.Count; ++i) {
			string path = searchs[i] + "/";//AssetDatabase.GUIDToAssetPath(searchs[i]);
			//if (path.StartsWith("Assets/"))
			{
				int idx = path.LastIndexOf(searchStr);
				if (idx >= 0)
				{
					path = path.Substring(0, idx + searchStr.Length - 1);
					// Assets/Resources/
					if (!rootPathHash.Contains(path))
						rootPathHash.Add(path);
				}

			}
		}


		List<string> ret = new List<string> ();
		var iter = rootPathHash.GetEnumerator ();
		HashSet<string> dirSet = new HashSet<string> ();
		while (iter.MoveNext()) {
			string path = iter.Current;
			BuildResAllDirPath (path, dirSet);
		}

		iter = dirSet.GetEnumerator ();
		while (iter.MoveNext())
			ret.Add (iter.Current);
		ret.Sort (OnDirSort);

		return ret;
#endif
    }

	[MenuItem("Assets/打印依赖关系")]
	static void OnPrintDir()
	{
		List<string> dirList = GetResAllDirPath ();//GetResAllDirPath ("Assets/Resources");
        if (dirList == null)
        {
            Debug.Log("Resources res is None!");
            return;
        }
		// dirList.Add("Assets/Scene");
		mMgr.BuildDirs (dirList, true);
		mMgr.Print ();
	}

	private static string GetAndCreateDefaultOutputPackagePath(eBuildPlatform platform)
	{
		string ret = Path.GetDirectoryName(Application.dataPath) + "/output";
		
		if (!Directory.Exists (ret)) {
			DirectoryInfo info = Directory.CreateDirectory (ret);
			if (info == null)
				return null;
		}

		switch(platform)
		{
		case eBuildPlatform.eBuildAndroid:
			ret += "/Android";
			break;
		case eBuildPlatform.eBuildIOS:
			ret += "/IOS";
			break;
		case eBuildPlatform.eBuildMac:
			ret += "/Mac";
			break;
		case eBuildPlatform.eBuildWindow:
			ret += "/Windows";
			break;
		default:
			return null;
		}
		
		if (!Directory.Exists (ret)) {
			if (Directory.CreateDirectory (ret) == null)
				return null;
		}
		
		return ret;
	}

	private static string m_PackageVersion = string.Empty;
    private static string m_LastPackageVersion = string.Empty;
	// 当前版本号
	public static void GetPackageVersion(eBuildPlatform platform, out string currentVersion, out string lastVersion)
	{
		if (string.IsNullOrEmpty(m_PackageVersion))
		{
			string versionFile = "buildVersion.cfg";
            if (!System.IO.File.Exists(versionFile)) {
                m_PackageVersion = "1.000";
                m_LastPackageVersion = "1.000";
            } else {
                FileStream stream = new FileStream(versionFile, FileMode.Open, FileAccess.Read);
                try {
                    if (stream.Length <= 0) {
                        m_PackageVersion = "1.000";
                        m_LastPackageVersion = "1.000";
                    } else {
                        byte[] src = new byte[stream.Length];
                        stream.Read(src, 0, src.Length);
                        string ver = System.Text.Encoding.ASCII.GetString(src);
                        ver = ver.Trim();
                        if (string.IsNullOrEmpty(ver)) {
                            m_PackageVersion = "1.000";
                            m_LastPackageVersion = "1.000";
                        } else {
                            // 分号拆分
                            string[] splits = ver.Split(';');
                            if (splits == null || splits.Length <= 1) {
                                m_PackageVersion = ver;
                                m_LastPackageVersion = ver;
                            } else {
                                m_PackageVersion = splits[0];
                                m_LastPackageVersion = splits[1];
                            }
                        }
                    }
                } finally {
                    stream.Close();
                    stream.Dispose();
                }
            }
		}
        currentVersion = m_PackageVersion;
        lastVersion = m_LastPackageVersion;
    }

	public static string GetPackageExt(eBuildPlatform platform)
	{
		switch (platform) {
		case eBuildPlatform.eBuildAndroid:
			return ".apk";
		case eBuildPlatform.eBuildIOS:
			return ".ipa";
		default:
			return "";
		}
	}

	static private bool IsBuildNewPackageMode(eBuildPlatform platform, string outpath)
	{
		if (string.IsNullOrEmpty (outpath))
			return false;
		switch (platform) {
		case eBuildPlatform.eBuildIOS:
			string[] fileNames = Directory.GetFiles(outpath, "*.xcodeproj");
			return fileNames.Length <= 0;
		case eBuildPlatform.eBuildAndroid:
			return false;
		default:
			return true;
		}
	}

	static public void SetExternalABSplitDirs(List<PkgSplitABDirInfo> list)
	{
		mMgr.ExternSplitABDirs = list;
	}
	
	// 打AB根据路径列表，用于BuildPkg.txt中AssetBundles项
	static void BuildABFromPathList(string outPath, string[] list) {
			if (string.IsNullOrEmpty(outPath) || list == null || list.Length <= 0)
                return;

			List<string> resList = new List<string> ();
			for (int i = 0; i < list.Length; ++i) {
				string path = list[i];
                if (string.IsNullOrEmpty(path))
                    continue;

				var subList = AssetBundleBuild.GetAllLocalSubDirs(path);
				if (subList != null && subList.Count > 0)
					resList.AddRange (subList);

                if (DirExistResource(path)) {
                    resList.AddRange(list);
                }
            }

            if (resList.Count <= 0) {
                Debug.LogError("需打包的所有目录中不包含可打包的资源");
                return;
            }

			string targetStreamingAssetsPath = "Assets/StreamingAssets/";
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            eBuildPlatform platform;
            switch (buildTarget) {
                case BuildTarget.Android:
					platform = eBuildPlatform.eBuildAndroid;		
					targetStreamingAssetsPath += "Android";
                    break;
                case BuildTarget.iOS:
                    platform = eBuildPlatform.eBuildIOS;
					targetStreamingAssetsPath += "IOS";
                    break;
                case BuildTarget.StandaloneWindows:
                    platform = eBuildPlatform.eBuildWindow;
					targetStreamingAssetsPath += "Windows";
                    break;
                case BuildTarget.StandaloneWindows64:
                    platform = eBuildPlatform.eBuildWindow;
					targetStreamingAssetsPath += "Windows";
                    break;
                case BuildTarget.StandaloneOSXIntel:
                    platform = eBuildPlatform.eBuildMac;
					targetStreamingAssetsPath += "Mac";
                    break;
                case BuildTarget.StandaloneOSXIntel64:
                    platform = eBuildPlatform.eBuildMac;
					targetStreamingAssetsPath += "Mac";
                    break;
                default:
                    return;
            }

			// Delete outPath StreaingAssets subDirs
			targetStreamingAssetsPath = outPath + '/' + targetStreamingAssetsPath;
			if (System.IO.Directory.Exists (targetStreamingAssetsPath)) {
				string[] subDirs = System.IO.Directory.GetDirectories (targetStreamingAssetsPath);
				if (subDirs != null) {
					for (int i = 0; i < subDirs.Length; ++i) {
						System.IO.Directory.Delete (subDirs [i], true);
					}
				}

				string[] subFiles = System.IO.Directory.GetFiles (targetStreamingAssetsPath);
				if (subFiles != null) {
					for (int i = 0; i < subFiles.Length; ++i) {
						System.IO.File.Delete (subFiles [i]);
					}
				}
			} else {
				System.IO.Directory.CreateDirectory(targetStreamingAssetsPath);
			}

			List<string> buildList = GetResAllDirPath (resList);

			// 开始打包
			mMgr.BuildDirs(buildList);
			mMgr.BuildAssetBundles(platform, 2, true, null);
            mMgr.LocalAssetBundlesCopyToOtherProj("outPath/Proj", platform);
        }
		
		public static void BuildFromBuildPkg()
		{
			// 判斷目錄是否存在
			string dir = System.IO.Path.GetFullPath ("outPath");
			if (!Directory.Exists (dir)) {
				if (Directory.CreateDirectory (dir) == null)
					return;
			}

			// 1.生成新工程
			string outPath = "outPath/Proj";
			string allNewProjPath = System.IO.Path.GetFullPath(outPath);
			if (!System.IO.Directory.Exists(allNewProjPath)) {
				// Create Unity Project
#if UNITY_EDITOR_WIN
				RunCmd("Unity.exe -quit -batchmode -nographics -createProject " + allNewProjPath);
#elif UNITY_EDITOR_OSX
				RunCmd("/Application/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -nographics -createProject " + allNewProjPath);
#endif
			}

			// 读取配置文件
			string pkgFileName = "BuildPkg.txt";
			pkgFileName = System.IO.Path.GetFullPath (pkgFileName);
			if (!File.Exists (pkgFileName)) {
				Debug.LogError ("BuildPkg.txt配置文件不存在");
				return;
			}
			BuildPkg pkgCfg = new BuildPkg();
			if (!pkgCfg.LoadFromFile (pkgFileName)) {
				Debug.LogError ("BuildPkg.txt解析错误，请检查");
				return;
			}

            // 设置外部Split目录信息
            SetExternalABSplitDirs(pkgCfg.SplitABDirs);

            // 2.SVN更新不打包美术资源
            if (pkgCfg.Svns != null && pkgCfg.Svns.Length > 0)
				Cmd_Svn (outPath, new List<string> (pkgCfg.Svns));

            // 3.Copy中Resources非打包资源
			if (pkgCfg.Copys != null && pkgCfg.Copys.Length > 0)
				Cmd_CopyList(outPath, new List<string>(pkgCfg.Copys));
            // 4.将AB丢进新工程
			BuildABFromPathList(outPath, pkgCfg.AssetBundles);
            string md5Find = "Assets/md5Find.txt";
            File.Copy(md5Find, outPath + '/' + md5Find, true);
            // 5.生成平台包
		}

	static public void BuildPlatform(eBuildPlatform platform, int compressType = 0, bool isMd5 = false, 
									 bool isForceAppend = false)
	{
		// GetResAllDirPath ();
		// 编译平台`
		m_PackageVersion = string.Empty;
        m_LastPackageVersion = string.Empty;
        List<string> resList = GetResAllDirPath();
		// resList.Add("Assets/Scene");
		mMgr.BuildDirs(resList);
		mMgr.BuildAssetBundles(platform, compressType, isMd5, null, isForceAppend);

        mMgr.LocalAssetBundlesCopyToOtherProj("outPath/Proj", platform);
        /*
		string outpath = GetAndCreateDefaultOutputPackagePath (platform);
		string outFileName = outpath + "/" + GetCurrentPackageVersion (platform);
		if (!mMgr.BuildPackage (platform, outFileName, IsBuildNewPackageMode(platform, outpath)))
			LogMgr.Instance.LogError ("BuildPlatform: BuildPackage error!");*/
    }

	internal static AssetBunbleInfo FindAssetBundle(string fileName)
	{
		return mMgr.FindAssetBundle (fileName);
	}

	[MenuItem("Assets/平台打包/Windows(非压缩)")]
	static public void OnBuildPlatformWindowsNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow);
	}

	[MenuItem("Assets/平台打包/Windows MD5(非压缩)")]
	static public void OnBuildPlatformWindowsNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow, 0, true);
	}

	[MenuItem("Assets/平台打包/OSX(非压缩)")]
	static public void OnBuildPlatformOSXNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildMac);
	}

	[MenuItem("Assets/平台打包/OSX MD5(非压缩)")]
	static public void OnBuildPlatformOSXNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildMac, 0, true);
	}

	[MenuItem("Assets/平台打包/Android(非压缩)")]
	static public void OnBuildPlatformAndroidNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid);
	}

	[MenuItem("Assets/平台打包/Android MD5(非压缩)")]
	static public void OnBuildPlatformAndroidNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid, 0, true);
	}

	[MenuItem("Assets/平台打包/IOS(非压缩)")]
	static public void OnBuildPlatformIOSNoCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS);
	}

	[MenuItem("Assets/平台打包/IOS MD5(非压缩)")]
	static public void OnBuildPlatformIOSNoCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS, 0, true);
	}

	[MenuItem("Assets/平台打包/----------")]
	static public void OnBuildPlatformNone()
	{
	}

	[MenuItem("Assets/平台打包/----------", true)]
	static bool CanBuildPlatformNone()
	{
		return false;
	}

	[MenuItem("Assets/平台打包/Windows(压缩)")]
	static public void OnBuildPlatformWindowsCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow, 1);
	}

	[MenuItem("Assets/平台打包/Windows MD5(压缩)")]
	static public void OnBuildPlatformWindowsCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildWindow, 1, true);
	}
	
	[MenuItem("Assets/平台打包/OSX(压缩)")]
	static public void OnBuildPlatformOSXCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildMac, 1);
	}

	[MenuItem("Assets/平台打包/OSX MD5(压缩)")]
	static public void OnBuildPlatformOSXCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildMac, 1, true);
	}
	
	[MenuItem("Assets/平台打包/Android(压缩)")]
	static public void OnBuildPlatformAndroidCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildAndroid, 1);
	}

	[MenuItem("Assets/平台打包/Android MD5(压缩)")]
	static public void OnBuildPlatformAndroidCompressMd5()
	{
		BuildPlatform(eBuildPlatform.eBuildAndroid, 1, true);
	}
	
	[MenuItem("Assets/平台打包/IOS(压缩)")]
	static public void OnBuildPlatformIOSCompress()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS, 1);
		//UnityEditor.EditorUserBuildSettings.SetBuildLocation
	}

	[MenuItem("Assets/平台打包/IOS MD5(压缩)")]
	static public void OnBuildPlatformIOSCompressMd5()
	{
		BuildPlatform (eBuildPlatform.eBuildIOS, 1, true);
	}

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6

    [MenuItem("Assets/平台打包/-----------")]
	static public void OnBuildPlatformNone1() {
	}

	[MenuItem("Assets/平台打包/-----------", true)]
	static bool CanBuildPlatformNone1() {
		return false;
	}

	[MenuItem("Assets/平台打包/Windows(Lz4)")]
	static public void OnBuildPlatformWinLz4() {
		BuildPlatform(eBuildPlatform.eBuildWindow, 2);
	}

    [MenuItem("Assets/平台打包/增量Windows(Lz4)")]
    static public void OnAppendBuildPlatformWinLz4() {
        BuildPlatform(eBuildPlatform.eBuildWindow, 2, false, true);
    }

    [MenuItem("Assets/平台打包/Windows Md5(Lz4)")]
	static public void OnBuildPlatformWinLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildWindow, 2, true);
	}

	[MenuItem("Assets/平台打包/增量Windows Md5(Lz4)")]
	static public void OnAppendBuildPlatformWinLz4Md5()
	{
		BuildPlatform(eBuildPlatform.eBuildWindow, 2, true, true);
	}

	[MenuItem("Assets/平台打包/OSX(Lz4)")]
	static public void OnBuildPlatformOSXLz4() {
		BuildPlatform(eBuildPlatform.eBuildMac, 2);
	}

	[MenuItem("Assets/平台打包/OSX MD5(Lz4)")]
	static public void OnBuildPlatformOSXLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildMac, 2, true);
	}

    [MenuItem("Assets/平台打包/增量OSX MD5(Lz4)")]
    static public void OnAppendBuildPlatformOSXLz4Md5()
    {
        BuildPlatform (eBuildPlatform.eBuildMac, 2, true, true);
    }

	[MenuItem("Assets/平台打包/Android(Lz4)")]
	static public void OnBuildPlatformAndroidLz4() {
		BuildPlatform(eBuildPlatform.eBuildAndroid, 2);
	}

	[MenuItem("Assets/平台打包/Android MD5(Lz4)")]
	static public void OnBuildPlatformAndroidLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildAndroid, 2, true);
	}

	[MenuItem("Assets/平台打包/增量Android MD5(Lz4)")]
	static public void OnAppendBuildPlatformAndroidLz4Md5()
	{
		BuildPlatform(eBuildPlatform.eBuildAndroid, 2, true, true);
	}

	[MenuItem("Assets/平台打包/IOS(Lz4)")]
	static public void OnBuildPlatformIOSLz4() {
		BuildPlatform(eBuildPlatform.eBuildIOS, 2);
		//UnityEditor.EditorUserBuildSettings.SetBuildLocation
	}

	[MenuItem("Assets/平台打包/IOS MD5(Lz4)")]
	static public void OnBuildPlatformIOSLz4Md5() {
		BuildPlatform(eBuildPlatform.eBuildIOS, 2, true);
	}

    [MenuItem("Assets/平台打包/增量IOS MD5(Lz4)")]
    static public void OnAppendBuildPlatformIOSLz4Md5()
    {
        BuildPlatform (eBuildPlatform.eBuildIOS, 2, true, true);
    }

#endif

    /* 真正打包步骤 */
    /*
     * 1.判断目录是否为空，否则生成新工程
     * 2.拷贝非资源的文件
     * 3.打包原工程的资源到AB
     * 4.新工程生成APK
     */

    static void _CopyAllDirs(string dir, string outPath, List<string> resPaths)
    {
		if (resPaths != null)
		{
        	for (int j = 0; j < resPaths.Count; ++j)
        	{
           	 	string resPath = resPaths[j];
            	if (string.IsNullOrEmpty(resPath))
                	continue;
				int idx = dir.IndexOf(resPath, StringComparison.CurrentCultureIgnoreCase);
				if (idx >= 0)
                	return;
        	}
		}

		string newDir;
		if (!outPath.EndsWith(dir, StringComparison.CurrentCultureIgnoreCase))
		{
       		string subDir = System.IO.Path.GetFileName(dir);
        	newDir = outPath + '/' + subDir;
        	if (!System.IO.Directory.Exists(newDir))
        	{
            	System.IO.DirectoryInfo dstDirInfo = System.IO.Directory.CreateDirectory(newDir);
            	if (dstDirInfo == null)
                	return;
       	 	}
		} else
			newDir = outPath;

        string[] fileNames = System.IO.Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
        if (fileNames != null)
        {
            for (int i = 0; i < fileNames.Length; ++i)
            {
                string fileName = fileNames[i];
				fileName = fileName.Replace('\\', '/');
			//	string ext = System.IO.Path.GetExtension(fileName);
			//	if (string.Compare(ext, ".meta", StringComparison.CurrentCultureIgnoreCase) == 0)
			//		continue;
                string dstFileName = newDir + '/' + System.IO.Path.GetFileName(fileName);
                if (File.Exists(dstFileName))
                    File.Delete(dstFileName);
                System.IO.File.Copy(fileName, dstFileName);
            }
        }

        string[] subDirs = System.IO.Directory.GetDirectories(dir);
        if (subDirs != null)
        {
            for (int i = 0; i < subDirs.Length; ++i)
            {
				string sub = subDirs[i];
				sub = sub.Replace('\\', '/');
				_CopyAllDirs(sub, newDir, resPaths);
            }
        }
    }

	// 后面可以考虑加版本好
	//[MenuItem("Assets/測試/APK")]
	static public void Cmd_Apk()
	{
		// Import Package
	//	Cmd_OtherImportAssetRootFiles("..");
	//	Cmd_RemoveAssetRootFiles("..");
		string apkName = "../" + DateTime.Now.ToString("yyyy_MM_dd[HH_mm_ss]") + ".apk";
		apkName = System.IO.Path.GetFullPath(apkName);
		Debug.Log("Build APK: " + apkName);
		mMgr.BuildPackage(eBuildPlatform.eBuildAndroid, apkName, true); 
	}

	//[MenuItem("Assets/測試/IOS")]
	static public void Cmd_IOS()
	{
		string xcodeProj = "../IOS_Build";
		xcodeProj = System.IO.Path.GetFullPath(xcodeProj);
		if (Directory.Exists (xcodeProj)) {
			DeleteDirectorAndFiles (xcodeProj);
		}
		Directory.CreateDirectory (xcodeProj);

		Debug.Log ("Build XCode: " + xcodeProj);
		mMgr.BuildPackage(eBuildPlatform.eBuildIOS, xcodeProj, true); 
	}

	static public void Cmd_Mac()
	{
		string macOutPath = "../Mac_Build";
		macOutPath = System.IO.Path.GetFullPath(macOutPath);
		if (Directory.Exists (macOutPath)) {
			DeleteDirectorAndFiles (macOutPath);
		}
		Directory.CreateDirectory (macOutPath);
		macOutPath += "/client";
		Debug.Log ("Build Mac: " + macOutPath);
		mMgr.BuildPackage(eBuildPlatform.eBuildMac, macOutPath, true);
	}

	static public void Cmd_Win()
	{
		string winOutPath = "../Win_Build";
		winOutPath = System.IO.Path.GetFullPath (winOutPath);
		if (Directory.Exists (winOutPath)) {
			DeleteDirectorAndFiles (winOutPath);
		}
		Directory.CreateDirectory (winOutPath);

		string winApp = winOutPath + "/client.exe";
		winApp = System.IO.Path.GetFullPath(winApp);
		Debug.Log("Build WIN: " + winApp);
		mMgr.BuildPackage(eBuildPlatform.eBuildWindow, winApp, true); 
	}

	static public void Cmd_Win_Debug()
	{
		string winOutPath = "../Win_Build";
		winOutPath = System.IO.Path.GetFullPath (winOutPath);
		if (Directory.Exists (winOutPath)) {
			DeleteDirectorAndFiles (winOutPath);
		}
		Directory.CreateDirectory (winOutPath);

		string winApp = winOutPath + "/client.exe";
		Debug.Log("Build WIN: " + winApp);
		mMgr.BuildPackage(eBuildPlatform.eBuildWindow, winApp, true, true, true, true); 
	}

	[MenuItem("Assets/发布/Win32(非压缩)")]
	static public void Cmd_BuildWin32_NoCompress()
	{
		Cmd_Build(0, true, eBuildPlatform.eBuildWindow);
	}

	[MenuItem("Assets/发布/Win32_Debug(非压缩)")]
	static public void Cmd_BuidWin32_Debug_NoCompress()
	{
		Cmd_Build(0, true, eBuildPlatform.eBuildWindow, true);
	}

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
    [MenuItem("Assets/发布/Win32_Debug(Lz4)")]
	static public void Cmd_BuidWin32_Debug_Lz4()
	{
		Cmd_Build(2, true, eBuildPlatform.eBuildWindow, true);
	}
#endif

    [MenuItem("Assets/发布/Win32(压缩)")]
    static public void Cmd_BuildWin32_Compress()
    {
        Cmd_Build(1, true, eBuildPlatform.eBuildWindow);
    }

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
    [MenuItem("Assets/发布/Win32(Lz4)")]
	static public void Cmd_BuildWin32_Lz4() {
		Cmd_Build(2, true, eBuildPlatform.eBuildWindow);
	}
#endif

    [MenuItem("Assets/编译CSharp")]
    static public void Cmd_BuildCSharpProj() {
        /*
		string unityEditorPath = mMgr.GetUnityEditorPath();
		if (string.IsNullOrEmpty(unityEditorPath))
			return;

		string buildExe = unityEditorPath + "/Data/MonoBleedingEdge/lib/mono/unity/xbuild.exe";
		*/
        string buildExe = "xbuild.bat";

        string rootPath = System.IO.Directory.GetCurrentDirectory();
        rootPath = rootPath.Replace('\\', '/');
        string[] csPrjs = new string[1];
        csPrjs[0] = "Assembly-CSharp.csproj";

        for (int i = 0; i < csPrjs.Length; ++i) {
            string projFileName = rootPath + '/' + csPrjs[i];
            mMgr.BuildCSharpProject(projFileName, buildExe);
        }
    }

    static public void Cmd_Build_Copy()
    {
    	string outPath = "outPath/Proj";
		
		string searchProjPath = System.IO.Path.GetFullPath(outPath);
		if (!System.IO.Directory.Exists(searchProjPath))
			return;

		// 如果后面COPY慢，可以从SVN Download(不会每次都有更新)
		List<string> resPaths = new List<string>();
		resPaths.Add("Assets/Resources");
		resPaths.Add("Assets/StreamingAssets/Android");
		resPaths.Add("Assets/StreamingAssets/IOS");
		resPaths.Add("Assets/StreamingAssets/Windows");
		//	resPaths.Add("Library/metadata");
		//resPaths.Add("Assets/Plugs");
		Cmd_CopyOther(outPath, resPaths);

		// Delete outPath StreaingAssets subDirs
		string targetStreamingAssetsPath = outPath + '/' + "Assets/StreamingAssets";
		if (System.IO.Directory.Exists(targetStreamingAssetsPath))
		{
			string[] subDirs = System.IO.Directory.GetDirectories(targetStreamingAssetsPath);
			if (subDirs != null)
			{
				for (int i = 0; i < subDirs.Length; ++i)
				{
					System.IO.Directory.Delete(subDirs[i], true);
				}
			}
		} else
		{
			System.IO.Directory.CreateDirectory(targetStreamingAssetsPath);
		}
    }

	static private void Cmd_Build_AB(eBuildPlatform platform, int compressType, string outPath, bool isAppend)
	{
		string targetStreamingAssetsPath = outPath + '/' + "Assets/StreamingAssets";
		string searchProjPath = System.IO.Path.GetFullPath(outPath);
		if (!System.IO.Directory.Exists (searchProjPath)) {
			System.IO.Directory.CreateDirectory (searchProjPath);
		}
            
	    BuildPlatform (platform, compressType, true, isAppend);
		// 处理Manifest
		string rootManifest = targetStreamingAssetsPath;
		string copyManifest = "Assets/StreamingAssets";
		switch (platform) {
		case eBuildPlatform.eBuildAndroid:
			rootManifest += "/Android";
			copyManifest += "/Android";
			break;
		case eBuildPlatform.eBuildIOS:
			rootManifest += "/IOS";
			copyManifest += "/IOS";
			break;
		case eBuildPlatform.eBuildMac:
			rootManifest += "/Mac";
			copyManifest += "/Mac";
			break;
		case eBuildPlatform.eBuildWindow:
			rootManifest += "/Windows";
			copyManifest += "/Windows";
			break;
		}

		/*
        if (isAppendBuild) {
            if (!Directory.Exists (copyManifest))
                Directory.CreateDirectory (copyManifest);
            string[] files = Directory.GetFiles (rootManifest, "*.*", SearchOption.TopDirectoryOnly);
            if (files != null) {
                for (int i = 0; i < files.Length; ++i) {
                    string fileName = Path.GetFileName (files [i]);
                    string newFilePath = string.Format ("{0}/{1}", copyManifest, fileName);
                    File.Copy (files [i], newFilePath, true);
                }
            }
        }*/

		mMgr.RemoveBundleManifestFiles_5_x (rootManifest);
	}

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
    static public void Cmd_Build_Android_ABLz4_Append()
    {
		string outPath = "outPath/Proj";
		Cmd_Build_AB (eBuildPlatform.eBuildAndroid, 2, outPath, true);
    }

	static public void Cmd_Build_IOS_ABLz4_Append()
	{
		string outPath = "outPath/Proj";
		Cmd_Build_AB (eBuildPlatform.eBuildIOS, 2, outPath, true);
	}

	static public void Cmd_Build_Win_ABLz4_Append()
	{
		string outPath = "outPath/Proj";
		Cmd_Build_AB (eBuildPlatform.eBuildWindow, 2, outPath, true);
	}

	static public void Cmd_Build_Mac_ABLz4_Append()
	{
		string outPath = "outPath/Proj";
		Cmd_Build_AB (eBuildPlatform.eBuildMac, 2, outPath, true);
	}
#endif

    static private void Cmd_Build(int compressType, bool isMd5, eBuildPlatform platform, bool isDebug = false)
	{
		string outPath = "outPath/Proj";
		
		string searchProjPath = System.IO.Path.GetFullPath(outPath);
		if (!System.IO.Directory.Exists(searchProjPath))
		{
			// Create Unity Project
#if UNITY_EDITOR_WIN
			RunCmd("Unity.exe -quit -batchmode -nographics -createProject " + searchProjPath);
#endif
		}
			
		// 如果后面COPY慢，可以从SVN Download(不会每次都有更新)
		List<string> resPaths = new List<string>();
		resPaths.Add("Assets/Resources");
		resPaths.Add("Assets/StreamingAssets/Android");
		resPaths.Add("Assets/StreamingAssets/IOS");
		resPaths.Add("Assets/StreamingAssets/Windows");
		//	resPaths.Add("Library/metadata");
		//resPaths.Add("Assets/Plugs");
		Cmd_CopyOther(outPath, resPaths);

		// Delete outPath StreaingAssets subDirs
		string targetStreamingAssetsPath = outPath + '/' + "Assets/StreamingAssets";
		if (System.IO.Directory.Exists(targetStreamingAssetsPath))
		{
			string[] subDirs = System.IO.Directory.GetDirectories(targetStreamingAssetsPath);
			if (subDirs != null)
			{
				for (int i = 0; i < subDirs.Length; ++i)
				{
					System.IO.Directory.Delete(subDirs[i], true);
				}
			}
		} else
		{
			System.IO.Directory.CreateDirectory(targetStreamingAssetsPath);
		}

		// 增量
		// build AssetsBundle to Target

		BuildPlatform(platform, compressType, isMd5); 
		// 处理Manifest
		string rootManifest = targetStreamingAssetsPath;
		string copyManifest = "Assets/StreamingAssets";
		switch (platform) {
		case eBuildPlatform.eBuildAndroid:
			rootManifest += "/Android";
			copyManifest += "/Android";
			break;
		case eBuildPlatform.eBuildIOS:
			rootManifest += "/IOS";
			copyManifest += "/IOS";
			break;
		case eBuildPlatform.eBuildMac:
			rootManifest += "/Mac";
			copyManifest += "/Mac";
			break;
		case eBuildPlatform.eBuildWindow:
			rootManifest += "/Windows";
			copyManifest += "/Windows";
			break;
		}

		/*
        if (isAppendBuild) {
            if (!Directory.Exists (copyManifest))
                Directory.CreateDirectory (copyManifest);
            string[] files = Directory.GetFiles (rootManifest, "*.*", SearchOption.TopDirectoryOnly);
            if (files != null) {
                for (int i = 0; i < files.Length; ++i) {
                    string fileName = Path.GetFileName (files [i]);
                    string newFilePath = string.Format ("{0}/{1}", copyManifest, fileName);
                    File.Copy (files [i], newFilePath, true);
                }
            }
        }*/

		mMgr.RemoveBundleManifestFiles_5_x (rootManifest);

		string logFileName = string.Empty;
		string funcName = string.Empty;
		if (platform == eBuildPlatform.eBuildAndroid)
		{
			// Copy 渠道包
		
			// 新工程生成APK Path=outPath/XXX.apk
			funcName = "AssetBundleBuild.Cmd_Apk";
            logFileName = System.IO.Path.GetDirectoryName(searchProjPath) + '/' + "apkLog.txt";
        }
        else if (platform == eBuildPlatform.eBuildWindow)
        {
            logFileName = System.IO.Path.GetDirectoryName(searchProjPath) + '/' + "winLog.txt";
            if (isDebug)
				funcName = "AssetBundleBuild.Cmd_Win_Debug";
			else
				funcName = "AssetBundleBuild.Cmd_Win";
        }

		if (!string.IsNullOrEmpty(funcName))
		{
#if UNITY_EDITOR_WIN
			string cmdApk = string.Format("Unity.exe -quit -batchmode -nographics -executeMethod {0} -logFile {1} -projectPath {2}", 
			                              funcName, logFileName, searchProjPath);
			RunCmd(cmdApk);
#endif
		}
	}

    [MenuItem("Assets/发布/APK_整包(非压缩AB)")]
    static public void Cmd_BuildAPK_NoCompress()
    {
		Cmd_Build(0, true, eBuildPlatform.eBuildAndroid);
    }

    [MenuItem("Assets/发布/APK_整包(压缩AB)")]
    static public void Cmd_BuildAPK_Compress()
    {
        Cmd_Build(1, true, eBuildPlatform.eBuildAndroid);
    }

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6
    [MenuItem("Assets/发布/APK_整包(Lz4)")]
	static public void Cmd_BuildAPK_Lz4() {
		Cmd_Build(2, true, eBuildPlatform.eBuildAndroid);
	}

    [MenuItem("Assets/发布/APK_整包增量(Lz4)")]
    static public void Cmd_AppendBuildAPK_Lz4()
    {
        Cmd_Build(2, true, eBuildPlatform.eBuildAndroid, false);
    }

	[MenuItem("Assets/发布/APK_Debug(Lz4)")]
	static public void Cmd_BuildAPK_Debug_Lz4()
	{
		Cmd_Build(2, true, eBuildPlatform.eBuildAndroid, true);
	}
#endif

	[MenuItem("Assets/发布/APK_Debug(非压缩)")]
	static public void Cmd_BuildAPK_DEBUG_UNCOMPRESS()
	{
		Cmd_Build(0, true, eBuildPlatform.eBuildAndroid, true);
	}

	public static void RunCmd(string command)
	{

		if (string.IsNullOrEmpty(command))
			return;
#if UNITY_EDITOR_WIN
		command = " /c " + command;
		processCommand("cmd.exe", command);
#elif UNITY_EDITOR_OSX
		processCommand(command, string.Empty);
#endif
	}

	private static void processCommand(string command, string argument){
		System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo(command);
		start.Arguments = argument;
		start.CreateNoWindow = false;
		start.ErrorDialog = true;
	    start.UseShellExecute = true;
	//	start.UseShellExecute = false;
		
		if(start.UseShellExecute){
			start.RedirectStandardOutput = false;
			start.RedirectStandardError = false;
			start.RedirectStandardInput = false;
		} else{
			start.RedirectStandardOutput = true;
			start.RedirectStandardError = true;
			start.RedirectStandardInput = true;
		//	start.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
		//	start.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
			start.StandardOutputEncoding = System.Text.Encoding.Default;
			start.StandardErrorEncoding = System.Text.Encoding.Default;
		}
		
		System.Diagnostics.Process p = System.Diagnostics.Process.Start(start);
		
		if(!start.UseShellExecute){
			Exec_Print(p.StandardOutput, false);
			Exec_Print(p.StandardError, true);
		}

		p.WaitForExit();
		p.Close();
	}

	private static void Exec_Print(StreamReader reader, bool isError)
	{
		if (reader == null)
			return;

		string str = reader.ReadToEnd();

		if (!string.IsNullOrEmpty(str))
		{
			if (isError)
				Debug.LogError(str);
			else
				Debug.Log(str);
		}

		reader.Close();
	}

	static private void _CopyAllFiles(string srcPath, string dstPath, List<string> resPaths)
	{
		if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstPath))
			return;
		string[] dirs = System.IO.Directory.GetDirectories(srcPath);
		if (dirs != null)
		{
			for (int i = 0; i < dirs.Length; ++i)
			{
				string dir = dirs[i];
				dir = dir.Replace('\\', '/');
				_CopyAllDirs(dir, dstPath, resPaths);
			}
		}

		string[] srcRootFiles = System.IO.Directory.GetFiles(srcPath, "*.*", SearchOption.TopDirectoryOnly);
		if (srcRootFiles != null)
		{
			for (int i = 0; i < srcRootFiles.Length; ++i)
			{
				string srcFilePath = srcRootFiles[i];
				string srcFileName = System.IO.Path.GetFileName(srcFilePath);
				string dstFilePath = dstPath + '/' + srcFileName;
				System.IO.File.Copy(srcFilePath, dstFilePath, true);
			}
		}
	} 

	static private void Cmd_Svn(string outPath, List<string> resPaths)
	{
		if (string.IsNullOrEmpty (outPath) || resPaths == null || resPaths.Count <= 1)
				return;
			string url = resPaths [0].Trim ();
			if (string.IsNullOrEmpty (url))
				return;
			
			// SVN更新
			for (int i = 1; i < resPaths.Count; ++i) {
				string path = string.Format ("{0}/{1}", outPath, resPaths [i]);
				path = Path.GetFullPath (path);
				if (Directory.Exists (path)) {
					// svn update
#if UNITY_EDITOR_WIN
					string cmd = string.Format("TortoiseProc.exe /command:update /path:\"{0}\" /closeonend:3", path);
					RunCmd(cmd);
#endif
				} else {
					// svn checkout
#if UNITY_EDITOR_WIN
					string cmd = string.Format("TortoiseProc.exe /command:checkout /path:\"{0}\" /url:\"{1}/{2}\"", path, url, resPaths[i]);
					RunCmd(cmd);
#endif
				}
			}
	}

	static private void _CreateDirs(string dirs)
	{
		if (string.IsNullOrEmpty(dirs))
			return;
		string[] dd = dirs.Split('/');
		if (dd == null || dd.Length <= 0)
		{
			if (!Directory.Exists(dirs))
				Directory.CreateDirectory(dirs);
			return;
		}

		string root = string.Empty;
		for (int i = 0; i < dd.Length; ++i)
		{
			string d = dd[i];
			if (string.IsNullOrEmpty(d))
				continue;
			if (string.IsNullOrEmpty(root))
				root = d;
			else
				root = '/' + d;
			if (!Directory.Exists(root))
				Directory.CreateDirectory(root);
		}
	}

    static internal void DeleteDirectorAndFiles(string dir, bool isDebugLog = false) {
        if (string.IsNullOrEmpty(dir))
            return;
        if (Directory.Exists(dir)) {
            var subDirs = System.IO.Directory.GetDirectories(dir);
            if (subDirs != null) {
                for (int i = 0; i < subDirs.Length; ++i) {
                    string subDir = subDirs[i];
                    DeleteDirectorAndFiles(subDir, isDebugLog);
                }
            }

            var subFiles = System.IO.Directory.GetFiles(dir);
            if (subFiles != null) {
                for (int j = 0; j < subFiles.Length; ++j) {
                    if (isDebugLog)
                        Debug.LogFormat("正在删除文件{0}", subFiles[j]);
                    System.IO.File.Delete(subFiles[j]);
                }
            }

            if (isDebugLog)
                Debug.LogFormat("正在删除目录{0}", dir);
            System.IO.Directory.Delete(dir);
        }
    }

    static private void Cmd_CopyList(string outPath, List<string> copyList)
		{
			if (string.IsNullOrEmpty(outPath) || copyList == null || copyList.Count <= 0)
				return;

			string dstAssets = outPath + "/Assets";
			if (!System.IO.Directory.Exists(dstAssets)) {
				if (System.IO.Directory.CreateDirectory(dstAssets) == null)
					return;
			}

			for (int i = 0; i < copyList.Count; ++i) {
				string dir = copyList [i];
				string dstDir = Path.GetFullPath(outPath + '/' + dir);
				if (Directory.Exists (dstDir)) {
					var subDirs = System.IO.Directory.GetDirectories (dstDir);
					if (subDirs != null) {
						for (int j = 0; j < subDirs.Length; ++j) {
							DeleteDirectorAndFiles(subDirs[j]);
						}
					}

					var subFiles = System.IO.Directory.GetFiles (dstDir);
					if (subFiles != null) {
						for (int j = 0; j < subFiles.Length; ++j) {
							System.IO.File.Delete (subFiles [j]);
						}
					}
				}

				dir = dir.Replace('\\', '/');

			int idx = dir.IndexOf("Assets/");
			if (idx >= 0)
			{
				dstAssets = outPath + "/Assets/" + dir.Substring(idx + 1);
				_CreateDirs(dstAssets);
			} else
				dstAssets = outPath + "/Assets";

				_CopyAllDirs(dir, dstAssets, null);
			}

			dstAssets = outPath +  "/ProjectSettings";
			_CopyAllFiles("ProjectSettings", dstAssets, null);
		}

    // 拷贝非资源文件夹
    // resPaths: 资源目录列表
    // outPath: 目录
    static private void Cmd_CopyOther(string outPath, List<string> resPaths)
    {
        if (string.IsNullOrEmpty(outPath))
            return;
        var dirs = System.IO.Directory.GetDirectories("Assets");
        if (dirs == null || dirs.Length <= 0)
            return;

        string dstAssets = outPath + '/' + "Assets";
        if (!System.IO.Directory.Exists(dstAssets))
        {
            if (System.IO.Directory.CreateDirectory(dstAssets) == null)
                return;
        }

		var delDirs = System.IO.Directory.GetDirectories(dstAssets);
		if (delDirs != null)
		{
			for (int i = 0; i < delDirs.Length; ++i)
			{
				System.IO.Directory.Delete(delDirs[i], true);
			}
		}

		var delFiles = System.IO.Directory.GetFiles(dstAssets);
		if (delFiles != null)
		{
			for (int i = 0; i < delFiles.Length; ++i)
			{
				System.IO.File.Delete(delFiles[i]);
			}
		}

		var srcRootFiles = System.IO.Directory.GetFiles("Assets", "*.*", SearchOption.TopDirectoryOnly);
		if (srcRootFiles != null)
		{
			for (int i = 0; i < srcRootFiles.Length; ++i)
			{
				string srcFilePath = srcRootFiles[i];
				string srcFileName = System.IO.Path.GetFileName(srcFilePath);
				string dstFilePath = dstAssets + '/' + srcFileName;
				System.IO.File.Copy(srcFilePath, dstFilePath, true);
			}
		}

		// Copy DstAssets

        for (int i = 0; i < dirs.Length; ++i)
        {
            string dir = dirs[i];
			dir = dir.Replace('\\', '/');
			_CopyAllDirs(dir, dstAssets, resPaths);
        }

	//	dstAssets = outPath + '/' + "Library";
	//	_CopyAllFiles("Library", dstAssets, null);

		dstAssets = outPath + '/' + "ProjectSettings";
		_CopyAllFiles("ProjectSettings", dstAssets, null);
    }

	private static void Cmd_RemoveAssetRootFiles(string outPath)
	{
		string tempPacketPath = outPath + "/Temp.unitypackage";
		if (System.IO.File.Exists(tempPacketPath))
			System.IO.File.Delete(tempPacketPath);
	}

	private static void Cmd_OtherImportAssetRootFiles(string outPath)
	{
		// Import unitypackage to New Project
		string tempPacketPath = outPath + "/Temp.unitypackage";
		if (!System.IO.File.Exists(tempPacketPath))
			return;
		AssetDatabase.ImportPackage(tempPacketPath, false);
		AssetDatabase.Refresh();
	}

	private static void Cmd_ExportAssetRootFiles(string outPath)
	{
		var topFiles = System.IO.Directory.GetFiles("Assets", "*.*", SearchOption.TopDirectoryOnly);
		List<string> packetFileList = new List<string>();
		if (topFiles != null)
		{
		//	string dstTopFilePath = outPath + "/Assets";
			for (int i = 0; i < topFiles.Length; ++i)
			{
				string filePath = topFiles[i];
			//	string fileName = System.IO.Path.GetFileName(filePath);
			//	string ext = System.IO.Path.GetExtension(fileName);
			//	if (string.Compare(ext, ".meta", StringComparison.CurrentCultureIgnoreCase) == 0)
			//		continue;

				string packetSubFileName = AssetBundleMgr.GetAssetRelativePath(filePath);
				packetFileList.Add(packetSubFileName);
			//	string dstFilePath = dstTopFilePath + '/' + fileName;
			//	System.IO.File.Copy(filePath, dstFilePath, true);
			}
		}

		if (packetFileList.Count > 0)
		{
			string tempPacketFile = "outPath/Temp.unitypackage";
			if (System.IO.File.Exists(tempPacketFile))
				System.IO.File.Delete(tempPacketFile);
			string[] allFiles = AssetDatabase.GetDependencies(packetFileList.ToArray());
			AssetDatabase.ExportPackage(allFiles, tempPacketFile);
		}
	}

	public static void AddShowTagProcess(string tagName)
	{
		if (mMgr.MaxTagFileCount <= 0)
			return;
		mMgr.CurTagIdx += 1;
		float maxCnt = mMgr.MaxTagFileCount;
		float curIdx = mMgr.CurTagIdx;
		float process = curIdx/maxCnt;
		EditorUtility.DisplayProgressBar("设置Tag中...", tagName, process);
	}

#if UNITY_5

	[MenuItem("Assets/清理所有AssetBundle的Tag")]
	public static void ClearAllAssetNames()
	{
		string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
		if (assetBundleNames == null || assetBundleNames.Length <= 0)
			return;
		for (int i = 0; i <assetBundleNames.Length; ++i)
		{
			float process = ((float)i)/((float)assetBundleNames.Length);
			EditorUtility.DisplayProgressBar("清理Tag中...", assetBundleNames[i], process);
			AssetDatabase.RemoveAssetBundleName(assetBundleNames[i], true);
		}
        EditorUtility.UnloadUnusedAssetsImmediate();
        EditorUtility.ClearProgressBar();
	}

#endif

	private static AssetBundleMgr mMgr = new AssetBundleMgr();
}
