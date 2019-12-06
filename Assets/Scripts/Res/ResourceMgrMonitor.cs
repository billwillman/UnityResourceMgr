using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 测试用
public class ResourceMgrMonitor: MonoBehaviour
{
	void Update()
	{
		if (Application.isEditor) {
			UsedAssetCount = AssetCacheManager.Instance.UsedCacheCount;
			NotUsedAssetCount = AssetCacheManager.Instance.NotUsedCacheList.Count;
			float mem = (float)AssetCacheManager.Instance.GetCheckMemorySize () / (1024 * 1024);
			CurrentMemory = StringHelper.Format ("{0}M", mem.ToString ());
		}
	} 

	public int UsedAssetCount = 0;
	public int NotUsedAssetCount = 0;
	public string CurrentMemory = string.Empty;
}
