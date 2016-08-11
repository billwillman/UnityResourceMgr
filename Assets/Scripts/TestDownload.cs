using UnityEngine;
using System.Collections;
using AutoUpdate;

public class TestDownload : MonoBehaviour {

    public UIButton m_BtnDownload = null;
    public UISlider m_Progress = null;

    void InitUI()
    {
        if (m_BtnDownload != null)
        {
            EventDelegate.Add(m_BtnDownload.onClick, OnBtnDownClick);
        }

        AutoUpdateMgr.Instance.OnStateChanged = StateChanged;
    }

    void OnBtnDownClick()
    {
        AutoUpdateMgr.Instance.StartAutoUpdate("http://192.168.1.102:1983");
    }

    void StateChanged(AutoUpdateState state)
    {
        if (state == AutoUpdateState.auEnd)
        {
            // 進入遊戲
        } else if (state == AutoUpdateState.auFinished)
        {
            // 下載完成
        }
    }

	// Use this for initialization
	void Start () {
        InitUI();
       
    }
	
	// Update is called once per frame
	void Update () {
        AutoUpdateMgr.Instance.Update();
        float value = AutoUpdateMgr.Instance.DownProcess;
        if (m_Progress != null)
            m_Progress.value = value;
    }
}
