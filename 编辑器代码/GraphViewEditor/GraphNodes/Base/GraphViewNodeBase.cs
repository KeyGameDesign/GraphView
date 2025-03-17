using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphViewNodeBase : Node
{
    public int GUID;
    public string Text;
    public bool Entry = false;
    public GraphViewRuntimeNodeBase m_Data;

    public string m_ChinaName;

    public Port m_InPort;
    public List<Port> m_OutPort = new List<Port>();
    
    public List<Port> m_ParamOutPorts = new List<Port>();

    public void SetUp(int _guid, string _text, bool _entry)
    {
        title = _text;
        GUID = _guid;
        Text = _text;
        Entry = _entry;
    }

    public void SetMoveState(bool _isShow)
    {
        if (_isShow)
        {
            var selectColor = new Color(0.744f, 1, 0, 1);
            mainContainer.style.borderBottomColor = new StyleColor(selectColor);
            mainContainer.style.borderLeftColor = new StyleColor(selectColor);
            mainContainer.style.borderTopColor = new StyleColor(selectColor);
            mainContainer.style.borderRightColor = new StyleColor(selectColor);
        }
        else
        {
            var selectColor = new Color(0.2196079f, 0.2196079f, 0.2196079f, 1);
            mainContainer.style.borderBottomColor = new StyleColor(selectColor);
            mainContainer.style.borderLeftColor = new StyleColor(selectColor);
            mainContainer.style.borderTopColor = new StyleColor(selectColor);
            mainContainer.style.borderRightColor = new StyleColor(selectColor);
        }
    }
    /// <summary>
    /// 节点初始化方法，派生类重载这个方法来根据不同数据进行绘制可视化节点结构
    /// </summary>
    public virtual void Init(bool isResetGuid = true)
    {
        expanded = true;
        var element = this.Q("selection-border", (string) null);
        if (element != null)
            element.style.overflow = (StyleEnum<Overflow>) Overflow.Hidden;
        m_OutPort.Clear();
        m_ParamOutPorts.Clear();
        m_Data.m_Guid = GUID;
        var editorNodeType = GetType();
        if (editorNodeType.GetCustomAttribute<CustomAttribute>() != null)
        {
            var customAttribute = editorNodeType.GetCustomAttribute<CustomAttribute>();
            title = customAttribute.DisplayName  + " (UID:" + GUID + ")";
        }
        else
        {
            title = Text + " (UID:" + GUID + ")";
        }
        System.Reflection.FieldInfo[] fields = m_Data.m_Data.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        foreach (System.Reflection.FieldInfo item in fields)
        {
            string name = item.Name; //名称
            int beforeIndex = name.IndexOf("_");
            string realName = name.Substring(beforeIndex + 1);
            MethodInfo fieldMethodInfo = m_Data.m_Data.GetType().GetMethod(realName);
            object value = item.GetValue(m_Data.m_Data);  //值

            var tests = item.GetCustomAttribute<CustomAttribute>();
            if (tests == null)
            {
                continue;
            }
            string displayName = tests.DisplayName;

            if (item.FieldType == typeof(System.Int32) || item.FieldType == typeof(System.Single) || item.FieldType == typeof(System.String))
            {
                Label labelDisplayName = new Label(displayName);
                labelDisplayName.style.position = Position.Relative;
                labelDisplayName.style.width = 70;
                TextField levelIdTextField = new TextField(); 
                levelIdTextField.value = value.ToString();  
                levelIdTextField.style.position = Position.Relative;
                //levelIdTextField.style.width = 100;
                levelIdTextField.style.left = 70;
                levelIdTextField.style.top = -15;
                levelIdTextField.RegisterValueChangedCallback((_value) =>
                {
                    if (string.IsNullOrEmpty(_value.newValue) == false)
                    {
                        if (item.FieldType == typeof(System.Int32))
                        {

                            item.SetValue(m_Data.m_Data, System.Int32.Parse(_value.newValue));
                        }
                        if (item.FieldType == typeof(System.Single))
                        {

                            item.SetValue(m_Data.m_Data, System.Single.Parse(_value.newValue));
                        }
                        if (item.FieldType == typeof(System.String))
                        {

                            item.SetValue(m_Data.m_Data, _value.newValue);
                        }

                    }

                });


                VisualElement root = new VisualElement();
                root.name = "FieldMain";
                //root.StretchToParentWidth();
                root.Add(labelDisplayName);
                root.Add(levelIdTextField);
                root.style.width = 200;

                if (fieldMethodInfo != null)
                {
                    var test = GenPortForNode(Direction.Output, Port.Capacity.Multi);
                    test.AddToClassList(PortType.FieldOutPortFlag);
                    test.AddToClassList(PortType.FieldOutPortCheck + realName);
                    test.style.position = Position.Relative;
                    test.style.left = 0;
                    test.style.top = -35;
                    test.portType = item.FieldType;
                    test.portName = "";
                    root.Add(test);
                }
                
                //root.Add(test);
                mainContainer.Add(root);
                mainContainer.style.color = new StyleColor(new Color(1,1,1,1));
                mainContainer.style.backgroundColor = new StyleColor(new Color(0.2196079f, 0.2196079f, 0.2196079f, 1));
            }
            if (item.FieldType == typeof(System.Boolean))
            {
                Label labelDisplayName = new Label(displayName);
                labelDisplayName.style.position = Position.Relative;
                labelDisplayName.style.width = 70;
                Toggle toggle = new Toggle();
                toggle.value = (bool)value;
                toggle.style.position = Position.Relative;
                toggle.style.left = 70;
                toggle.style.top = -15;
                toggle.RegisterValueChangedCallback((_value) =>
                {
                    item.SetValue(m_Data.m_Data, _value.newValue);

                });
                mainContainer.Add(labelDisplayName);
                mainContainer.Add(toggle);
                
            }

        }

        // 2. 为其创建InputPort
        m_InPort = GenPortForNode(Direction.Input, Port.Capacity.Multi);
        m_InPort.portName = "In";
        m_InPort.portType = typeof(InAndOutPortType);
        inputContainer.Add(m_InPort);
        foreach (string key in m_Data.m_Childs.Keys)
        {
            var outPort = GenPortForNode(Direction.Output, Port.Capacity.Multi);
            outPort.portName = key;
            outPort.portType = typeof(InAndOutPortType);
            outputContainer.Add(outPort);
            m_OutPort.Add(outPort);
        }
        //根据参数输入接口字典生成对应的接口
        foreach (var VARIABLE in m_Data.m_ParamInPortDic.Keys)
        {
            ParamPreNodeInfo tempData = m_Data.m_ParamInPortDic[VARIABLE];
            var paramInport = GenPortForNode(Direction.Input, Port.Capacity.Single);
            paramInport.portName = VARIABLE;
            paramInport.portType = PortType.GetTypeByString(tempData.m_PreNodeTypeName);
            inputContainer.Add(paramInport);
        }
        //根据参数输出口字典生成对应的接口
        foreach (var VARIABLE in m_Data.m_ParamOutPortDic)
        {
            var paramInport = GenPortForNode(Direction.Output, Port.Capacity.Multi);
            paramInport.portName = VARIABLE.m_ParamOutPortName;
            paramInport.portType = PortType.GetTypeByString(VARIABLE.m_ParamOutPortType);
            outputContainer.Add(paramInport);
        }
        //m_OutPort = GenPortForNode(Direction.Output, Port.Capacity.Multi);
        //m_OutPort.portName = "Out";
        //outputContainer.Add(m_OutPort);
        RefreshExpandedState();
        RefreshPorts();
    }
    public Port GenPortForNode(Direction portDir, Port.Capacity capacity = Port.Capacity.Single)
    {
        // Orientation也是个简单的枚举，分为Horizontal和Vertical两种，port的数据类型是float
        return this.InstantiatePort(Orientation.Horizontal, portDir, capacity, typeof(float));
    }
}
