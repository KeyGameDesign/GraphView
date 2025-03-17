using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Scripting.ScriptCompilation;


class NodeBase
{
    public string m_TypeName;
    public string m_TypePropertyName;
    public string m_Type;
    public object m_Value;
    public bool m_HadOutPort;
}

public class ParamInPortBase
{
    public string m_InPortName;
    public string m_TypeName;
}

public class NodeEditorWindow : EditorWindow
{
    // [MenuItem("Graph/打开节点编辑界面")]
    // public static void OpenNodeEditorWindow()
    // {
    //     // 定义了创建并打开Window的方法
    //     var window = GetWindow<NodeEditorWindow>();
    //     window.titleContent = new GUIContent("节点编辑界面");
    // }
    List<NodeBase> m_objects = new List<NodeBase>();

    List<string> m_OutPortList = new List<string>();

    private List<ParamInPortBase> m_ParamInPortList = new List<ParamInPortBase>();

    private List<ParamOutPortStruct> m_ParamOutPortList = new List<ParamOutPortStruct>();

    public string m_SaveClassName = "";
    public string m_SaveNodeName = "";

    public string m_CheckSearchNodeName = "";

    public string m_ShowChinaName = "";
    public void OnEnable()
    {
        m_objects.Clear();
        m_OutPortList.Clear();
        m_ParamInPortList.Clear();
        m_ParamOutPortList.Clear();
        m_ShowChinaName = "";
    }  

    bool BeginSearchDataWithData(string _searchName)
    {
        //m_SaveClassName = "BulletNodeData";
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
        GraphViewRuntimeNodeBase nodeInstance = (GraphViewRuntimeNodeBase)assembly.CreateInstance(_searchName);
        
        string lastStr = "EditorNode";
        string fullName = _searchName + lastStr;
        var assembly1 = typeof(GraphViewNodeBase).Assembly;
        var editorNodeType = assembly1.GetType(fullName);
        if (editorNodeType != null && editorNodeType.GetCustomAttribute<CustomAttribute>() != null)
        {
            var customAttribute = editorNodeType.GetCustomAttribute<CustomAttribute>();
            m_ShowChinaName = customAttribute.DisplayName;
        }
        if (nodeInstance != null)
        {
            nodeInstance.m_Data = (NodeDataBase)assembly.CreateInstance(_searchName + "Data");
            m_SaveClassName = _searchName + "Data";
            m_SaveNodeName = _searchName;
            System.Reflection.FieldInfo[] fields = nodeInstance.m_Data.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (System.Reflection.FieldInfo item in fields)
            {
                string name = item.Name; //名称
                int beforeIndex = name.IndexOf("_");
                string realName = name.Substring(beforeIndex + 1);
                object value = item.GetValue(nodeInstance.m_Data);  //值
                MethodInfo fieldMethodInfo = nodeInstance.m_Data.GetType().GetMethod(realName);

                var tests = item.GetCustomAttribute<CustomAttribute>();
                if (tests == null)
                {
                    continue;
                }
                string displayName = tests.DisplayName;
                NodeBase newData = new NodeBase();
                newData.m_TypeName = displayName;
                newData.m_TypePropertyName = name;
                newData.m_Type = PortType.GetTransformStr(item.FieldType.Name);
                // newData.m_Value = default(item.FieldType);
                // if (item.FieldType == typeof(System.Int32))
                // {
                //     newData.m_Type = BASE_TYPE.Int;
                //     newData.m_Value = default(int);
                //
                // }
                // if (item.FieldType == typeof(System.Single))
                // {
                //     newData.m_Type = BASE_TYPE.Float;
                //     newData.m_Value = default(float);
                // }
                // if (item.FieldType == typeof(System.String))
                // {
                //     newData.m_Type = BASE_TYPE.String;
                //     newData.m_Value = default(string);
                // }
                // if (item.FieldType == typeof(System.Boolean))
                // {
                //     newData.m_Type = BASE_TYPE.Bool;
                //     newData.m_Value = default(bool);
                //
                // }
                if (fieldMethodInfo != null)
                {
                    newData.m_HadOutPort = true;
                }
                else
                {
                    newData.m_HadOutPort = false;
                }
                m_objects.Add(newData);
            }
            foreach (var key in nodeInstance.m_Childs.Keys)
            {
                m_OutPortList.Add(key);
            }
            foreach (var key in nodeInstance.m_ParamInPortDic.Keys)
            {
                var value = nodeInstance.m_ParamInPortDic[key];
                ParamInPortBase newData = new ParamInPortBase();
                newData.m_InPortName = key;
                newData.m_TypeName = value.m_PreNodeTypeName;
                m_ParamInPortList.Add(newData);
            }
            foreach (var key in nodeInstance.m_ParamOutPortDic)
            {
                ParamOutPortStruct newData = new ParamOutPortStruct(key.m_ParamOutPortName,key.m_ParamOutPortType);
                m_ParamOutPortList.Add(newData);
            }
            return true;
        }
        return false;


    }
    /// <summary>
    /// 生成具体的CS文件
    /// </summary>
    void GenCSFile()
    {
        string fileUrl = Application.dataPath + "/Scripts/GraphView/NodeDatas/";
        string filePath = fileUrl + "/" + m_SaveClassName + ".cs";
        string writeStr = "";
        string nextLine = "\r\n";
        //第一行是通用
        writeStr += "public class " + m_SaveClassName + " : NodeDataBase" + nextLine;
        writeStr += "{" + nextLine;
        //中间这里遍历所有的参数
        for (int i = 0;i < m_objects.Count;i++)
        {
            writeStr += "    [Custom(\"" + m_objects[i].m_TypeName + "\")]" + nextLine;
            string typeName = "";
            switch (m_objects[i].m_Type)
            {
                case "string":
                {
                    typeName = "string";
                    writeStr += "    public" + " " + typeName + " " + m_objects[i].m_TypePropertyName + " =" + " \"\"" + ";" + nextLine;
                }
                    break;
                default:
                {
                    writeStr += "    public" + " " + typeName + " " + m_objects[i].m_TypePropertyName + ";" + nextLine;
                    break;
                }
            };
        }
        writeStr += "}";


        if (writeStr != null)
        {
            System.IO.File.WriteAllText(filePath, writeStr);

            AssetDatabase.Refresh();
        }


    }
    
    
    public async void CreateNewNode(string _nodeName)
    {

        // Action<object> temp = (Action<object>)obj;
        // temp += OnImportCompleted;
        Action<object> action = o =>
        {
            m_SaveNodeName = _nodeName;
            m_CheckSearchNodeName = _nodeName;
            OnEnable();
        };
        EditorPath.AddFinishCompile(action);
        string writeStr = "";
        string nextLine = "\r\n";
        //先创建节点模板文件
        string nodeFileUrl = EditorPath.RUNTIME_NODE_PATH;
        string nodeFilePath = nodeFileUrl + "/" + _nodeName + ".cs";
        string partialNodeFilePath = nodeFileUrl + "/" + _nodeName + "_partial" + ".cs";
        writeStr += "using System.Collections;" + nextLine;
        writeStr += "using System.Collections.Generic;" + nextLine;
        writeStr += "public partial class " + _nodeName + " : GraphViewRuntimeNodeBase" + nextLine;
        writeStr += "{" + nextLine;
        //写入类主体信息
        writeStr += "    public " + _nodeName + "()" + " : base()" + nextLine;
        writeStr += "    {" + nextLine;
        writeStr += "        m_Name = GetType().Name;" + nextLine;
        for (int i = 0; i < m_OutPortList.Count; i++)
        {
            writeStr += "        m_Childs.Add(" + "\"" + m_OutPortList[i] + "\"" + ", new System.Collections.Generic.List<int>());" + nextLine;
        }

        for (int i = 0; i < m_ParamInPortList.Count; i++)
        {
            writeStr += "        m_ParamInPortDic.Add(" + "\"" + m_ParamInPortList[i].m_InPortName + "\"" + ", new ParamPreNodeInfo(" + "0"  + "," + "\"" + "\""+ "," + "\"" + m_ParamInPortList[i].m_TypeName + "\""  + "))" + ";" + nextLine; 
        }
        for (int i = 0; i < m_ParamOutPortList.Count; i++)
        {
            writeStr += "        m_ParamOutPortDic.Add(new ParamOutPortStruct(" + "\"" + m_ParamOutPortList[i].m_ParamOutPortName + "\"" + "," + "\"" + m_ParamOutPortList[i].m_ParamOutPortType + "\""  + "))" + ";" + nextLine; 
        }

        writeStr += "    }" + nextLine;
        
        
        writeStr += "    public "+ _nodeName + "Data " + "m_RuntimeData" + ";" + nextLine;
        writeStr += "    public " + "override "+ "void " + "InitRuntimeData" + "()" + nextLine;
        writeStr += "    {" + nextLine;
        writeStr += "        m_RuntimeData = " + "(" + _nodeName + "Data" + ")" + "m_Data" +";" + nextLine;
        writeStr += "    }" + nextLine;
        
        //遍历所有的参数输入口数组
        for (int i = 0; i < m_ParamInPortList.Count; i++)
        {
            var tempData = m_ParamInPortList[i];
            writeStr += "    public " + tempData.m_TypeName + " " + "Get" + tempData.m_InPortName + "ParamInportData" + "()" + nextLine;
            writeStr += "    {" + nextLine;
            writeStr += "        var paramName = " + "\"" + tempData.m_InPortName + "\"" +";" + nextLine;
            writeStr += "        if (m_ParamInPortDic.ContainsKey(paramName) == false)" + nextLine;
            writeStr += "            return default(" + tempData.m_TypeName + ")" +";" + nextLine;
            writeStr += "        var preNodeUid = m_ParamInPortDic[paramName].m_PreNodeUid" +";" + nextLine;
            writeStr += "        var preNodeFuncName = m_ParamInPortDic[paramName].m_PreNodeFuncName" +";" + nextLine;
            writeStr += "        var preNodeParamType = m_ParamInPortDic[paramName].m_PreNodeParamType" +";" + nextLine;
            writeStr += "        var preNode = m_GraphMgr.m_GraphData.GetNodeByUid(preNodeUid)" +";" + nextLine;
            writeStr += "        if (preNode != null)" + nextLine;
            writeStr += "        {" + nextLine;
            writeStr += "            System.Reflection.MethodInfo methodInfo = null" +";" + nextLine;
            writeStr += "            System.Object target = null" +";" + nextLine;
            writeStr += "            if (preNodeParamType == 0)" + nextLine;
            writeStr += "            {" + nextLine;
            writeStr += "                methodInfo = preNode.GetType().GetMethod(preNodeFuncName)" +";" + nextLine;
            writeStr += "                target = preNode" +";" + nextLine;
            writeStr += "            }" + nextLine;
            writeStr += "            if (preNodeParamType == 1)" + nextLine;
            writeStr += "            {" + nextLine;
            writeStr += "                methodInfo = preNode.m_Data.GetType().GetMethod(preNodeFuncName)" +";" + nextLine;
            writeStr += "                target = preNode.m_Data" +";" + nextLine;
            writeStr += "            }" + nextLine;
            writeStr += "            if (methodInfo != null)" + nextLine;
            writeStr += "            {" + nextLine;
            writeStr += "                return (" + tempData.m_TypeName + ")" + "methodInfo.Invoke(target, null)" +";" + nextLine;
            writeStr += "            }" + nextLine;
            writeStr += "        }" + nextLine;
            writeStr += "        return default(" + tempData.m_TypeName + ")" +";" + nextLine;
            writeStr += "    }" + nextLine;
        }
        
        //写入类主体信息
        writeStr += "}";

        if (writeStr != null)
        {
            await System.IO.File.WriteAllTextAsync(partialNodeFilePath, writeStr);
            AssetDatabase.Refresh();
        }
        if (File.Exists(nodeFilePath) == false)
        {
            writeStr = "";
            //说明当前是新的节点模板
            writeStr += "public partial class " + _nodeName + " : GraphViewRuntimeNodeBase" + nextLine;
            writeStr += "{" + nextLine;
            //写入类主体信息
            writeStr += "    public override void OnTriggerIn()" + nextLine;
            writeStr += "    {" + nextLine;
            writeStr += "        base.OnTriggerIn();" + nextLine;
            writeStr += nextLine;
            writeStr += "    }" + nextLine;
            
            writeStr += "    public override void OnUpdate(Fix64 _deltaTime)" + nextLine;
            writeStr += "    {" + nextLine;
            writeStr += "        base.OnUpdate(_deltaTime);" + nextLine;
            writeStr += "    }" + nextLine;
            //写入类主体信息
            writeStr += "}";
            
            if (writeStr != null)
            {
                await System.IO.File.WriteAllTextAsync(nodeFilePath, writeStr);
                AssetDatabase.Refresh();
            }
        }
        
        
        //这里写入对应的数据Data模板代码
        string fileUrl = EditorPath.RUNTIME_NODE_DATA_PATH;
        string filePath = fileUrl + "/" + _nodeName + "Data" + ".cs";
        writeStr = "";
        nextLine = "\r\n";
        //第一行是通用
        writeStr += "public class " + _nodeName + "Data" + " : NodeDataBase" + nextLine;
        writeStr += "{" + nextLine;
        //中间这里遍历所有的参数
        for (int i = 0; i < m_objects.Count; i++)
        {
            writeStr += "    [Custom(\"" + m_objects[i].m_TypeName + "\")]" + nextLine;
            string typeName = m_objects[i].m_Type;
            switch (m_objects[i].m_Type)
            {
                case "string":
                    {
                        typeName = "string";
                        writeStr += "    public" + " " + typeName + " " + m_objects[i].m_TypePropertyName + " =" + " \"\"" + ";" + nextLine;
                    }
                    break;
                default:
                {
                    writeStr += "    public" + " " + typeName + " " + m_objects[i].m_TypePropertyName + ";" + nextLine;
                    break;
                }
            }
        }
        for (int i = 0; i < m_objects.Count; i++)
        {
            string typeName = m_objects[i].m_Type;
            // switch (m_objects[i].m_Type)
            // {
            //     case BASE_TYPE.Int:
            //     {
            //         typeName = "int";
            //     }
            //         break;
            //     case BASE_TYPE.Float:
            //     {
            //         typeName = "float";
            //     }
            //         break;
            //     case BASE_TYPE.Bool:
            //     {
            //         typeName = "bool";
            //     }
            //         break;
            //     case BASE_TYPE.String:
            //     {
            //         typeName = "string";
            //     }
            //         break;
            // }
            if (m_objects[i].m_HadOutPort)
            {
                int beforeIndex = m_objects[i].m_TypePropertyName.IndexOf("_");
                string funcName = m_objects[i].m_TypePropertyName.Substring(beforeIndex + 1);
                writeStr += "    public" + " " + typeName + " " + funcName + "() { return " + m_objects[i].m_TypePropertyName +"; }" + nextLine;
            }
        }

        writeStr += "}";


        if (writeStr != null)
        {
            await System.IO.File.WriteAllTextAsync(filePath, writeStr);

            AssetDatabase.Refresh();
        }

        //这里写入编辑用的节点数据
        string nodeClassName = _nodeName + "EditorNode";
        fileUrl = EditorPath.EDITOR_NODE_PATH;
        filePath = fileUrl + "/" + nodeClassName + ".cs";
        writeStr = "";
        nextLine = "\r\n";
        //第一行是通用
        writeStr += "using System.Collections;" + nextLine;
        writeStr += "using System.Collections.Generic;" + nextLine;
        writeStr += "using System.Reflection;" + nextLine;
        writeStr += "using UnityEditor.Experimental.GraphView;" + nextLine;
        writeStr += "using UnityEngine;" + nextLine;
        writeStr += "using UnityEngine.UIElements;" + nextLine;
        writeStr += nextLine;
        if (string.IsNullOrEmpty(m_ShowChinaName) == false && string.IsNullOrWhiteSpace(m_ShowChinaName) == false)
        {
            writeStr += "[Custom(" + "\"" + m_ShowChinaName + "\"" + ")]" + nextLine;
        }
        writeStr += "public class " + nodeClassName + " : GraphViewNodeBase" + nextLine;
        writeStr += "{" + nextLine;
        writeStr += "    public " + nodeClassName + "()" + nextLine;
        writeStr += "    {" + nextLine;
        writeStr += "        m_Data = new " + _nodeName + "();" + nextLine;
        writeStr += "        m_Data.m_Data = new " + _nodeName + "Data" + "();" + nextLine;
        writeStr += "    }" + nextLine;
        writeStr += "}";
        if (writeStr != null)
        {
            await System.IO.File.WriteAllTextAsync(filePath, writeStr);

            AssetDatabase.Refresh();
        }

    }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("节点名称：" + m_SaveNodeName,GUILayout.Width(500));
        m_ShowChinaName = EditorGUILayout.TextField(m_ShowChinaName,GUILayout.Width(600));;
        GUILayout.BeginScrollView(Vector2.zero);
        
        if (!string.IsNullOrEmpty(m_CheckSearchNodeName))// && m_objects.Count <= 0 && m_OutPortList.Count <= 0
        {
            BeginSearchDataWithData(m_CheckSearchNodeName);
            m_CheckSearchNodeName = "";
        }
        
        if (!string.IsNullOrEmpty(m_SaveClassName) && !string.IsNullOrWhiteSpace(m_SaveClassName))
        {
            int removeIndex = -1;
            GUILayout.BeginVertical();
            for (int i = 0; i < m_objects.Count; i++)
            {
                var keyItem = m_objects[i];
                GUILayout.BeginHorizontal();
                RefreshMainLayout(keyItem);
                string beforeType = keyItem.m_Type;
                EditorGUILayout.LabelField("类型：",GUILayout.Width(50));
                keyItem.m_Type = EditorGUILayout.TextField(keyItem.m_Type,GUILayout.Width(100));;
                //TypeChange(beforeType, keyItem);
                GUILayout.Space(5);
                if (GUILayout.Button("删除",GUILayout.Width(50)))
                {
                    //删除当前数据
                    removeIndex = i;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            if (removeIndex >= 0)
            {
                m_objects.RemoveAt(removeIndex);
            }
            if (GUILayout.Button("+属性"))
            {
                //添加一个新的数据
                NodeBase newData = new NodeBase();
                newData.m_Type = "int";
                newData.m_Value = default(int);
                newData.m_TypeName = default(string);
                m_objects.Add(newData);
            }
            GUILayout.EndHorizontal();
            using (var temp2 = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int portRemoveIndex = -1;
                for (int i = 0; i < m_OutPortList.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("出口名称：", GUILayout.ExpandWidth(true));
                    m_OutPortList[i] = EditorGUILayout.TextField(m_OutPortList[i], GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("删除"))
                    {
                        //删除当前数据
                        portRemoveIndex = i;
                    }

                    GUILayout.EndHorizontal();
                }

                if (portRemoveIndex >= 0)
                {
                    m_OutPortList.RemoveAt(portRemoveIndex);
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+出口"))
                {
                    //添加一个新的数据
                    m_OutPortList.Add("");

                }

                GUILayout.EndHorizontal();
            }
            using (var temp1 = new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var widthTemp = GUILayout.Width(100);
                //这个是参数输入口定义区域
                using (var temp2 = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    int portRemoveIndex = -1;
                    for (int i = 0; i < m_ParamInPortList.Count; i++)
                    {
                        
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("数据输入口名称：",widthTemp);
                        m_ParamInPortList[i].m_InPortName = EditorGUILayout.TextField(m_ParamInPortList[i].m_InPortName,widthTemp);
                        EditorGUILayout.LabelField("数据输入口类型：", widthTemp);
                        m_ParamInPortList[i].m_TypeName = EditorGUILayout.TextField(m_ParamInPortList[i].m_TypeName,widthTemp);
                        if (GUILayout.Button("删除",widthTemp))
                        {
                            //删除当前数据
                            portRemoveIndex = i;
                        }
                        GUILayout.EndHorizontal();
                    }

                    if (portRemoveIndex >= 0)
                    {
                        m_ParamInPortList.RemoveAt(portRemoveIndex);
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+数据输入口"))
                    {
                        //添加一个新的数据
                        m_ParamInPortList.Add(new ParamInPortBase());

                    }

                    GUILayout.EndHorizontal();
                }
                //这个是参数输入口定义区域
                using (var temp2 = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    int portRemoveIndex = -1;
                    for (int i = 0; i < m_ParamOutPortList.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("数据输出口名称：",widthTemp);
                        m_ParamOutPortList[i].m_ParamOutPortName = EditorGUILayout.TextField(m_ParamOutPortList[i].m_ParamOutPortName,widthTemp);
                        EditorGUILayout.LabelField("数据输出口类型：", widthTemp);
                        m_ParamOutPortList[i].m_ParamOutPortType = EditorGUILayout.TextField(m_ParamOutPortList[i].m_ParamOutPortType,widthTemp);
                        if (GUILayout.Button("删除",widthTemp))
                        {
                            //删除当前数据
                            portRemoveIndex = i;
                        }
                        GUILayout.EndHorizontal();
                    }

                    if (portRemoveIndex >= 0)
                    {
                        m_ParamOutPortList.RemoveAt(portRemoveIndex);
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+数据输出口"))
                    {
                        //添加一个新的数据
                        m_ParamOutPortList.Add(new ParamOutPortStruct("",""));
                    }

                    GUILayout.EndHorizontal();
                }
            }



            GUILayout.BeginVertical();
            if (GUILayout.Button("生成文件"))
            {
                CreateNewNode(m_SaveNodeName);
            }
            GUILayout.EndVertical();
        }

        GUILayout.EndScrollView();
    }
    void RefreshMainLayout(NodeBase nodeBase)
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("显示名称：",GUILayout.Width(100));
        nodeBase.m_TypeName = EditorGUILayout.TextField(nodeBase.m_TypeName,GUILayout.Width(100));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("属性名称：",GUILayout.Width(100));
        GUILayout.Space(5);
        nodeBase.m_TypePropertyName = EditorGUILayout.TextField(nodeBase.m_TypePropertyName,GUILayout.Width(100));
        GUILayout.Space(5);
        EditorGUILayout.LabelField("值：",GUILayout.Width(50));
        GUILayout.Space(5);
        // switch (nodeBase.m_Type)
        // {
        //     case BASE_TYPE.Int:
        //         {
        //             nodeBase.m_Value = EditorGUILayout.IntField((int)nodeBase.m_Value,GUILayout.Width(100));
        //         }
        //         break;
        //     case BASE_TYPE.Float:
        //         {
        //             nodeBase.m_Value = EditorGUILayout.FloatField((float)nodeBase.m_Value,GUILayout.Width(100));
        //         }
        //         break;
        //     case BASE_TYPE.Bool:
        //         {
        //             nodeBase.m_Value = EditorGUILayout.Toggle((bool)nodeBase.m_Value,GUILayout.Width(100));
        //         }
        //         break;
        //     case BASE_TYPE.String:
        //         {
        //             nodeBase.m_Value = EditorGUILayout.TextField((string)nodeBase.m_Value,GUILayout.Width(100));
        //         }
        //         break;
        // }
        EditorGUILayout.LabelField("是否输出：",GUILayout.Width(100));
        nodeBase.m_HadOutPort = EditorGUILayout.Toggle((bool)nodeBase.m_HadOutPort,GUILayout.Width(50));
    }
}
