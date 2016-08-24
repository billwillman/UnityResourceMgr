#define USE_CHECK_VISIBLE
using UnityEngine;
using Utils;

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

	public void CheckVisible()
	{
		#if USE_CHECK_VISIBLE
		if (m_IsCheckedVisible)
			return;
		m_IsCheckedVisible = true;
		GameObject obj = this.gameObject;
		if (obj == null)
			return;
		if (!obj.activeSelf)
		{
			obj.SetActive(true);
			obj.SetActive(false);
		}
		#endif
	}

	#if USE_CHECK_VISIBLE
	private bool m_IsCheckedVisible = false;
	#endif
}