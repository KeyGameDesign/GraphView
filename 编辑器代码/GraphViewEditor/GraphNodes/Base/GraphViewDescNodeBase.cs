using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphViewDescNodeBase : Node
{
    /// <summary>
    /// 这个是拥有这个描述节点的拥有者的节点UID
    /// </summary>
    public int GUID;
    /// <summary>
    /// 描述文本框
    /// </summary>
    public string Text;
    
    public void SetUp(int _guid, string _text, bool _entry)
    {
        title = _text;
        GUID = _guid;
        Text = _text;
    }

    /// <summary>
    /// 节点初始化方法，派生类重载这个方法来根据不同数据进行绘制可视化节点结构
    /// </summary>
    public void Init(bool isResetGuid = true)
    {
        titleContainer.visible = false;
        inputContainer.visible = false;
        outputContainer.visible = false;
        expanded = true;
        var element = this.Q("selection-border", (string) null);
        if (element != null)
            element.style.overflow = (StyleEnum<Overflow>) Overflow.Hidden;

        TextField levelIdTextField = new TextField(500, true, false, default(char)); 
        levelIdTextField.value = Text;  
        levelIdTextField.style.position = Position.Relative;
        levelIdTextField.style.width = 200;
        levelIdTextField.style.height = 200;
        levelIdTextField.style.left = 0;
        levelIdTextField.style.top = 0;
        levelIdTextField.multiline = true;
        levelIdTextField.SetVerticalScrollerVisibility(ScrollerVisibility.AlwaysVisible);
        levelIdTextField.style.color = new StyleColor(new Color(0, 0, 0, 1));
        levelIdTextField.RegisterValueChangedCallback((_value) =>
        {
            Text = _value.newValue;
        });
        mainContainer.Add(levelIdTextField);
        mainContainer.style.color = new StyleColor(new Color(1,1,1,1));
        mainContainer.style.backgroundColor = new StyleColor(new Color(1, 1, 0, 1));
        RefreshExpandedState();
    }
}
