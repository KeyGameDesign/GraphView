using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphEventMgr
{
    private static GraphEventMgr instance = null;
    private static readonly object obj = new object();
    private Dictionary<EventName, List<Delegate>> m_EventDic = new Dictionary<EventName, List<Delegate>>();
    private GraphEventMgr()
    {

    }
    public static GraphEventMgr Instance
    {
        get
        {
            lock (obj)
            {
                if (instance == null)
                {
                    instance = new GraphEventMgr();
                }
                return instance;
            }
        }
    }
    private void AddListenerToDic(EventName _key,Delegate _callBack)
    {
        List<Delegate> eventList;
        if (m_EventDic.TryGetValue(_key,out eventList))
        {
            eventList.Add(_callBack);
        }
        else
        {
            eventList = new List<Delegate>();
            eventList.Add(_callBack);
            m_EventDic.Add(_key, eventList);
        }
    }

    private void RemoveListenerFromDic(EventName _key, Delegate _callBack)
    {
        List<Delegate> eventList = null;
        if (m_EventDic.TryGetValue(_key, out eventList))
        {
            eventList.Remove(_callBack);
        }
        //����������Ż���������Ϊ0��ʱ�����Ƴ�������Ҫ��Ȼ������ע��ʱ�򻹻���newһ�Σ����²���GC
        if (eventList != null && eventList.Count == 0)
        {
            m_EventDic.Remove(_key);
        }
    }

    public void ClearAllListeners()
    {
        m_EventDic.Clear();
    }

    public void AddListener(EventName _key, Action _callBack)
    {
        AddListenerToDic(_key, _callBack);
    }

    public void AddListener<T1>(EventName _key, Action<T1> _callBack)
    {
        AddListenerToDic(_key, _callBack);
    }
    public void AddListener<T1, T2>(EventName _key, Action<T1, T2> _callBack)
    {
        AddListenerToDic(_key, _callBack);
    }
    public void AddListener<T1, T2,T3>(EventName _key, Action<T1, T2,T3> _callBack)
    {
        AddListenerToDic(_key, _callBack);
    }
    public void AddListener<T1, T2,T3,T4>(EventName _key, Action<T1, T2,T3,T4> _callBack)
    {
        AddListenerToDic(_key, _callBack);
    }

    public void RemoveListener(EventName _key, Action _callBack)
    {
        RemoveListenerFromDic(_key, _callBack);
    }
    public void RemoveListener<T1>(EventName _key, Action<T1> _callBack)
    {
        RemoveListenerFromDic(_key, _callBack);
    }
    public void RemoveListener<T1,T2>(EventName _key, Action<T1,T2> _callBack)
    {
        RemoveListenerFromDic(_key, _callBack);
    }
    public void RemoveListener<T1, T2,T3>(EventName _key, Action<T1, T2,T3> _callBack)
    {
        RemoveListenerFromDic(_key, _callBack);
    }
    public void RemoveListener<T1, T2,T3,T4>(EventName _key, Action<T1, T2,T3,T4> _callBack)
    {
        RemoveListenerFromDic(_key, _callBack);
    }

    public void SendMessage(EventName _key)
    {

        List<Delegate> eventList;
        if (m_EventDic.TryGetValue(_key, out eventList))
        {
            for (int i = 0;i < eventList.Count;i++)
            {
                if (eventList[i] is Action)
                {
                    Action callBack = eventList[i] as Action;
                    callBack.Invoke();
                }
            }
        }
    }
    public void SendMessage<T1>(EventName _key, T1 _args)
    {

        List<Delegate> eventList;   
        if (m_EventDic.TryGetValue(_key, out eventList))
        {
            for (int i = 0; i < eventList.Count; i++)
            {
                if (eventList[i] is Action<T1>)
                {
                    Action<T1> callBack = eventList[i] as Action<T1>;
                    callBack.Invoke(_args);
                }
            }
        }
    }
    public void SendMessage<T1,T2>(EventName _key, T1 _args, T2 _args2)
    {

        List<Delegate> eventList;
        if (m_EventDic.TryGetValue(_key, out eventList))
        {
            for (int i = 0; i < eventList.Count; i++)
            {
                if (eventList[i] is Action<T1,T2>)
                {
                    Action<T1,T2> callBack = eventList[i] as Action<T1,T2>;
                    callBack.Invoke(_args, _args2);
                }
            }
        }
    }
    public void SendMessage<T1,T2,T3>(EventName _key, T1 _args, T2 _args2,T3 _args3)
    {

        List<Delegate> eventList;
        if (m_EventDic.TryGetValue(_key, out eventList))
        {
            for (int i = 0; i < eventList.Count; i++)
            {
                if (eventList[i] is Action<T1,T2,T3>)
                {
                    Action<T1,T2,T3> callBack = eventList[i] as Action<T1,T2,T3>;
                    callBack.Invoke(_args, _args2,_args3);
                }
            }
        }
    }
    public void SendMessage<T1,T2,T3,T4>(EventName _key, T1 _args, T2 _args2,T3 _args3,T4 _args4)
    {

        List<Delegate> eventList;
        if (m_EventDic.TryGetValue(_key, out eventList))
        {
            for (int i = 0; i < eventList.Count; i++)
            {
                if (eventList[i] is Action<T1,T2,T3,T4>)
                {
                    Action<T1,T2,T3,T4> callBack = eventList[i] as Action<T1,T2,T3,T4>;
                    callBack.Invoke(_args, _args2,_args3,_args4);
                }
            }
        }
    }
}
