using System;
using UnityEngine;

public class LogMgr : Singleton<LogMgr>
{
    public void LogError(string str)
    {
        string s = StringHelper.Format("[Error]{0}", str);
        Debug.LogError(s);
    }

    public void LogWarning(string str)
    {
        string s = StringHelper.Format("[Warning]{0}", str);
        Debug.LogWarning(s);
    }

    public void Log(string str)
    {
        string s = StringHelper.Format("[Log]{0}", str);
        Debug.Log(s);
    }

    public void LogExcept(Exception exp)
    {
        Debug.LogException(exp);
    }
}
