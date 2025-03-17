using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 节点数据基类，主要用来显示可视化节点相关数据和蓝图节点运行数据
/// </summary>
public class NodeDataBase
{
    public string m_ClassName;
    public Dictionary<string, Func<float>> m_FieldActionDic = new Dictionary<string, Func<float>>();
    public NodeDataBase()
    {
        m_ClassName = GetType().Name;
        InitFieldActionDic();
    }

    public virtual void InitFieldActionDic()
    {
        
    }
}
