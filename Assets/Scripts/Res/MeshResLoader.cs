using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class MeshResLoader: BaseResLoader
{
	public bool LoadMaterial(MeshRenderer renderer, string fileName)
	{
		if (renderer == null)
			return false;

		Material mat = null;
		if (!string.IsNullOrEmpty(fileName))
			mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResources(renderer, null, typeof(Material[]));
		SetResource(renderer, mat, typeof(Material));
		renderer.sharedMaterial = mat;
		return true;
	}

	public void ClearMaterials(MeshRenderer renderer)
	{
		SetResources(renderer, null, typeof(Material[]));
		SetResource(renderer, null, typeof(Material));
		renderer.sharedMaterial = null;
		renderer.material = null;
		renderer.sharedMaterials = null;
		renderer.materials = null;
	}
}
