using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParamPreNodeInfo
{
    public ParamPreNodeInfo(int _uid,string _funcName,string _typeName,int _paramOutType = 0)
    {
        m_PreNodeUid = _uid;
        m_PreNodeFuncName = _funcName;
        m_PreNodeTypeName = _typeName;
        m_PreNodeParamType = _paramOutType;
    }
    public int m_PreNodeUid;
    public string m_PreNodeFuncName;
    public string m_PreNodeTypeName;
    public int m_PreNodeParamType;
}

public class ParamOutPortStruct
{
    public ParamOutPortStruct(string _portName,string _typeName)
    {
        m_ParamOutPortName = _portName;
        m_ParamOutPortType = _typeName;
    }
    public string m_ParamOutPortName = "";
    public string m_ParamOutPortType = "";
}

/// <summary>
/// 用来序列化蓝图运行时的数据
/// </summary>
public class GraphViewRuntimeNodeBase
{
    //每个节点的名称
    public string m_Name;
    //每个节点的数据
    public NodeDataBase m_Data;
    /// <summary>
    /// 每个输入参数接口的存储结构
    /// Key：InPort名称
    /// Value：连接这个Port的对应的节点
    /// </summary> 
    public Dictionary<string, ParamPreNodeInfo> m_ParamInPortDic = new Dictionary<string, ParamPreNodeInfo>();
    //所有子节点的名称
    public Dictionary<string, List<int>> m_Childs = new Dictionary<string, List<int>>();
    /// <summary>
    /// 每个节点名称对应的参数输出方法名称
    /// </summary>
    public List<ParamOutPortStruct> m_ParamOutPortDic = new List<ParamOutPortStruct>();
    /// <summary>
    /// 蓝图运行管理器，这个节点属于哪个管理器，并且归属于这个蓝图管理器管理
    /// </summary>
    public GraphComponent m_GraphMgr;
    /// <summary>
    /// 每个节点的唯一ID标识 
    /// </summary>
    public int m_Guid = 0;
    /// <summary>
    /// 基类的构造方法
    /// </summary>
    public GraphViewRuntimeNodeBase()
    {
        
    }
    /// <summary>
    /// 节点初始化方法，在每个蓝图创建的时候会将当前蓝图内的所有节点执行一遍初始化
    /// 初始化的时候将输出口的对应UID数组进行排序
    /// </summary>
    public virtual void OnInit()
    {
        var keys = m_Childs.Keys;
        foreach (var key in keys)
        {
            m_Childs[key].Sort();
        }
    }
    /// <summary>
    /// 节点销毁方法，在每个蓝图执行销毁的时候会将当前蓝图内的所有节点执行一遍销毁
    /// </summary>
    public virtual void OnDestory()
    {
        
    }

    /// <summary>
    /// 节点可用的时候
    /// </summary>
    public virtual void OnEnable()
    {
        
    }

    /// <summary>
    /// 节点不可用的方法
    /// </summary>
    public virtual void OnDisable()
    {
        
    }
    /// <summary>
    /// 在运行时对每个节点进行类型转化
    /// </summary>
    public virtual void InitRuntimeData()
    {
        
    }
    /// <summary>
    /// 当前节点的进入方法
    /// </summary>
    public virtual void OnTriggerIn()
    {
        #if UNITY_EDITOR
        if (string.Equals(m_Name,"RootNodeData"))
        {
            GraphEventMgr.Instance.SendMessage<string>(EventName.BroadCastResetNodeState,m_GraphMgr.m_JsonConfigName);
        }
        if (EnumSpace.IsDebugBluePrint)
        {
            GraphEventMgr.Instance.SendMessage<string,int>(EventName.BroadCastBluePrintTriggerIn,m_GraphMgr.m_JsonConfigName,m_Guid);
        }
        #endif
        
        
        //基类进入的执行方法默认调用出口方法
        if (string.Equals(m_Name,"RootNodeData") || string.Equals(m_Name,"UpdateNodeData"))
        {
            SendTriggerOut("Out");
        }
    }
    /// <summary>
    /// 调用这个方法来进入下一个节点流程
    /// </summary>
    public virtual void SendTriggerOut(string _outPortName = "Out")
    {
        #if UNITY_EDITOR
        // if (EnumSpace.IsDebugBluePrint)
        // {
        //     EventMgr.Instance.SendMessage<string,int>(EventName.BroadCastBluePrintTriggerOut,m_GraphMgr.m_JsonConfigName,m_Guid);
        // }
        #endif
        if (m_Childs.ContainsKey(_outPortName))
        {
            for (int i = 0; i < m_Childs[_outPortName].Count; i++)
            {
                m_GraphMgr.EnterNextNode(m_Childs[_outPortName][i]);
            }
        }
    }
    
    public void SetUpdateActive(bool _isActive)
    {
        m_GraphMgr?.SetNodeUpdate(m_Guid, _isActive);
    }
    /// <summary>
    /// 每帧更新的函数，主要是有些节点可能会处于执行状态，此时就依靠这个方法进行操作
    /// </summary>
    public virtual void OnUpdate(Fix64 _deltaTime)
    {
        
    }
}
