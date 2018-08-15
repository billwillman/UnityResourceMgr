using UnityEngine;
using System.Collections;
using AutoUpdate;

public class DownloadCheck : MonoBehaviour {

	private static DownloadCheck m_Instance = null;
	private static string m_downloadUrl = "http://127.0.0.1";

	public static void ShowDownload()
	{
		if (m_Instance == null)
		{
			GameObject obj = new GameObject("download", typeof(DownloadCheck));
			m_Instance = obj.GetComponent<DownloadCheck>();
		}
	}

	void OnDownError(AutoUpdateErrorType errType, int code)
	{
		Debug.LogFormat("OnUpdateError: errType {0:D} code {0:D}", (int)errType, code);
	}

	void OnResFinished(bool isOk)
	{
		EnterGame();
	}

	void EnterGame()
	{
		GameObject.Destroy(gameObject);
		LuaMain.EnterLuaGame();
	}

	void OnDownStateChanged(AutoUpdateState state)
	{
		Debug.LogFormat("AutoUpdate ChangeState: {0:D}", (int)state);

		if (state == AutoUpdateState.auEnd)
		{
			// 進入遊戲
			Debug.Log("Enter Game!!!");
			EnterGame();
		} else if (state == AutoUpdateState.auFinished)
		{
			// 下載完成
			Debug.Log("Res Update Finished!!!");
			ResourceMgr.Instance.AutoUpdateClear();
			ResourceMgr.Instance.LoadConfigs(OnResFinished);
		}
	}

	void OnDestroy()
	{
		AutoUpdateMgr.Instance.OnError = null;
		AutoUpdateMgr.Instance.OnStateChanged = null;
		AutoUpdateMgr.Instance.Clear();
	}

	void OnGUI()
	{
		if (GUI.Button(new Rect(100, 100, 100, 50), "检查更新"))
		{
			AutoUpdateMgr.Instance.StartAutoUpdate(m_downloadUrl, 10, 64 * 1024);
			AutoUpdateMgr.Instance.OnError = OnDownError;
			AutoUpdateMgr.Instance.OnStateChanged = OnDownStateChanged;
		}

		float process = AutoUpdateMgr.Instance.DownProcess * 100f;
		GUI.Label(new Rect(100, 150, 100, 50), string.Format("下载进度: {0:D}", (int)process));

		string downStr = string.Format("{0}/{1}", AutoUpdateMgr.Instance.CurDownM, AutoUpdateMgr.Instance.CurDownM, AutoUpdateMgr.Instance.TotalDownM);
		GUI.Label(new Rect(100, 200, 100, 50), downStr);
	}
	
	// Update is called once per frame
	void Update () {
		AutoUpdateMgr.Instance.Update();
	}
}
