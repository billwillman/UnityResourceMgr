
using UnityEngine;

public class ResInstDelayDestroy: MonoBehaviour
{
	public float DelayDestroyTime = 0;

	void Start()
	{
		GameObject.Destroy (gameObject, DelayDestroyTime);
	}

	void OnDestroy()
	{
		ResourceMgr.Instance.OnDestroyInstObject (gameObject.GetInstanceID ());
	}
}