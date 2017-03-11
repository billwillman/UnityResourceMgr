using UnityEngine;
using System.Collections;
using AutoUpdate;

public class TestDownload : MonoBehaviour {

    public UIButton m_BtnDownload = null;
    public UISlider m_Progress = null;
	public UILabel m_LbDown = null;
	public UILabel m_LbUseDownTime = null;
	public UIToggle m_CheckMultThread = null;

	public int ThreadCount = 1;

	private double m_LastM = 0;
	private double m_LastTotalM = 0;

    void InitUI()
    {
        if (m_BtnDownload != null)
        {
            EventDelegate.Add(m_BtnDownload.onClick, OnBtnDownClick);
        }

        AutoUpdateMgr.Instance.OnStateChanged = StateChanged;
        AutoUpdateMgr.Instance.OnError = OnAutoUpdateError;
    }

    void OnAutoUpdateError(AutoUpdateErrorType errType, int code)
    {
        Debug.LogFormat("OnUpdateError: errType {0:D} code {0:D}", (int)errType, code);
    }

	private float m_StartTimer = 0;
    void OnBtnDownClick()
    {
		m_StartTimer = Time.realtimeSinceStartup;
		bool isMultiThread = false;
		if (m_CheckMultThread != null)
		{
			isMultiThread = m_CheckMultThread.value;
		}

		if (isMultiThread)
			AutoUpdateMgr.Instance.StartMultAutoUpdate("http://192.168.1.105:1983/outPath", ThreadCount, 5f, 1024 * 1024);
		else
			AutoUpdateMgr.Instance.StartAutoUpdate("http://192.168.1.105:1983/outPath", 5f, 1024 * 1024);
    }

    void StateChanged(AutoUpdateState state)
    {
        Debug.LogFormat("AutoUpdate ChangeState: {0:D}", (int)state);

        if (state == AutoUpdateState.auEnd)
        {
            // 進入遊戲
            Debug.Log("Enter Game!!!");
        } else if (state == AutoUpdateState.auFinished)
        {
            // 下載完成
            Debug.Log("Res Update Finished!!!");

			float delta = Time.realtimeSinceStartup - m_StartTimer;
			Debug.LogFormat("下载耗时：{0}", delta.ToString());
			if (m_LbUseDownTime != null)
				m_LbUseDownTime.text = delta.ToString("F2");

            ResourceMgr.Instance.AutoUpdateClear();
            ResourceMgr.Instance.LoadConfigs(OnResLoad);
        }
    }

    void OnResLoad(bool isFinished)
    {
        if (isFinished)
        {
            AssetLoader loader = ResourceMgr.Instance.AssetLoader as AssetLoader;
            if (loader != null)
            {
                
            }
        }
    }

	// Use this for initialization
	void Start () {
        InitUI();
        ResourceMgr.Instance.LoadConfigs(OnResLoad);
    }
	
	// Update is called once per frame
	void Update () {
        TimerMgr.Instance.ScaleTick(Time.deltaTime);
        TimerMgr.Instance.UnScaleTick(Time.unscaledDeltaTime);

        AutoUpdateMgr.Instance.Update();
		float value;

		if (AutoUpdateMgr.Instance.TotalDownM > float.Epsilon)
			value = (float)(AutoUpdateMgr.Instance.CurDownM/AutoUpdateMgr.Instance.TotalDownM);
		else if (AutoUpdateMgr.Instance.DownProcess > float.Epsilon)
			value = 1f; 
		else
			value = 0;

        if (m_Progress != null)
            m_Progress.value = value;
		
		if (m_LbDown != null)
		{
			if (m_LastM != AutoUpdateMgr.Instance.CurDownM || m_LastTotalM != AutoUpdateMgr.Instance.TotalDownM)
			{
				m_LastM = AutoUpdateMgr.Instance.CurDownM;
				m_LastTotalM = AutoUpdateMgr.Instance.TotalDownM;
				string s = string.Format("{0}/{1} M", m_LastM.ToString("F2"), m_LastTotalM.ToString("F2"));
				m_LbDown.text = s;
			}
		}
    }

	void OnApplicationQuit()
	{
		NsHttpClient.HttpHelper.OnAppExit();
	}
}
