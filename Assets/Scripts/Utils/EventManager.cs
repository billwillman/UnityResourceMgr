using System;
using System.Collections;
using System.Collections.Generic;

// 事件分发器
public class EventDispatch : Singleton<EventDispatch>
{
    protected Dictionary<string, Delegate> Events
    {
        get
        {
            if (mEventMap == null)
                mEventMap = new Dictionary<string, Delegate>();
            return mEventMap;
        }
    }

    public void AddEvent(string evtName, Action evt)
    {
        Delegate evts;
        if (Events.TryGetValue(evtName, out evts))
        {
            Action act = evts as Action;
            if (act != null)
            {
                act += evt;
                mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
        else
            Events.Add(evtName, evt);
    }


    public void RemoveEvent(string evtName, Action evt)
    {
        if ((mEventMap == null) || (evt == null))
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Action act = evts as Action;
            if (act != null)
            {
                act -= evt;
                if (act == null)
                    mEventMap.Remove(evtName);
                else
                    mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
    }

    public void AddEvent<T>(string evtName, Action<T> evt)
    {
        Delegate evts;
        if (Events.TryGetValue(evtName, out evts))
        {
            Action<T> act = evts as Action<T>;
            if (act != null)
            {
                act += evt;
                mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
        else
            Events.Add(evtName, evt);
    }

    public void RemoveEvent<T>(string evtName, Action<T> evt)
    {
        if ((mEventMap == null) || (evt == null))
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Action<T> act = evts as Action<T>;
            if (act != null)
            {
                act -= evt;
                if (act == null)
                    mEventMap.Remove(evtName);
                else
                    mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
    }

    public void AddEvent<T, U>(string evtName, Action<T, U> evt)
    {
        Delegate evts;
        if (Events.TryGetValue(evtName, out evts))
        {
            Action<T, U> act = evts as Action<T, U>;
            if (act != null)
            {
                act += evt;
                mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
        else
            Events.Add(evtName, evt);
    }

    public void RemoveEvent<T, U>(string evtName, Action<T, U> evt)
    {
        if ((mEventMap == null) || (evt == null))
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Action<T, U> act = evts as Action<T, U>;
            if (act != null)
            {
                act -= evt;
                if (act == null)
                    mEventMap.Remove(evtName);
                else
                    mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
    }

    public void AddEvent<T, U, V>(string evtName, Action<T, U, V> evt)
    {
        Delegate evts;
        if (Events.TryGetValue(evtName, out evts))
        {
            Action<T, U, V> act = evts as Action<T, U, V>;
            if (act != null)
            {
                act += evt;
                mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
        else
            Events.Add(evtName, evt);
    }

    public void RemoveEvent<T, U, V>(string evtName, Action<T, U, V> evt)
    {
        if ((mEventMap == null) || (evt == null))
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Action<T, U, V> act = evts as Action<T, U, V>;
            if (act != null)
            {
                act -= evt;
                if (act == null)
                    mEventMap.Remove(evtName);
                else
                    mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
    }

    public void AddEvent<T, U, V, W>(string evtName, Action<T, U, V, W> evt)
    {
        Delegate evts;
        if (Events.TryGetValue(evtName, out evts))
        {
            Action<T, U, V, W> act = evts as Action<T, U, V, W>;
            if (act != null)
            {
                act += evt;
                mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
        else
            Events.Add(evtName, evt);
    }

    public void RemoveEvent<T, U, V, W>(string evtName, Action<T, U, V, W> evt)
    {
        if ((mEventMap == null) || (evt == null))
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Action<T, U, V, W> act = evts as Action<T, U, V, W>;
            if (act != null)
            {
                act -= evt;
                if (act == null)
                    mEventMap.Remove(evtName);
                else
                    mEventMap[evtName] = act;
            }
            else
                throw new Exception("EventDispatch evt type is null");
        }
    }

    public void TriggerEvent(string evtName)
    {
        if (mEventMap == null)
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Delegate[] list = evts.GetInvocationList();
            for (int i = 0; i < list.Length; ++i)
            {
                Action act = list[i] as Action;
                if (act != null)
                    act();
            }
        }
    }

    public void TriggerEvent<T>(string evtName, T V1)
    {
        if (mEventMap == null)
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Delegate[] list = evts.GetInvocationList();
            for (int i = 0; i < list.Length; ++i)
            {
                Action<T> act = list[i] as Action<T>;
                if (act != null)
                    act(V1);
            }
        }
    }

    public void TriggerEvent<T, U>(string evtName, T V1, U V2)
    {
        if (mEventMap == null)
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Delegate[] list = evts.GetInvocationList();
            for (int i = 0; i < list.Length; ++i)
            {
                Action<T, U> act = list[i] as Action<T, U>;
                if (act != null)
                    act(V1, V2);
            }
        }
    }

    public void TriggerEvent<T, U, V>(string evtName, T V1, U V2, V V3)
    {
        if (mEventMap == null)
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Delegate[] list = evts.GetInvocationList();
            for (int i = 0; i < list.Length; ++i)
            {
                Action<T, U, V> act = list[i] as Action<T, U, V>;
                if (act != null)
                    act(V1, V2, V3);
            }
        }
    }

    public void TriggerEvent<T, U, V, W>(string evtName, T V1, U V2, V V3, W V4)
    {
        if (mEventMap == null)
            return;

        Delegate evts;
        if (mEventMap.TryGetValue(evtName, out evts))
        {
            Delegate[] list = evts.GetInvocationList();
            for (int i = 0; i < list.Length; ++i)
            {
                Action<T, U, V, W> act = list[i] as Action<T, U, V, W>;
                if (act != null)
                    act(V1, V2, V3, V4);
            }
        }
    }

    // event Name, function
    private Dictionary<string, Delegate> mEventMap = null;
}