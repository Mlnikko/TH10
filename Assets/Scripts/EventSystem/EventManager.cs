using System;
using System.Collections.Generic;
using UnityEngine.Events;

public enum E_Event
{
    NULL,
    BattleStart
}

public enum E_EventValue
{
    NULL,
    Int,
    Float,
    String,
}

public interface IEventInfo { }

public class EventInfo : IEventInfo
{
    public UnityAction action;
}

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> action;
}

public class EventManager : Singleton<EventManager>
{
    Dictionary<E_Event, IEventInfo> eventDict = new();

    #region 无参事件控制
    public void RegistEvent(E_Event name, UnityAction action)
    {
        if (eventDict.ContainsKey(name)) //如果这个action已经注册过了
            (eventDict[name] as EventInfo).action += action;
        else
            eventDict.Add(name, new EventInfo() { action = action });
    }
    public void UnRegistEvent(E_Event name, UnityAction action)
    {
        if (eventDict.ContainsKey(name))
            (eventDict[name] as EventInfo).action -= action;
    }
    public void TriggerEvent(E_Event name)
    {
        if (eventDict.ContainsKey(name))
        {
            (eventDict[name] as EventInfo).action?.Invoke();
        }
    }

    #endregion

    #region 含参事件控制
    public void RegistEvent<T>(E_Event name, UnityAction<T> action)
    {
        if (eventDict.ContainsKey(name)) //如果这个action已经注册过了
            (eventDict[name] as EventInfo<T>).action += action;
        else
            eventDict.Add(name, new EventInfo<T>() { action = action });
    }
    public void UnRegistEvent<T>(E_Event name, UnityAction<T> action)
    {
        if (eventDict.ContainsKey(name))
            (eventDict[name] as EventInfo<T>).action -= action;
    }
    public void TriggerEvent<T>(E_Event name, T arg)
    {
        if (eventDict.ContainsKey(name))
        {
            (eventDict[name] as EventInfo<T>).action?.Invoke(arg);
        }
    }
    #endregion

    public void ClearEventDict()
    {
        eventDict.Clear();
    }
}
