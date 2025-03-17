using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

[Custom("测试用")]
public class Test1EditorNode : GraphViewNodeBase
{
    public Test1EditorNode()
    {
        m_Data = new Test1();
        m_Data.m_Data = new Test1Data();
    }
}