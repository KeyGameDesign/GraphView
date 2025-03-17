using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DialogueGraphWindow : EditorWindow
{
    public string m_ConfigName;

    private DialogueGraphView _graphView;
    TextField m_SaveTextField;

    private Toggle m_IsDebugToggle;

    private static int m_GUID;

    public static int GenUID()
    {
        m_GUID++;
        return m_GUID;
    }

    public static void InitUID(int _initValue)
    {
        m_GUID = _initValue;
    }

    private void OnEnable()
    {
        m_GUID = PlayerPrefs.GetInt(m_ConfigName);
        GraphEventMgr.Instance.RemoveListener<string,int>(EventName.BroadCastBluePrintTriggerIn,OnBroadCastBluePrintTriggerIn);
        GraphEventMgr.Instance.AddListener<string,int>(EventName.BroadCastBluePrintTriggerIn,OnBroadCastBluePrintTriggerIn);
        GraphEventMgr.Instance.RemoveListener<string,int>(EventName.BroadCastBluePrintTriggerOut,OnBroadCastBluePrintTriggerOut);
        GraphEventMgr.Instance.AddListener<string,int>(EventName.BroadCastBluePrintTriggerOut,OnBroadCastBluePrintTriggerOut);
        GraphEventMgr.Instance.RemoveListener<string>(EventName.BroadCastResetNodeState,BroadCastResetNodeState);
        GraphEventMgr.Instance.AddListener<string>(EventName.BroadCastResetNodeState,BroadCastResetNodeState);
        RefreshView();
    }

    void OnBroadCastBluePrintTriggerIn(string _graphName, int _nodeUID)
    {
        _graphView?.OnBroadCastBluePrintTriggerIn(_graphName,_nodeUID);
    }
    void OnBroadCastBluePrintTriggerOut(string _graphName, int _nodeUID)
    {
        _graphView?.OnBroadCastBluePrintTriggerOut(_graphName,_nodeUID);
    }
    public void BroadCastResetNodeState(string _graphName)
    {
        _graphView?.BroadCastResetNodeState(_graphName);
    }
    public void RefreshView()
    {
        if (_graphView == null)
        {
            _graphView = new DialogueGraphView
            {
                name = m_ConfigName
            };
        }

        if (string.IsNullOrEmpty(m_ConfigName) == false)
        {
            string directoryPath = EditorPath.EDITOR_JSON_PATH;
            string filePath = directoryPath + "/" + m_ConfigName + ".json";
            _graphView.m_SaveFileName = m_ConfigName;
            if (File.Exists(filePath))
            {
                //_graphView.InitTreeByData();
                _graphView.InitTreeByDataEditor();
            }
            else
            {
                _graphView.InitFirstNode();
            }
        }
        else
        {
            _graphView.InitFirstNode();
        }
        // 让graphView铺满整个Editor窗口
        _graphView.StretchToParentSize();
        // 把它添加到EditorWindow的可视化Root元素下面
        rootVisualElement.Add(_graphView);

        Toolbar toolbar = new Toolbar();
        //创建lambda函数，代表点击按钮后发生的函数调用
        Button btn = new Button(clickEvent: () =>
        {
            _graphView.GenTreeData();
        });
        btn.text = "保存数据";
        toolbar.Add(btn);

        Label label = new Label();
        label.text = "请输入要保存的名称：";
        toolbar.Add(label);

        m_SaveTextField = new TextField();

        m_SaveTextField.value = _graphView.m_SaveFileName;

        var lala = new Label("开启调试：");
        lala.style.width = 70;
        m_IsDebugToggle = new Toggle();
        m_IsDebugToggle.value = EnumSpace.IsDebugBluePrint;

        toolbar.Add(m_SaveTextField);

        toolbar.Add(lala);
        toolbar.Add(m_IsDebugToggle);
        rootVisualElement.Add(toolbar);

    }
    // 关闭窗口时销毁graphView
    private void OnDisable()
    {
        _graphView.GenTreeData();

        PlayerPrefs.SetString(m_ConfigName, _graphView.m_SaveFileName);
        PlayerPrefs.SetInt(m_ConfigName,DialogueGraphWindow.m_GUID);
        
        PlayerPrefs.Save();
        
        GraphEventMgr.Instance.RemoveListener<string,int>(EventName.BroadCastBluePrintTriggerIn,OnBroadCastBluePrintTriggerIn);
        GraphEventMgr.Instance.RemoveListener<string,int>(EventName.BroadCastBluePrintTriggerOut,OnBroadCastBluePrintTriggerOut);
        GraphEventMgr.Instance.RemoveListener<string>(EventName.BroadCastResetNodeState,BroadCastResetNodeState);

        rootVisualElement.Remove(_graphView); 


    }
    private void OnDestroy()
    {
        PlayerPrefs.DeleteKey(m_ConfigName);
        GraphEventMgr.Instance.RemoveListener<string,int>(EventName.BroadCastBluePrintTriggerIn,OnBroadCastBluePrintTriggerIn);
        GraphEventMgr.Instance.RemoveListener<string,int>(EventName.BroadCastBluePrintTriggerOut,OnBroadCastBluePrintTriggerOut);
        GraphEventMgr.Instance.RemoveListener<string>(EventName.BroadCastResetNodeState,BroadCastResetNodeState);
    }
    //每帧刷新的编辑器界面方法
    private void OnGUI()
    {
        if (_graphView != null)
        {
            _graphView.m_SaveFileName = m_SaveTextField.value;
        }

        if (EditorApplication.isPlaying == false)
        {
            _graphView?.BroadCastResetNodeState(_graphView.m_SaveFileName);
            
        }
        EnumSpace.IsDebugBluePrint = m_IsDebugToggle.value;
        
        //Debug.Log(_graphView.viewTransform.position);
    }
}

public class TempGridGround : GridBackground
{

}


// 创建dialogue graph的底层类
public class DialogueGraphView : GraphView
{
    // 在构造函数里，对GraphView进行一些初始的设置
    public static Vector2 clickPosition;
    GraphViewNodeBase startNode;
    GraphViewNodeBase updateNode;
    public string m_SaveFileName = "Default";
    public Dictionary<int, GraphViewNodeBase> m_CacheDic = new Dictionary<int, GraphViewNodeBase>();

    private Port m_DisconnectPort;
    public DialogueGraphView()
    {
        // 允许对Graph进行Zoom in/out
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        // 允许拖拽Content
        this.AddManipulator(new ContentDragger());
        // 允许Selection里的内容
        this.AddManipulator(new SelectionDragger());
        // GraphView允许进行框选
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new ClickSelector());

        var grid = new TempGridGround();
        Insert(0, grid);
        
        PlayableNodeGraphSearchWindowProvider searchWindowProvider = ScriptableObject.CreateInstance<PlayableNodeGraphSearchWindowProvider>();
        searchWindowProvider.onCreateNode =(type,pos)=> AddDialogueNode(type.Name);
        nodeCreationRequest = (context) =>
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
        };
        
        

    }
    public void OnBroadCastBluePrintTriggerIn(string _graphName, int _nodeUID)
    {
        if (string.Equals(m_SaveFileName,_graphName))
        {
            foreach (var item in nodes)
            {
                GraphViewNodeBase node = item as GraphViewNodeBase;
                if (node != null)
                {
                    if (node.m_Data.m_Guid == _nodeUID)
                    {
                        node.SetMoveState(true);
                        return;
                    }
                }
            }
        }

    }
    public void OnBroadCastBluePrintTriggerOut(string _graphName, int _nodeUID)
    {
        if (string.Equals(m_SaveFileName, _graphName))
        {
            foreach (var item in nodes)
            {
                GraphViewNodeBase node = item as GraphViewNodeBase;
                if (node != null)
                {
                    if (node.m_Data.m_Guid == _nodeUID)
                    {
                        node.SetMoveState(false);
                        return;
                    }
                }

            }
        }
    }

    public void BroadCastResetNodeState(string _graphName)
    {
        if (string.Equals(m_SaveFileName, _graphName))
        {
            foreach (var item in nodes)
            {
                GraphViewNodeBase node = item as GraphViewNodeBase;
                if (node != null)
                {
                    node.SetMoveState(false);
                }
            }
        }
    }
    public void InitFirstNode()
    {
        if (startNode != null)
        {
            RemoveElement(startNode);
        }
        startNode = GenEntryPointNode();

        AddElement(startNode);

        Port port = GenPortForNode(startNode, Direction.Output,Port.Capacity.Multi);

        port.portName = "Out";
        port.portType = typeof(InAndOutPortType);

        startNode.outputContainer.Add(port);

        startNode.m_OutPort = new List<Port>();
        startNode.m_OutPort.Add(port);

        startNode.RefreshExpandedState();
        startNode.RefreshPorts();
        
        if (updateNode != null)
        {
            RemoveElement(updateNode);
        }
        updateNode = GenUpdateNode();
        AddElement(updateNode);
        Port port2 = GenPortForNode(updateNode, Direction.Output,Port.Capacity.Multi);
        port2.portName = "Out";
        port2.portType = typeof(InAndOutPortType);
        updateNode.outputContainer.Add(port2);
        updateNode.m_OutPort = new List<Port>();
        updateNode.m_OutPort.Add(port2);
        updateNode.RefreshExpandedState();
        updateNode.RefreshPorts();

    }
    
    public void InitFirstNodeEditor(GraphEditorNode _graphEditorNode)
    {
        if (startNode != null)
        {
            RemoveElement(startNode);
        }
        GraphViewNodeBase node = new GraphViewNodeBase
        {
            title = "START",
            GUID = DialogueGraphWindow.GenUID(),// 借助System的Guid生成方法
            Text = "ENTRYPOINT",
            Entry = true
        };
        node.m_Data = _graphEditorNode.m_Root.m_Data;
        // node.SetPosition(new Rect(x: _graphEditorNode.m_Root.m_XPos, y: _graphEditorNode.m_Root.m_YPos, width: _graphEditorNode.m_Root.m_Width, height: _graphEditorNode.m_Root.m_Height));
        node.SetPosition(new Rect(_graphEditorNode.m_Root.m_XPos,_graphEditorNode.m_Root.m_YPos,_graphEditorNode.m_Root.m_Width,_graphEditorNode.m_Root.m_Height));

        startNode = node;

        AddElement(startNode);

        Port port = GenPortForNode(startNode, Direction.Output,Port.Capacity.Multi);

        port.portName = "Out";
        port.portType = typeof(InAndOutPortType);

        startNode.outputContainer.Add(port);

        startNode.m_OutPort = new List<Port>();
        startNode.m_OutPort.Add(port);

        startNode.RefreshExpandedState();
        startNode.RefreshPorts();

        
        if (updateNode != null)
        {
            RemoveElement(updateNode);
        }
        updateNode = GenUpdateNode();
        updateNode.m_Data = _graphEditorNode.m_Update.m_Data;
        updateNode.SetPosition(new Rect(_graphEditorNode.m_Update.m_XPos,_graphEditorNode.m_Update.m_YPos,_graphEditorNode.m_Update.m_Width,_graphEditorNode.m_Update.m_Height));

        AddElement(updateNode);
        Port port2 = GenPortForNode(updateNode, Direction.Output,Port.Capacity.Multi);
        port2.portName = "Out";
        port2.portType = typeof(InAndOutPortType);
        updateNode.outputContainer.Add(port2);
        updateNode.m_OutPort = new List<Port>();
        updateNode.m_OutPort.Add(port2);
        updateNode.RefreshExpandedState();
        updateNode.RefreshPorts();
    }

    public void InitConfigNodeTree()
    {

    }

    // 为节点n创建input port或者output port
    // Direction: 是一个简单的枚举，分为Input和Output两种
    private Port GenPortForNode(Node n, Direction portDir, Port.Capacity capacity = Port.Capacity.Single)
    {
        // Orientation也是个简单的枚举，分为Horizontal和Vertical两种，port的数据类型是float
        return n.InstantiatePort(Orientation.Horizontal, portDir, capacity, typeof(float));
    }

    // 比较简单，相当于new了一个Node
    private GraphViewNodeBase GenEntryPointNode()
    {
        GraphViewNodeBase node = new GraphViewNodeBase();
        node.m_Data = new RootNodeData();
        node.m_Data.m_Guid = DialogueGraphWindow.GenUID();
        node.Text = "ENTRYPOINT";
        node.title = "START";
        node.SetPosition(new Rect(x: 0, y: 0, width: 300, height: 300));

        return node;
    }
    
    private GraphViewNodeBase GenUpdateNode()
    {
        GraphViewNodeBase node = new GraphViewNodeBase();
        node.m_Data = new UpdateNodeData();
        node.m_Data.m_Guid = DialogueGraphWindow.GenUID();
        node.title = "UPDATE";
        node.Text = "UPDATEPOINT";
        node.SetPosition(new Rect(x: 0, y: 250, width: 300, height: 300));

        return node;
    }

    public void AddDialogueNode(string nodeName)
    {
        Type o = Type.GetType(nodeName);//加载类型

        object obj = Activator.CreateInstance(o, true);//根据类型创建实例

        GraphViewNodeBase node = obj as GraphViewNodeBase;

        string lastStr = "EditorNode";
        int count = lastStr.Length;
        int length = nodeName.Length;
        string result = nodeName.Substring(0, length - count);

        node.SetUp(DialogueGraphWindow.GenUID(), result, false);
        node.Init(false);
        node.SetPosition(new Rect(clickPosition.x, clickPosition.y, width: 300, height: 300));

        AddElement(node);
    }
    public void AddGraphViewDescNode()
    {
        GraphViewDescNodeBase obj = new GraphViewDescNodeBase();
        
        obj.SetUp(0, "", false);
        obj.Init(false);
        obj.SetPosition(new Rect(clickPosition.x, clickPosition.y, 300, 300));
        obj.layer = -1;
        AddElement(obj);
    }

    public void CopyGraphViewDescNode(GraphViewDescNodeBase _source)
    {
        GraphViewDescNodeBase obj = new GraphViewDescNodeBase();
        
        obj.SetUp(0, _source.Text, false);
        obj.Init(false);
        obj.SetPosition(new Rect(_source.GetPosition().x + 50, _source.GetPosition().y + 50, _source.GetPosition().width, _source.GetPosition().height));
        obj.layer = -1;
        AddElement(obj);
    }
    
    public GraphViewNodeBase CloneDialogueNode(GraphViewNodeBase _node)
    {
        var nodeName = _node.GetType().Name;
        Type o = Type.GetType(nodeName);//加载类型

        object obj = Activator.CreateInstance(o, true);//根据类型创建实例

        GraphViewNodeBase node = obj as GraphViewNodeBase;

        string lastStr = "EditorNode";
        int count = lastStr.Length;
        int length = nodeName.Length;
        string result = nodeName.Substring(0, length - count);

        node.SetUp(DialogueGraphWindow.GenUID(), result, false);
        node.SetPosition(new Rect(_node.GetPosition().x + 25, _node.GetPosition().y + 25, width: 300, height: 300));
        return node;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node &&
            endPort.portType == startPort.portType
        ).ToList();
    }
    /// <summary>
    /// 添加右键菜单事件
    /// </summary>
    /// <param name="evt"></param>
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // base.BuildContextualMenu(evt);
        // evt.menu.RemoveItemAt(1);
        // evt.menu.RemoveItemAt(2);
        // evt.menu.RemoveItemAt(3);
        if (evt.target is UnityEditor.Experimental.GraphView.GraphView && this.nodeCreationRequest != null)
        {
            Type type = typeof(GraphView);
            //BindingFlags类型枚举，BindingFlags.NonPublic | BindingFlags.Instance 组合才能获取到private私有方法
            MethodInfo methodInfo = type.GetMethod("OnContextMenuNodeCreate", BindingFlags.NonPublic | BindingFlags.Instance);
            Action<DropdownMenuAction> action = (Action<DropdownMenuAction>)Delegate.CreateDelegate(typeof(Action<DropdownMenuAction>), this, methodInfo);
            evt.menu.AppendAction("Create Node", action, new Func<DropdownMenuAction, DropdownMenuAction.Status>(DropdownMenuAction.AlwaysEnabled));
            evt.menu.AppendSeparator();
        }

        if (evt.target is UnityEditor.Experimental.GraphView.GraphView || evt.target is Node || evt.target is Group)
        {
            evt.menu.AppendAction("复制粘贴", TestBuildContextualMenu);
        }
        if (evt.target is UnityEditor.Experimental.GraphView.GraphView || evt.target is Node || evt.target is Group)
        {
            evt.menu.AppendAction("添加注释", AddNodeDesc);
        }
        if (evt.target is UnityEditor.Experimental.GraphView.GraphView || evt.target is Node || evt.target is Group)
        {
            evt.menu.AppendAction("复制注释", CopyNodeDesc);
        }
        clickPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        var graphViewNode = evt.target as GraphViewNodeBase;
        if (graphViewNode != null)
        {
            foreach (var VARIABLE in graphViewNode.outputContainer.Children())
            {
                Port port = VARIABLE as Port;
                var pos = port.WorldToLocal(evt.mousePosition);
                if (port != null && port.ContainsPoint(pos))
                {
                    m_DisconnectPort = port;
                    evt.menu.AppendAction("断开 " + port.portName + " 的所有连接", DisconnectOutPortAllLink);
                }
            }
            foreach (var VARIABLE in graphViewNode.inputContainer.Children())
            {
                Port port = VARIABLE as Port;
                var pos = port.WorldToLocal(evt.mousePosition);
                if (port != null && port.ContainsPoint(pos))
                {
                    m_DisconnectPort = port;
                    evt.menu.AppendAction("断开 " + port.portName + " 的所有连接", DisconnectInPortAllLink);
                }
            }
        }
    }

    /// <summary>
    /// 右键菜单Test
    /// </summary>
    /// <param name="obj"></param>
    private void TestBuildContextualMenu(DropdownMenuAction obj)
    {
        //AddDialogueNode(obj.name);
        List<ISelectable> removeSelectables = new List<ISelectable>();
        List<ISelectable> addSelectables = new List<ISelectable>();
        foreach (var VARIABLE in selection)
        {
            GraphViewNodeBase node = VARIABLE as GraphViewNodeBase;
            if (node != null)
            {
                var jsonResult = JsonConvert.SerializeObject(node.m_Data);
                var test = JsonConvert.DeserializeObject<GraphViewRuntimeNodeBase>(jsonResult,new MyJsonConverter_Copy());
                if (test != null)
                {
                    var cloneNode = CloneDialogueNode(node);
                    cloneNode.m_Data = test;
                    cloneNode.Init();
                    AddElement(cloneNode);
                    removeSelectables.Add(node);
                    addSelectables.Add(cloneNode);
                }
            }
            
        }
        

        foreach (var VARIABLE in removeSelectables)
        {
            RemoveFromSelection(VARIABLE);
        }

        foreach (var VARIABLE in addSelectables)
        {
            AddToSelection(VARIABLE);
        }
    }

    void AddNodeDesc(DropdownMenuAction obj)
    {
        AddGraphViewDescNode();
    }
    void CopyNodeDesc(DropdownMenuAction obj)
    {
        if (selection.Count > 0)
        {
            var node = selection[0] as GraphViewDescNodeBase;
            if (node != null)
            {
                CopyGraphViewDescNode(node);
            }
        }
    }

    public void DisconnectOutPortAllLink(DropdownMenuAction obj)
    {
        if (m_DisconnectPort != null)
        {
            foreach (var VARIABLE in m_DisconnectPort.connections)
            {
                if (VARIABLE != null)
                {
                    VARIABLE.input.Disconnect(VARIABLE);
                }
                RemoveElement(VARIABLE);
            }
            m_DisconnectPort.DisconnectAll();
        }
    }
    public void DisconnectInPortAllLink(DropdownMenuAction obj)
    {
        if (m_DisconnectPort != null)
        {
            foreach (var VARIABLE in m_DisconnectPort.connections)
            {
                if (VARIABLE != null)
                {
                    VARIABLE.output.Disconnect(VARIABLE);
                }
                RemoveElement(VARIABLE);
            }
            m_DisconnectPort.DisconnectAll();
        }
    }

    public void GenTreeData()
    {
        //导出编辑器用的配置文件
        ExportEditorJson();

        ExportEditorDescJson();
        
        GraphComponentData graphComponentData = new GraphComponentData();
        //导出运行时配置的文件
        if (startNode != null)
        {
            GraphViewRuntimeNodeBase treeNode = new RootNodeData();
            //第一层是多少个out
            foreach (Port _value in startNode.outputContainer.Children())
            {
                List<int> portList = new List<int>();
                treeNode.m_Childs.Add(_value.portName, portList);
                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                {
                    //第二层是多少个连接节点
                    GraphViewNodeBase node = _edge.input.node as GraphViewNodeBase;
                    //TreeSearchFunForRuntime(node, portList, ref graphComponentData);
                    if (portList.Contains(node.m_Data.m_Guid) == false)
                    {
                        portList.Add(node.m_Data.m_Guid);
                    }
                }

            }
            graphComponentData.m_Root = treeNode;
        }
        foreach (var item in nodes)
        {
            GraphViewNodeBase node = item as GraphViewNodeBase;
            if (node != null)
            {
                ///说明是对应的Node
                if (string.Equals(node.Text,startNode.Text) == false && string.Equals(node.Text,updateNode.Text) == false)
                {
                    //说明不是根节点
                    GraphViewRuntimeNodeBase treeNode = new GraphViewRuntimeNodeBase();
                    treeNode.m_Data = node.m_Data.m_Data;
                    treeNode.m_Name = node.m_Data.m_Name;
                    treeNode.m_Guid = node.m_Data.m_Guid;
                    treeNode.m_Childs = new Dictionary<string, List<int>>();
                    treeNode.m_ParamInPortDic = node.m_Data.m_ParamInPortDic;
                    treeNode.m_ParamOutPortDic = node.m_Data.m_ParamOutPortDic;
                    foreach (Port _value in node.m_OutPort)
                    {
                        List<int> portList = null;
                        if (node.m_Data.m_Childs.ContainsKey(_value.portName))
                        {
                            portList = node.m_Data.m_Childs[_value.portName];
                            portList.Clear();
                            foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                            {
                                //入口节点后是多少个连接节点
                                GraphViewNodeBase tempNode = _edge.input.node as GraphViewNodeBase;
                                if (portList.Contains(tempNode.m_Data.m_Guid) == false)
                                {
                                    portList.Add(tempNode.m_Data.m_Guid);
                                }
                            }
                        }
                        treeNode.m_Childs.Add(_value.portName, portList);
                        
                    }
                    foreach (var UPPER in treeNode.m_ParamInPortDic.Keys)
                    {
                        var value = treeNode.m_ParamInPortDic[UPPER];
                        if (m_CacheDic.TryGetValue(value.m_PreNodeUid,out GraphViewNodeBase nodeBase))
                        {
                            if (graphComponentData.m_AllDic.ContainsKey(nodeBase.m_Data.m_Guid) == false)
                            {
                                graphComponentData.m_AllDic.Add(nodeBase.m_Data.m_Guid, nodeBase.m_Data);
                            }
                        }
                    }
                    if (graphComponentData.m_AllDic.ContainsKey(treeNode.m_Guid) == false)
                    {
                        graphComponentData.m_AllDic.Add(treeNode.m_Guid, treeNode);
                    }
                    foreach (Port _value in node.inputContainer.Children())
                    {
                        if (string.Equals(_value.portName,"In"))
                        {
                            continue;
                        }
                        if (node.m_Data.m_ParamInPortDic.ContainsKey(_value.portName))
                        {
                            ParamPreNodeInfo info = node.m_Data.m_ParamInPortDic[_value.portName];
                            info.m_PreNodeUid = 0;
                            info.m_PreNodeFuncName = "";
                            info.m_PreNodeParamType = 0;
                            foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                            {
                                GraphViewNodeBase tempNode = _edge.output.node as GraphViewNodeBase;
                                info.m_PreNodeUid = tempNode.m_Data.m_Guid;
                                info.m_PreNodeFuncName = _edge.output.portName;
                                info.m_PreNodeParamType = 1;
                                var fieldPort = _edge.output.ClassListContains(PortType.FieldOutPortFlag);
                                if (fieldPort)
                                {
                                    foreach (var VARIABLE in _edge.output.GetClasses())
                                    {
                                        var splitResult = VARIABLE.Split(PortType.FieldOutPortCheck);
                                        if (splitResult.Length > 1)
                                        {
                                            string realName = splitResult[1];
                                            info.m_PreNodeFuncName = realName;
                                            break;
                                        }
                                    }
                                }
                                foreach (Port outputPort in tempNode.outputContainer.Children())
                                {
                                    if (outputPort == _edge.output)
                                    {
                                        //说明不是从属性值输出的
                                        info.m_PreNodeParamType = 0;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        if (updateNode != null)
        {
            GraphViewRuntimeNodeBase treeNode = new RootNodeData();
            //第一层是多少个out
            foreach (Port _value in updateNode.outputContainer.Children())
            {
                List<int> portList = new List<int>();
                treeNode.m_Childs.Add(_value.portName, portList);
                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                {
                    //第二层是多少个连接节点
                    GraphViewNodeBase node = _edge.input.node as GraphViewNodeBase;
                    //TreeSearchFunForRuntime(node, portList, ref graphComponentData);
                    if (portList.Contains(node.m_Data.m_Guid) == false)
                    {
                        portList.Add(node.m_Data.m_Guid);
                    }
                }

            }
            graphComponentData.m_UpdateRoot = treeNode;
        }
        SerilizeDataToJson(graphComponentData);
    } 
    
    
    public void ExportEditorJson()
    {
        GraphEditorNode graphEditorNode = new GraphEditorNode();
        if (startNode != null)
        {
            graphEditorNode.m_Root = new SerilizeEditorNode();
            graphEditorNode.m_Root.m_XPos = startNode.GetPosition().x;
            graphEditorNode.m_Root.m_YPos = startNode.GetPosition().y;
            graphEditorNode.m_Root.m_Width = startNode.GetPosition().width;
            graphEditorNode.m_Root.m_Height = startNode.GetPosition().height;
            graphEditorNode.m_Root.m_Data = startNode.m_Data;
            graphEditorNode.m_Root.m_EditorClassName = "RootNodeData";

            graphEditorNode.m_MaxGUID = DialogueGraphWindow.GenUID();
            
            graphEditorNode.m_Root.m_Childs = new Dictionary<string, List<int>>();
            foreach (Port _value in startNode.outputContainer.Children())
            {
                List<int> portList;
                if (graphEditorNode.m_Root.m_Data.m_Childs.ContainsKey(_value.portName) == false)
                {
                    portList = new List<int>();
                    graphEditorNode.m_Root.m_Data.m_Childs.Add(_value.portName, portList);
                }
                else
                {
                    portList = graphEditorNode.m_Root.m_Data.m_Childs[_value.portName];
                }
                portList.Clear();
                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                {
                    //入口节点后是多少个连接节点
                    GraphViewNodeBase node = _edge.input.node as GraphViewNodeBase;
                    if (portList.Contains(node.m_Data.m_Guid) == false)
                    {
                        portList.Add(node.m_Data.m_Guid);
                    }
                    
                }
            }

            graphEditorNode.m_AllDic = new Dictionary<int, SerilizeEditorNode>();
            foreach (var item in nodes)
            {
                GraphViewNodeBase node = item as GraphViewNodeBase;
                if (node != null)
                {
                    ///说明是对应的Node
                    if (string.Equals(node.Text,startNode.Text) == false && string.Equals(node.Text,updateNode.Text) == false)
                    {
                        //说明不是根节点
                        SerilizeEditorNode serilizeEditorNode = new SerilizeEditorNode();
                        serilizeEditorNode.m_XPos = node.GetPosition().x;
                        serilizeEditorNode.m_YPos = node.GetPosition().y;
                        serilizeEditorNode.m_Width = node.GetPosition().width;
                        serilizeEditorNode.m_Height = node.GetPosition().height;
                        serilizeEditorNode.m_Data = node.m_Data;
                        serilizeEditorNode.m_EditorClassName = node.GetType().Name;
                        foreach (Port _value in node.outputContainer.Children())
                        {
                            List<int> portList;
                            if (serilizeEditorNode.m_Data.m_Childs.ContainsKey(_value.portName))
                            {
                                portList = serilizeEditorNode.m_Data.m_Childs[_value.portName];
                                portList.Clear();
                                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                                {
                                    //入口节点后是多少个连接节点
                                    GraphViewNodeBase tempNode = _edge.input.node as GraphViewNodeBase;
                                    if (portList.Contains(tempNode.m_Data.m_Guid) == false)
                                    {
                                        portList.Add(tempNode.m_Data.m_Guid);
                                    }
                                }
                            }
                            
                        }
                        foreach (Port _value in node.inputContainer.Children())
                        {
                            if (string.Equals(_value.portName,"In"))
                            {
                                continue;
                            }
                            if (serilizeEditorNode.m_Data.m_ParamInPortDic.ContainsKey(_value.portName))
                            {
                                ParamPreNodeInfo info = serilizeEditorNode.m_Data.m_ParamInPortDic[_value.portName];
                                info.m_PreNodeUid = 0;
                                info.m_PreNodeFuncName = "";
                                info.m_PreNodeParamType = 0;
                                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                                {
                                    GraphViewNodeBase tempNode = _edge.output.node as GraphViewNodeBase;
                                    info.m_PreNodeUid = tempNode.m_Data.m_Guid;
                                    info.m_PreNodeFuncName = _edge.output.portName;
                                    info.m_PreNodeParamType = 1;
                                    var fieldPort = _edge.output.ClassListContains(PortType.FieldOutPortFlag);
                                    if (fieldPort)
                                    {
                                        foreach (var VARIABLE in _edge.output.GetClasses())
                                        {
                                            var splitResult = VARIABLE.Split(PortType.FieldOutPortCheck);
                                            if (splitResult.Length > 1)
                                            {
                                                string realName = splitResult[1];
                                                info.m_PreNodeFuncName = realName;
                                                break;
                                            }
                                        }
                                    }
                                    foreach (Port outputPort in tempNode.outputContainer.Children())
                                    {
                                        if (outputPort == _edge.output)
                                        {
                                            //说明不是从属性值输出的
                                            info.m_PreNodeParamType = 0;
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        if (graphEditorNode.m_AllDic.ContainsKey(node.m_Data.m_Guid) == false)
                        {
                            graphEditorNode.m_AllDic.Add(node.m_Data.m_Guid,serilizeEditorNode);
                        }
                    }
                }
            }
        }
        if (updateNode != null)
        {
            graphEditorNode.m_Update = new SerilizeEditorNode();
            graphEditorNode.m_Update.m_XPos = updateNode.GetPosition().x;
            graphEditorNode.m_Update.m_YPos = updateNode.GetPosition().y;
            graphEditorNode.m_Update.m_Width = updateNode.GetPosition().width;
            graphEditorNode.m_Update.m_Height = updateNode.GetPosition().height;
            graphEditorNode.m_Update.m_Data = updateNode.m_Data;
            graphEditorNode.m_Update.m_EditorClassName = "UpdateNodeData";

            graphEditorNode.m_MaxGUID = DialogueGraphWindow.GenUID();
            
            graphEditorNode.m_Update.m_Childs = new Dictionary<string, List<int>>();
            foreach (Port _value in updateNode.outputContainer.Children())
            {
                List<int> portList;
                if (graphEditorNode.m_Update.m_Data.m_Childs.ContainsKey(_value.portName) == false)
                {
                    portList = new List<int>();
                    graphEditorNode.m_Update.m_Data.m_Childs.Add(_value.portName, portList);
                }
                else
                {
                    portList = graphEditorNode.m_Update.m_Data.m_Childs[_value.portName];
                }
                portList.Clear();
                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                {
                    //入口节点后是多少个连接节点
                    GraphViewNodeBase node = _edge.input.node as GraphViewNodeBase;
                    if (portList.Contains(node.m_Data.m_Guid) == false)
                    {
                        portList.Add(node.m_Data.m_Guid);
                    }
                }
            }
        }
        SerilizeDataToJsonEditorVersion<GraphEditorNode>(graphEditorNode);
    }

    public void ExportEditorDescJson()
    { 
        GraphEditorDescList root = new GraphEditorDescList();
        foreach (var item in nodes)
        {
            GraphViewDescNodeBase node = item as GraphViewDescNodeBase;
            if (node != null)
            {
                SerilizeEditorDescNode newNode = new SerilizeEditorDescNode();
                newNode.m_Text = node.Text;
                newNode.m_XPos = node.GetPosition().x;
                newNode.m_YPos = node.GetPosition().y;
                newNode.m_Width = node.GetPosition().width;
                newNode.m_Height = node.GetPosition().height;
                root.m_List.Add(newNode);
            }
        }
        string json = JsonConvert.SerializeObject(root);
        if (string.IsNullOrEmpty(json) == false)
        {
            string directoryPath = EditorPath.EDITOR_DESC_JSON_PATH;
            if (Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = directoryPath + "/" + m_SaveFileName + ".json";
            System.IO.File.WriteAllText(filePath, json);
        }
    }
    public void SerilizeDataToJsonEditorVersion<T>(T _node)
    {
        //string json = LitJson.JsonMapper.ToJson(_node);
        string json = JsonConvert.SerializeObject(_node);
        if (string.IsNullOrEmpty(json) == false)
        {
            string directoryPath = EditorPath.EDITOR_JSON_PATH;
            if (Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = directoryPath + "/" + m_SaveFileName + ".json";
            System.IO.File.WriteAllText(filePath, json);

        }
    }
    public void InitTreeByDataEditor()
    {
        m_CacheDic.Clear();
        GraphEditorNode fileDataLoad = SerilizeJsonToDataEditor();
        if (fileDataLoad != null)
        {
            DialogueGraphWindow.InitUID(fileDataLoad.m_MaxGUID);
            //先加载初始节点
            InitFirstNodeEditor(fileDataLoad);

            SerilizeEditorNode node = fileDataLoad.m_Root;
            
            ///先将所有节点创建出来，然后再从根节点开始往后走

            foreach (var VARIABLE in fileDataLoad.m_AllDic.Keys)
            {
                var item = fileDataLoad.m_AllDic[VARIABLE];
                CreateEditorNode(item);
            }

            //这里处理根节点后续的子节点连接
            foreach (var portItem in node.m_Data.m_Childs.Keys)
            {
                foreach (var child in node.m_Data.m_Childs[portItem])
                {
                    if (fileDataLoad.m_AllDic.ContainsKey(child))
                    {
                        //TreeSearchBefore(fileDataLoad.m_AllDic[child], startNode, 0,fileDataLoad);
                        //
                        SerilizeEditorNode childEditorNode = fileDataLoad.m_AllDic[child];
                        GraphViewNodeBase graphViewNodeBase = CreateEditorNode(childEditorNode);
                        Edge edge = new Edge();
                        edge.input = graphViewNodeBase.m_InPort;
                        edge.output = startNode.m_OutPort[0];
                        startNode.m_OutPort[0].Connect(edge);
                        graphViewNodeBase.m_InPort.Connect(edge);
                        AddElement(edge);
                    }
                }
            }
            
            SerilizeEditorNode mUpdate = fileDataLoad.m_Update;
            //这里处理逻辑帧节点后续的子节点连接
            foreach (var portItem in mUpdate.m_Data.m_Childs.Keys)
            {
                foreach (var child in mUpdate.m_Data.m_Childs[portItem])
                {
                    if (fileDataLoad.m_AllDic.ContainsKey(child))
                    {
                        //TreeSearchBefore(fileDataLoad.m_AllDic[child], startNode, 0,fileDataLoad);
                        //
                        SerilizeEditorNode childEditorNode = fileDataLoad.m_AllDic[child];
                        GraphViewNodeBase graphViewNodeBase = CreateEditorNode(childEditorNode);
                        Edge edge = new Edge();
                        edge.input = graphViewNodeBase.m_InPort;
                        edge.output = updateNode.m_OutPort[0];
                        updateNode.m_OutPort[0].Connect(edge);
                        graphViewNodeBase.m_InPort.Connect(edge);
                        AddElement(edge);
                    }
                }
            }
            
            foreach (var VARIABLE in fileDataLoad.m_AllDic.Keys)
            {
                var item = fileDataLoad.m_AllDic[VARIABLE];
                
                GraphViewNodeBase parent = CreateEditorNode(item);
                //获取到这个节点的每个输出口
                foreach (var key in item.m_Data.m_Childs.Keys)
                {
                    foreach (var child in item.m_Data.m_Childs[key])
                    {
                        int index = -1;
                        for (int i = 0;i < parent.m_OutPort.Count;i++)
                        {
                            if (parent.m_OutPort[i].portName == key)
                            {
                                index = i;
                                break;
                            }
                        }
                        if (index >= 0)
                        {
                            if (fileDataLoad.m_AllDic.ContainsKey(child))
                            {
                                GraphViewNodeBase childNodeBase = CreateEditorNode(fileDataLoad.m_AllDic[child]);
                            
                                Edge edge = new Edge();
                                edge.input = childNodeBase.m_InPort;
                                edge.output = parent.m_OutPort[index];
                                parent.m_OutPort[index].Connect(edge);
                                childNodeBase.m_InPort.Connect(edge);
                                AddElement(edge);
                            }
                        }
                    }
                }

                //处理下每个节点的对应的参数输入接口的连线
                ParamDataInit(item);

            }
        }
        
        
        GraphEditorDescList fileDataLoad2 = SerilizeJsonDescData();
        if (fileDataLoad2 != null)
        {
            for (int i = 0; i < fileDataLoad2.m_List.Count; i++)
            {
                GraphViewDescNodeBase obj = new GraphViewDescNodeBase();
                obj.SetUp(0, fileDataLoad2.m_List[i].m_Text, false);
                obj.Init(false);
                obj.SetPosition(new Rect(fileDataLoad2.m_List[i].m_XPos, fileDataLoad2.m_List[i].m_YPos, fileDataLoad2.m_List[i].m_Width, fileDataLoad2.m_List[i].m_Height));
                obj.layer = -1;
                AddElement(obj);
            }
        }
    }
    public void SerilizeDataToJson(GraphViewRuntimeNodeBase _node)
    {
        //string json = LitJson.JsonMapper.ToJson(_node);
        string json = JsonConvert.SerializeObject(_node);
        if (string.IsNullOrEmpty(json) == false)
        {
            string directoryPath = EditorPath.RUNTIME_JSON_PATH;
            if (Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = directoryPath + "/" + m_SaveFileName + ".json";
            System.IO.File.WriteAllText(filePath, json);

        }
    }
    public void SerilizeDataToJson(GraphComponentData _node)
    {
        //string json = LitJson.JsonMapper.ToJson(_node);
        string json = JsonConvert.SerializeObject(_node);
        if (string.IsNullOrEmpty(json) == false)
        {
            string directoryPath = EditorPath.RUNTIME_JSON_PATH;
            if (Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = directoryPath + "/" + m_SaveFileName + ".json";
            System.IO.File.WriteAllText(filePath, json);

        }
    }
    public GraphEditorNode SerilizeJsonToDataEditor()
    {
        GraphEditorNode _node = null;
        string readData;
        string fileUrl = EditorPath.EDITOR_JSON_PATH;
        string filePath = fileUrl + "/" + m_SaveFileName +".json";
        using (StreamReader sr = File.OpenText(filePath))
        {
            //数据保存
            readData = sr.ReadToEnd();
            sr.Close();
            _node = JsonConvert.DeserializeObject<GraphEditorNode>(readData, new MyJsonConverter_Editor());
        }
        return _node;
    }
    public GraphEditorDescList SerilizeJsonDescData()
    {
        GraphEditorDescList _node = null;
        string readData;
        string fileUrl = EditorPath.EDITOR_DESC_JSON_PATH;
        string filePath = fileUrl + "/" + m_SaveFileName +".json";
        if (File.Exists(filePath))
        {
            using (StreamReader sr = File.OpenText(filePath))
            {
                //数据保存
                readData = sr.ReadToEnd();
                sr.Close();
                _node = JsonConvert.DeserializeObject<GraphEditorDescList>(readData);
            }
        }
        return _node;
    }

    public void TreeSearchFunForEditor(GraphViewNodeBase _treeNode,List<SerilizeNode> _childList)
    {
        SerilizeNode treeNode = new SerilizeNode();
        treeNode.m_Data = _treeNode.m_Data;
        treeNode.m_Name = _treeNode.GetType().Name;
        treeNode.m_Childs = new Dictionary<string, List<SerilizeNode>>();
        treeNode.m_XPos = _treeNode.GetPosition().x;
        treeNode.m_YPos = _treeNode.GetPosition().y;
        treeNode.m_Width = _treeNode.GetPosition().width;
        treeNode.m_Height = _treeNode.GetPosition().height;
        _childList.Add(treeNode);
        foreach (Port _value in _treeNode.outputContainer.Children())
        {
            List<SerilizeNode> portList;
            if (treeNode.m_Childs.ContainsKey(_value.portName))
            {
                portList = treeNode.m_Childs[_value.portName];
                portList.Clear();
            }
            else
            {
                portList = new List<SerilizeNode>();
                treeNode.m_Childs.Add(_value.portName, portList);
            }
            
            foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
            {
                //第二层是多少个连接节点
                GraphViewNodeBase node = _edge.input.node as GraphViewNodeBase;
                TreeSearchFunForEditor(node, portList);
            }

        } 
    }
    public void TreeSearchFunForRuntime(GraphViewNodeBase _treeNode, List<int> _childList,ref GraphComponentData _graphComponentData)
    {
        GraphViewRuntimeNodeBase treeNode = new GraphViewRuntimeNodeBase();
        treeNode.m_Data = _treeNode.m_Data.m_Data;
        treeNode.m_Name = _treeNode.m_Data.m_Name;
        treeNode.m_Guid = _treeNode.m_Data.m_Guid;
        treeNode.m_Childs = new Dictionary<string, List<int>>();
        treeNode.m_ParamInPortDic = _treeNode.m_Data.m_ParamInPortDic;
        treeNode.m_ParamOutPortDic = _treeNode.m_Data.m_ParamOutPortDic;
        _childList.Add(treeNode.m_Guid);
        foreach (Port _value in _treeNode.outputContainer.Children())
        {
            if (_value.portType == typeof(InAndOutPortType))
            {
                List<int> portList;
                if (treeNode.m_Childs.ContainsKey(_value.portName))
                {
                    portList = treeNode.m_Childs[_value.portName];
                    portList.Clear();
                }
                else
                {
                    portList = new List<int>();
                    treeNode.m_Childs.Add(_value.portName, portList);
                }

                foreach (UnityEditor.Experimental.GraphView.Edge _edge in _value.connections)
                {
                    //第二层是多少个连接节点
                    GraphViewNodeBase node = _edge.input.node as GraphViewNodeBase;
                    TreeSearchFunForRuntime(node, portList, ref _graphComponentData);
                }
            }
            else
            {
                //这里处理参数输出口相关参数
            }

        }

        foreach (var UPPER in treeNode.m_ParamInPortDic.Keys)
        {
            var value = treeNode.m_ParamInPortDic[UPPER];
            if (m_CacheDic.TryGetValue(value.m_PreNodeUid,out GraphViewNodeBase nodeBase))
            {
                if (_graphComponentData.m_AllDic.ContainsKey(nodeBase.m_Data.m_Guid) == false)
                {
                    _graphComponentData.m_AllDic.Add(nodeBase.m_Data.m_Guid, nodeBase.m_Data);
                }
            }
        }
        if (_graphComponentData.m_AllDic.ContainsKey(treeNode.m_Guid) == false)
        {
            _graphComponentData.m_AllDic.Add(treeNode.m_Guid, treeNode);
        }
    }

    public GraphViewNodeBase CreateEditorNode(SerilizeEditorNode _serilizeNode)
    {
        GraphViewNodeBase graphViewNodeBase;
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
        string graphViewNodeName = _serilizeNode.m_EditorClassName;


        Type runtimeNodeType = assembly.GetType(_serilizeNode.m_Data.m_Name);//加载类型
        if (m_CacheDic.ContainsKey(_serilizeNode.m_Data.m_Guid))
        {
            graphViewNodeBase = m_CacheDic[_serilizeNode.m_Data.m_Guid];
        }
        else
        {
            Type o = Type.GetType(graphViewNodeName);//加载类型
            graphViewNodeBase = Activator.CreateInstance(o, true) as GraphViewNodeBase;//根据类型创建实例
            m_CacheDic.Add(_serilizeNode.m_Data.m_Guid, graphViewNodeBase);
            
            string lastStr = "EditorNode";
            int count = lastStr.Length;
            int length = graphViewNodeName.Length;
            string result = graphViewNodeName.Substring(0, length - count);
            graphViewNodeBase.SetUp(_serilizeNode.m_Data.m_Guid, result, false);
            graphViewNodeBase.m_Data = _serilizeNode.m_Data;
            graphViewNodeBase.Init(false);
            AddElement(graphViewNodeBase);
        }
        graphViewNodeBase.SetPosition(new Rect(_serilizeNode.m_XPos, _serilizeNode.m_YPos, _serilizeNode.m_Width, _serilizeNode.m_Height));
        return graphViewNodeBase;
    }
    public void TreeSearchBefore(SerilizeEditorNode _serilizeNode,GraphViewNodeBase _parent,int _portIndex,GraphEditorNode _graphEditorNode)
    {
        GraphViewNodeBase graphViewNodeBase = CreateEditorNode(_serilizeNode);
        
        Edge edge = new Edge();
        edge.input = graphViewNodeBase.m_InPort;
        edge.output = _parent.m_OutPort[_portIndex];
        _parent.m_OutPort[_portIndex].Connect(edge);
        graphViewNodeBase.m_InPort.Connect(edge);
        AddElement(edge);
        //edge.output = _parent.m_OutPort;
        //_parent.m_OutPort.Connect(edge);

        foreach (var key in _serilizeNode.m_Data.m_Childs.Keys)
        {
            foreach (var child in _serilizeNode.m_Data.m_Childs[key])
            {
                int index = -1;
                for (int i = 0;i < graphViewNodeBase.m_OutPort.Count;i++)
                {
                    if (graphViewNodeBase.m_OutPort[i].portName == key)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    TreeSearchBefore(_graphEditorNode.m_AllDic[child], graphViewNodeBase, index,_graphEditorNode);
                }
                
            }
        }
    }

    public void ParamDataInit(SerilizeEditorNode _serilizeNode)
    {
        foreach (var key in _serilizeNode.m_Data.m_ParamInPortDic.Keys)
        {
            var value = _serilizeNode.m_Data.m_ParamInPortDic[key];
            int preNodeUid = value.m_PreNodeUid;
            string preNodeFuncName = value.m_PreNodeFuncName;
            string preNodeTypeName = value.m_PreNodeTypeName;
            int preNodeParamType = value.m_PreNodeParamType;

            if (m_CacheDic.TryGetValue(_serilizeNode.m_Data.m_Guid,out GraphViewNodeBase _curNode))
            {
                Port paramInport = null;
                foreach (Port VARIABLE in _curNode.inputContainer.Children())
                {
                    if (string.Equals(VARIABLE.portName,key) && VARIABLE.portType == PortType.GetTypeByString(preNodeTypeName))
                    {
                        paramInport = VARIABLE;
                        break;
                    }
                }
               
                if (m_CacheDic.TryGetValue(preNodeUid,out GraphViewNodeBase _resultNode))
                {
                    if (preNodeParamType == 0)
                    {
                        foreach (Port VARIABLE in _resultNode.outputContainer.Children())
                        {
                            if (VARIABLE.portType == PortType.GetTypeByString(preNodeTypeName) && string.Equals(VARIABLE.portName,preNodeFuncName))
                            {
                                //说明参数是连接的是这个节点的参数输出口
                                Edge paramEdge = new Edge();
                                paramEdge.input = paramInport;
                                paramEdge.output = VARIABLE;
                                paramInport.Connect(paramEdge);
                                VARIABLE.Connect(paramEdge);
                                AddElement(paramEdge);
                            }
                        }
                    }
                    if (preNodeParamType == 1)
                    {
                        foreach (var VARIABLE in _resultNode.mainContainer.Children())
                        {
                            if (VARIABLE.name == "FieldMain")
                            {
                                foreach (var _port in VARIABLE.Children())
                                {
                                    if (_port.GetType() == typeof(Port))
                                    {
                                        var port = (Port)_port;
                                        foreach (var _portClassName in port.GetClasses())
                                        {
                                            var splitResult = _portClassName.Split(PortType.FieldOutPortCheck);
                                            if (splitResult.Length > 1)
                                            {
                                                string realName = splitResult[1];
                                                if (port.portType == PortType.GetTypeByString(preNodeTypeName) && string.Equals(realName,preNodeFuncName))
                                                {
                                                    //说明参数是连接的是这个节点的参数输出口
                                                    Edge paramEdge = new Edge();
                                                    paramEdge.input = paramInport;
                                                    paramEdge.output = port;
                                                    paramInport.Connect(paramEdge);
                                                    port.Connect(paramEdge);
                                                    AddElement(paramEdge);
                                                }
                                                break;
                                            }
                                        }
                                        
                                    }
                                }
                            }
                        }
                    }
                    
                }
            }
            

        }
    }
}   

public class PlayableNodeGraphSearchWindowProvider:ScriptableObject, ISearchWindowProvider
{
    public Action<Type, Vector2> onCreateNode;
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var entries = new List<SearchTreeEntry>();
        entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));

        string lastStr = "EditorNode";
        var assembly = typeof(GraphViewNodeBase).Assembly;
        var types = assembly.GetTypes().Where(type => type.Name.EndsWith(lastStr));

        foreach (var type in types)
        {

            int count = lastStr.Length;
            int length = type.Name.Length;
            string result = type.Name.Substring(0, length - count);
            var temp = new SearchTreeEntry(new GUIContent(result))
            {
                level = 1,
                userData = type
            };
            var custom = type.GetCustomAttribute<CustomAttribute>();
            if (custom != null)
            {
                temp.content.text = custom.DisplayName + " (" + type.Name + ")";
            }
            entries.Add(temp);
        }
        return entries;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        // Debug.Log(SearchTreeEntry.userData);
        // context.screenMousePosition
        onCreateNode?.Invoke(SearchTreeEntry.userData as Type, context.screenMousePosition);
        return true;
    }
}
