using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Start�ڵ�����
/// </summary>
public class RootNodeData : GraphViewRuntimeNodeBase
{
    public RootNodeData()
    {
        m_Name = "RootNodeData";
        m_Data = new NodeDataBase();
        m_Childs = new Dictionary<string, List<int>>();
    }
}

public class UpdateNodeData : GraphViewRuntimeNodeBase
{
    public UpdateNodeData()
    {
        m_Name = "UpdateNodeData";
        m_Data = new NodeDataBase();
        m_Childs = new Dictionary<string, List<int>>();
    }
}
