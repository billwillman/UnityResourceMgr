using System;
using System.Collections;
using System.Collections.Generic;

namespace AutoUpdate
{
	// 压缩包类
	public static class ZipTools
	{
		public static void BuildVersionZip(string outDir, string oldVersion, string newVersion, ResListFile oldFileList, ResListFile newFileList)
		{
			if (string.IsNullOrEmpty(outDir) || 
				string.IsNullOrEmpty(newVersion) || newFileList == null || 
				string.IsNullOrEmpty(oldVersion) || oldFileList == null)
				return;
			
			List<string> diffFileList = CompareDiffList(oldFileList, newFileList);
			string zipFileName = string.Format("{0}/{1}-{2}.zip", outDir, oldVersion, newVersion);
			Compress(zipFileName, diffFileList);
		}

		private static List<string> CompareDiffList(ResListFile oldFileList, ResListFile newFileList)
		{
			if (newFileList == null)
				return null;

			List<string> ret = null;
			if (oldFileList == null)
			{
				var infos = newFileList.AllToDiffInfos();
				if (infos == null || infos.Length <= 0)
					return ret;
				for (int i = 0; i < infos.Length; ++i)
				{
					var info = infos[i];
					if (string.IsNullOrEmpty(info.fileName) || string.IsNullOrEmpty(info.fileContentMd5))
						continue;
					
					if (ret == null)
						ret = new List<string>();
					// 更新的文件都是以MD5为文件名的
					ret.Add(info.fileContentMd5);
				}
			} else
			{
				var iter = newFileList.GetIter();
				while (iter.MoveNext())
				{
					string oldMd5 = oldFileList.GetFileContentMd5(iter.Current.Key);
					if (string.Compare(oldMd5, iter.Current.Value.fileContentMd5) != 0)
					{
						if (string.IsNullOrEmpty(iter.Current.Value.fileContentMd5))
							continue;
						
						if (ret == null)
							ret = new List<string>();
						ret.Add(iter.Current.Value.fileContentMd5);
					}
				}
				iter.Dispose();
			}

			return ret;
		}

		private static void Compress(string zipFileName, List<string> files)
		{
			if (string.IsNullOrEmpty(zipFileName) || files == null || files.Count <= 0)
				return;
			// 对文件进行压缩
		}
	}

}
