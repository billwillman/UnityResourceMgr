using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestString : MonoBehaviour
{
    private void OnGUI() {
        int v1 = Random.Range(0, 9);
        int v2 = Random.Range(0, 9);
        int v3 = Random.Range(0, 9);
        int v4 = Random.Range(0, 9);
        if (GUI.Button(new Rect(100, 100, 100, 50), "string +")) {
            UnityEngine.Profiling.Profiler.BeginSample("string +");
            string s1 = "qwertyuiopasdfghjklzxcvbnm" + v1 + v2 + v3 + v4;
            UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(s1);
        } else if (GUI.Button(new Rect(200, 100, 100, 50), "string.format")) {
            UnityEngine.Profiling.Profiler.BeginSample("string.format");
            string s2 = string.Format("qwertyuiopasdfghjklzxcvbnm{0}{1}{2}{3}", v1, v2, v3, v4);
            UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(s2);
        } else if (GUI.Button(new Rect(300, 100, 100, 50), "StringHelp.format")) {
            UnityEngine.Profiling.Profiler.BeginSample("StringHelp.format");
            string s3 = StringHelper.Format("qwertyuiopasdfghjklzxcvbnm{0}{1}{2}{3}", v1, v2, v3, v4);
            UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(s3);
        }
    }
}
