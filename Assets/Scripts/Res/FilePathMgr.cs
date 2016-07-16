using System;
using UnityEngine;

namespace Utils
{
	public class FilePathMgr: Singleton<FilePathMgr>
	{
		public string WritePath
		{
			get
			{
				string ret = Application.persistentDataPath;
				if (string.IsNullOrEmpty(ret))
				{
					// call Java function
				}

				return ret;
			}
		}
	}
}

