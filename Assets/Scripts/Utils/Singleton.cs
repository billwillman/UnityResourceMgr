/*
 * 单例
 */

using UnityEngine;
using System;
using System.Collections;
using Utils;

public class Singleton<T> where T : class, new()
{
    //
    // Static Fields
    //
    protected static T m_Instance;

    //
    // Static Properties
    //
    public static T Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new T();
            }
            return m_Instance;
        }
    }

    //
    // Static Methods
    //
    public static T GetInstance()
    {
        return Instance;
    }
}

public class SingetonMono<T> : CachedMonoBehaviour where T : CachedMonoBehaviour
{

    public static T Instance
    {
        get
        {
            if (m_Instance == null)
            {
                GameObject gameObj = new GameObject();
                m_Instance = gameObj.AddComponent<T>();
            }

            return m_Instance;
        }
    }

    public static T GetInstance()
    {
        return Instance;
    }

    public static void DestroyInstance()
    {
        if (m_Instance == null)
            return;
        GameObject gameObj = m_Instance.CachedGameObject;
        ResourceMgr.Instance.DestroyObject(gameObj);
    }

    protected static T m_Instance = null;
}
