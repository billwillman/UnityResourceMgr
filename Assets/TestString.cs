using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestString : MonoBehaviour
{
    private void OnGUI() {
        if (GUI.Button(new Rect(100, 100, 100, 50), "string +")) {
            UnityEngine.Profiling.Profiler.BeginSample("string +");
            string s1 = "qwertyuiopasdfghjklzxcvbnm" + "1234567890";
            UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(s1);
        } else if (GUI.Button(new Rect(200, 100, 100, 50), "string.format")) {
            UnityEngine.Profiling.Profiler.BeginSample("string.format");
            string s2 = string.Format("{0}{1}", "qwertyuiopasdfghjklzxcvbnm", "1234567890");
            UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(s2);
        } else if (GUI.Button(new Rect(300, 100, 100, 50), "StringHelp.format")) {
            UnityEngine.Profiling.Profiler.BeginSample("StringHelp.format");
            string s3 = StringHelper.Format("{0}{1}", "qwertyuiopasdfghjklzxcvbnm", "1234567890");
            UnityEngine.Profiling.Profiler.EndSample();
            Debug.Log(s3);
        }
    }
}
