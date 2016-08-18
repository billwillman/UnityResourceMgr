using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils;

public class SpriteResLoader: BaseResLoader
{
	public bool LoadMaterial(SpriteRenderer sprite, string fileName)
	{
		if (sprite == null || string.IsNullOrEmpty(fileName))
			return false;
		Material mat = ResourceMgr.Instance.LoadMaterial(fileName, ResourceCacheType.rctRefAdd);
		SetResource(sprite, mat, typeof(Material));

		if (mat != null)
			sprite.sharedMaterial = mat;
		else
			sprite.sharedMaterial = null;

		return mat != null;
	}

	public void ClearMaterial(SpriteRenderer sprite)
	{
		if (sprite == null)
			return;
		ClearResource<Material>(sprite);
		sprite.sharedMaterial = null;
	}

	public bool LoadSprite(SpriteRenderer sprite, string fileName, string spriteName)
	{
		if (sprite == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
			return false;
		Sprite[] sps = ResourceMgr.Instance.LoadSprites(fileName, ResourceCacheType.rctRefAdd);
		bool isFound = false;
		for (int i = 0; i < sps.Length; ++i)
		{
			Sprite sp = sps[i];
			if (sp == null)
				continue;
			if (!isFound && string.Compare(sp.name, spriteName) == 0)
			{
				sprite.sprite = sp;
				isFound = true;
				SetResources(sprite, sps, typeof(Sprite[]));
				break;
			}
		}

		if (!isFound)
		{
			for (int i = 0; i < sps.Length; ++i)
			{
				Sprite sp = sps[i];
				DestroySprite(sp);
			}
			SetResources(sprite, null, typeof(Sprite[]));
		}
		
		return isFound;
	}
}