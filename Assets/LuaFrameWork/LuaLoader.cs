using System;
using System.IO;
using LuaInterface;

public class LuaLoader: LuaFileUtils
{
	private static readonly string _cResLuaExt = ".lua.bytes";

	public LuaLoader(): base()
	{
		// 使用自己的资源管理
		beZip = true;
	}

	public static byte[] LoadFileBufferFromResMgr(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return null;
		fileName = fileName.ToLower();
		bool isLuaExt = fileName.EndsWith(_cResLuaExt);
		if (!isLuaExt)
			fileName = Path.ChangeExtension(fileName, _cResLuaExt);
		string resFileName = string.Format("{0}/{1}", LuaConst.resToLuaDir, fileName);
		byte[] ret = ResourceMgr.Instance.LoadBytes(resFileName);
		if (ret == null)
		{
			resFileName = string.Format("{0}/{1}", LuaConst.resLuaDir, fileName);
			ret = ResourceMgr.Instance.LoadBytes(resFileName);
		}
		#if DEBUG
		UnityEngine.Debug.LogFormat("Lua加载: {0}", fileName);
		#endif
		return ret;
	}

	public override byte[] ReadFile(string fileName)
	{
		return LoadFileBufferFromResMgr(fileName);
	}
}

