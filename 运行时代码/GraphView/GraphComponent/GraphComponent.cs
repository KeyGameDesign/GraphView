using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public enum ENUM_TICKLIST_OPERATOR
{
    ADD,
    REMOVE,
}

public struct NodeTickOperator
{
    public ENUM_TICKLIST_OPERATOR m_Op;
    public int m_Guid;
}

public class GraphComponent
{
    public GraphComponentData m_GraphData;
    public List<int> m_UpdateTickList = new List<int>();
    public List<NodeTickOperator> m_UpdateTickOperatorList = new List<NodeTickOperator>();
    public string m_JsonConfigName;
    
    Dictionary<string,Fix64> m_BlackBoard = new Dictionary<string, Fix64>();
    public static GraphComponent New(string _jsonConfig)
    {
        GraphComponent graphComponent = new GraphComponent();
        graphComponent.m_GraphData = SerilizeJsonToData(_jsonConfig);
        graphComponent.m_GraphData.SetGraphMgr(graphComponent);
        graphComponent.Init();
        graphComponent.m_JsonConfigName = _jsonConfig;
        return graphComponent;
    }

    public void Start()
    {
        //每次开始的时候将Update相关的清空，避免一进去后之前的节点都有脏标记
        m_UpdateTickList.Clear();
        m_GraphData.OnEnable();
        if (m_GraphData != null && m_GraphData.m_Root != null)
        {
            m_GraphData.m_Root.OnTriggerIn();
        }
    }
    public void Update(Fix64 _deltaTime)
    {
        if (m_GraphData != null && m_GraphData.m_UpdateRoot != null)
        {
            m_GraphData.m_UpdateRoot.OnTriggerIn();
        }
        for (int i = 0; i < m_UpdateTickOperatorList.Count; i++)
        {
            var value = m_UpdateTickOperatorList[i];
            int uid = value.m_Guid;
            switch (value.m_Op)
            {
                case ENUM_TICKLIST_OPERATOR.ADD:
                {
                    if (m_UpdateTickList.Contains(uid) == false)
                    {
                        m_UpdateTickList.Add(uid);
                    }
                }
                    break;
                case ENUM_TICKLIST_OPERATOR.REMOVE:
                {
                    if (m_UpdateTickList.Contains(uid))
                    {
                        int index = m_UpdateTickList.LastIndexOf(uid);
                        m_UpdateTickList.RemoveAt(index);
                    }
                }
                    break;
            }
        }
        m_UpdateTickOperatorList.Clear();
        for (int i = 0; i < m_UpdateTickList.Count; i++)
        {
            if (m_GraphData.m_AllDic.ContainsKey(m_UpdateTickList[i]))
            {
                m_GraphData.m_AllDic[m_UpdateTickList[i]].OnUpdate(_deltaTime);
            }
        }
    }
    public void SetNodeUpdate(int _uid, bool _isActiveUpdate)
    {
        if (m_UpdateTickList.Contains(_uid))
        {
            //因为此时已经有记录了，不做任何处理
            return;
        }
        NodeTickOperator nodeTickOperator = new NodeTickOperator();
        nodeTickOperator.m_Guid = _uid;
        if (_isActiveUpdate == false)
        {
            nodeTickOperator.m_Op = ENUM_TICKLIST_OPERATOR.REMOVE;
            m_UpdateTickOperatorList.Add(nodeTickOperator);
        }
        else
        {
            nodeTickOperator.m_Op = ENUM_TICKLIST_OPERATOR.ADD;
            m_UpdateTickOperatorList.Add(nodeTickOperator);
        }
    }
    public void EnterNextNode(int _nodeGuid)
    {
        if (m_GraphData != null && m_GraphData.m_AllDic.ContainsKey(_nodeGuid))
        {
            m_GraphData.m_AllDic[_nodeGuid].OnTriggerIn();
        }
    }

    public static GraphComponentData SerilizeJsonToData(string _jsonConfig)
    {
        GraphComponentData _node = null;
        string fileUrl = EnumSpace.RUNTIME_JSON_PATH;
        string filePath = fileUrl + _jsonConfig + ".json";
        
        if (File.Exists(filePath))
        {
            using (StreamReader sr = File.OpenText(filePath))
            {
                //数据保存
                var readData = sr.ReadToEnd();
                sr.Close();
                var myJsonConverter = new MyJsonConverter_Runtime();
                _node = JsonConvert.DeserializeObject<GraphComponentData>(readData, new MyJsonConverter_Runtime());
            }
            return _node;
        }

        return null;
    }

    public void Init()
    {
        if (m_GraphData != null)
        {
            m_GraphData.Init();
        }
    }

    public void Destory()
    {
        if (m_GraphData != null)
        {
            m_GraphData.Destory();
        }
    }

    public void SetBlackBoardData(string _saveName, Fix64 _value)
    {
        if (m_BlackBoard.ContainsKey(_saveName))
        {
            m_BlackBoard[_saveName] = _value;
        }
        else
        {
            m_BlackBoard.Add(_saveName, _value);
        }
    }

    public Fix64 GetBlackBoardData(string _saveName)
    {
        if (m_BlackBoard.ContainsKey(_saveName))
        {
            return m_BlackBoard[_saveName];
        }
        return Fix64.Zero;
    }

    public bool CheckHadBlackBoardData(string _saveName)
    {
        return m_BlackBoard.ContainsKey(_saveName);
    }
}

public class GraphComponentData
{
    /// <summary>
    /// 根节点入口
    /// </summary>
    public GraphViewRuntimeNodeBase m_Root;
    /// <summary>
    /// 每帧更新节点
    /// </summary>
    public GraphViewRuntimeNodeBase m_UpdateRoot;
    //所有节点的集合
    public Dictionary<int, GraphViewRuntimeNodeBase> m_AllDic = new Dictionary<int, GraphViewRuntimeNodeBase>();

    ///初始化蓝图管理器方法
    public void SetGraphMgr(GraphComponent _graphComponent)
    {
        if (m_Root != null)
        {
            m_Root.m_GraphMgr = _graphComponent;
            m_Root.InitRuntimeData();
        }
        
        if (m_UpdateRoot != null)
        {
            m_UpdateRoot.m_GraphMgr = _graphComponent;
            m_UpdateRoot.InitRuntimeData();
        }
        
        var keys = m_AllDic.Keys;
        var tempKey = new List<int>();
        foreach (var key in keys)
        {
            tempKey.Add(key);
        }
        tempKey.Sort();
        for (int i = 0; i < tempKey.Count; i++)
        {
            m_AllDic[tempKey[i]].m_GraphMgr = _graphComponent;
            m_AllDic[tempKey[i]].InitRuntimeData();
        }
    }
    public void Init()
    {
        if (m_Root != null)
        {
            m_Root.OnInit();
        }
        
        var keys = m_AllDic.Keys;
        var tempKey = new List<int>();
        foreach (var key in keys)
        {
            tempKey.Add(key);
        }
        tempKey.Sort();
        for (int i = 0; i < tempKey.Count; i++)
        {
            m_AllDic[tempKey[i]].OnInit();
        }
    }

    public void Destory()
    {
        if (m_Root != null)
        {
            m_Root.OnDestory();
        }

        var keys = m_AllDic.Keys;
        var tempKey = new List<int>();
        foreach (var key in keys)
        {
            tempKey.Add(key);
        }
        tempKey.Sort();
        for (int i = 0; i < tempKey.Count; i++)
        {
            m_AllDic[tempKey[i]].OnDestory();
        }
    }

    public void OnEnable()
    {
        if (m_Root != null)
        {
            m_Root.OnEnable();
        }
        var keys = m_AllDic.Keys;
        var tempKey = new List<int>();
        foreach (var key in keys)
        {
            tempKey.Add(key);
        }
        tempKey.Sort();
        for (int i = 0; i < tempKey.Count; i++)
        {
            m_AllDic[tempKey[i]].OnEnable();
        }
    }
    public void OnDisable()
    {
        if (m_Root != null)
        {
            m_Root.OnDisable();
        }
        var keys = m_AllDic.Keys;
        var tempKey = new List<int>();
        foreach (var key in keys)
        {
            tempKey.Add(key);
        }
        tempKey.Sort();
        for (int i = 0; i < tempKey.Count; i++)
        {
            m_AllDic[tempKey[i]].OnDisable();
        }
    }

    public GraphViewRuntimeNodeBase GetNodeByUid(int _uid)
    {
        if (m_AllDic.ContainsKey(_uid))
        {
            return m_AllDic[_uid];
        }

        return null;
    }
}
