using UnityEngine;
using System.Collections;
using AutoUpdate;
using NsHttpClient;

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
		Debug.LogErrorFormat("OnUpdateError: errType {0:D} code {0:D}", (int)errType, code);
	}

	void OnResFinished(bool isOk)
	{
		EnterGame();
	}

	void EnterGame()
	{
		GameObject.Destroy(gameObject);
		LuaMain.Main.EnterLuaGame();
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

	float process = 0f;
	HttpClient m_Client = null;
	float read, maxRead;

	void OnDownEnd(HttpClient client, HttpListenerStatus status)
	{
		
		switch (status) {
		case HttpListenerStatus.hsDone:
			{
				process = 1f;
				Debug.Log ("下载完成");
				break;
			}
		case HttpListenerStatus.hsError:
			{
				Debug.Log ("下载失败");
				break;
			}
		}
		m_Client = null;
	}

	void OnDownProcess(HttpClient client)
	{
		var rep = client.Listener as HttpClientResponse;
		process = rep.DownProcess;
		read = rep.ReadBytes;
		maxRead = rep.MaxReadBytes;
	}

	void OnGUI()
	{
        if (GUI.Button(new Rect(300, 100, 100, 50), "上传文件")) {
            HttpHelper.OpenUrl<HttpClientUpFileStream>("http://10.246.54.43/upload/", new HttpClientUpFileStream("Assets/Main.unity"),
                (HttpClient client, HttpListenerStatus status) =>
                {
                    Debug.LogError("上传文件完成");
                }
                );
        }

		if (GUI.Button(new Rect(100, 100, 100, 50), "检查更新"))
		{
		    AutoUpdateMgr.Instance.StartAutoUpdate(m_downloadUrl, 10, 64 * 1024);
			AutoUpdateMgr.Instance.OnError = OnDownError;
			AutoUpdateMgr.Instance.OnStateChanged = OnDownStateChanged;
          /*
			var stream = new HttpClientFileStream(Utils.FilePathMgr.GetInstance().WritePath + "/Android-1.5.0.0.zip", 0, 64 * 1024);
			if (m_Client != null) {
				m_Client.Dispose();
				m_Client = null;
			}
			m_Client = HttpHelper.OpenUrl ("https://patch.cgm.beanfun.com/cgm/update/Android/Android-1.5.0.0.zip", stream, OnDownEnd, OnDownProcess, 30, 30);
            */
		}

		//float process = AutoUpdateMgr.Instance.DownProcess * 100f;
		GUI.Label(new Rect(100, 150, 100, 20), StringHelper.Format("下载进度: {0:D}", (int)process));

        //string downStr = StringHelper.Format("{0}/{1}", AutoUpdateMgr.Instance.CurDownM, AutoUpdateMgr.Instance.CurDownM, AutoUpdateMgr.Instance.TotalDownM);
        string downStr = StringHelper.Format("{0}/{1}", read, maxRead);
		GUI.Label(new Rect(100, 170, 100, 20), downStr);
	}
	
	// Update is called once per frame
	void Update () {
		AutoUpdateMgr.Instance.Update();
	}
}
