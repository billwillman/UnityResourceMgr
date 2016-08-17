using UnityEngine;
using System.Collections;

public class TestUI : NGUIResLoader {

	public UITexture uiTexture = null;
	private int flag = 0;
	void OnGUI()
	{
		if (m_IsConfigLoaded && uiTexture != null)
		{
			if (GUI.Button(new Rect(100, 100, 150, 50), "切换贴图"))
			{
				if (flag%2 == 0)
					LoadMainTexture(uiTexture, "resources/models/@flag/m_hadry_01.psd");
				else
					LoadMainTexture(uiTexture, "resources/models/@flag/m_items_01.psd");
				++flag;
			}
		}

	}

	void OnConfigLoad(bool isOK)
	{
		m_IsConfigLoaded = true;
	}

	void Start()
	{
		ResourceMgr.Instance.LoadConfigs(OnConfigLoad);
	}

	void Update()
	{
		TimerMgr.Instance.ScaleTick(Time.deltaTime);
		TimerMgr.Instance.UnScaleTick(Time.unscaledDeltaTime);
	}

	private bool m_IsConfigLoaded = false;
}
