
using UnityEngine;

public class ResInstDestroy: MonoBehaviour
{
	void OnDestroy()
	{
		ResourceMgr.Instance.OnDestroyInstObject (gameObject);
	}
}
