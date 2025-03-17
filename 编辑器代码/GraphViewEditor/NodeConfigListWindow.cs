using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using File = UnityEngine.Windows.File;

public class NodeConfigListWindow : EditorWindow
{
    [MenuItem("蓝图/节点列表界面")]
    public static void OpenConfigListWindow()
    {
        // 定义了创建并打开Window的方法
        var window = GetWindow<NodeConfigListWindow>();
        window.titleContent = new GUIContent("节点列表界面");
    }

    Vector2 scroll = Vector2.zero;
    List<string> m_FileName = new List<string>();
    private string m_NewNodeName;

    private string m_SearchNodeName;
    private string m_SearchEditorNodeName;
    private void OnEnable()
    {
        RefreshFile();
    }
    void RefreshFile()  
    {
        m_FileName.Clear();
        string directoryPath = EditorPath.RUNTIME_NODE_PATH;
        if (Directory.Exists(directoryPath))
        {
            DirectoryInfo folder = new DirectoryInfo(directoryPath);
            foreach (FileInfo file in folder.GetFiles("*"))
            {
                if (file.Name.EndsWith(".meta"))
                    continue;
                if (file.Name.Contains("_partial"))
                {
                    continue;
                }
                // 处理每个文件
                var filename = "";
                //去掉扩展名
                if (file.Name.LastIndexOf(".") != -1)
                {
                    filename = file.Name.Substring(0, file.Name.LastIndexOf("."));
                }
                else
                {
                    //申请文件名称
                    filename = file.Name;
                }
                m_FileName.Add(filename);
            }

        }
        AssetDatabase.Refresh();
    }
    private void OnDisable()
    {
        m_NewNodeName = "";
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新路径："))
        {
            RefreshFile();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新建一个节点配置"))
        {
            if (m_FileName.Contains(m_NewNodeName))
            {
                UnityEditor.EditorUtility.DisplayDialog("标题", "已经有一个同名的节点了", "关闭");
            }
            else
            {
                if(string.IsNullOrEmpty(m_NewNodeName) || string.IsNullOrWhiteSpace(m_NewNodeName))
                {
                    UnityEditor.EditorUtility.DisplayDialog("标题", "名称不允许为空", "关闭");
                }
                else
                {
                    OpenGraphView(m_NewNodeName,true);
                    Action<object> action = o =>
                    {
                        RefreshFile();
                    };
                    EditorPath.AddFinishCompile(action);
                }
            }
        }
        m_NewNodeName = EditorGUILayout.TextField(m_NewNodeName, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginVertical();
        GUILayout.Space(30);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        m_SearchNodeName = EditorGUILayout.TextField(m_SearchNodeName, GUILayout.ExpandWidth(true));
        m_SearchEditorNodeName = EditorGUILayout.TextField(m_SearchEditorNodeName, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginVertical();
        GUILayout.Space(30);
        EditorGUILayout.EndVertical();
        scroll = GUILayout.BeginScrollView(scroll, "Box");
        for (int i = 0; i < m_FileName.Count; i++)
        {
            string lastStr = "EditorNode";
            string fullName = m_FileName[i] + lastStr;
            var assembly1 = typeof(GraphViewNodeBase).Assembly;
            var editorNodeType = assembly1.GetType(fullName);
            var showName = m_FileName[i];
            var searchName = showName;
            if (editorNodeType != null && editorNodeType.GetCustomAttribute<CustomAttribute>() != null)
            {
                var customAttribute = editorNodeType.GetCustomAttribute<CustomAttribute>();
                showName = customAttribute.DisplayName;
                searchName = showName;
            }
            else
            {
                searchName = searchName.ToLower();
            }

            var tempStr = m_SearchNodeName;
            if (string.IsNullOrEmpty(m_SearchEditorNodeName) == false && string.IsNullOrWhiteSpace(m_SearchEditorNodeName) == false) 
            {
                tempStr = m_SearchEditorNodeName;
                searchName = m_FileName[i].ToLower();
            }
            if (string.IsNullOrEmpty(tempStr) == false)
            {
                if (Regex.IsMatch(searchName,tempStr.ToLower()) == false)
                {
                    continue;
                }
            }
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(showName,GUILayout.Width(200));
            if (GUILayout.Button("打开",GUILayout.Width(200)))
            {
                OpenGraphView(m_FileName[i]);
            }

            if (m_FileName[i] != "SkillEnd")
            {
                if (GUILayout.Button("删除节点",GUILayout.Width(200)))
                {
                    if (UnityEditor.EditorUtility.DisplayDialog("标题", "确定删除节点？？？？？？？删除后不可恢复", "确定","取消"))
                    {
                        DeleteNode(m_FileName[i]);
                    }
                }
            }
            EditorGUILayout.LabelField(m_FileName[i],GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            
        }
        GUILayout.EndScrollView();
    }
    
    void OpenGraphView(string configName,bool _isCreate = false)
    {
        var window = GetWindow<NodeEditorWindow>();
        window.titleContent = new GUIContent("节点编辑界面: " + configName);
        window.m_SaveNodeName = configName;
        window.m_CheckSearchNodeName = configName;
        if (_isCreate == true)
        {
            window.CreateNewNode(configName);
        }
        else
        {
            window.OnEnable();
        }
    }

    void DeleteNode(string _nodeName)
    {
        string directoryPath = EditorPath.RUNTIME_NODE_PATH;
        if (Directory.Exists(directoryPath))
        {
            string filePath1 = directoryPath + _nodeName + ".cs";
            string filePath2 = directoryPath + _nodeName + "_partial.cs";
            if (File.Exists(filePath1))
            {
                File.Delete(filePath1);
            }
            if (File.Exists(filePath2))
            {
                File.Delete(filePath2);
            }

            string editorNodeParentPath = EditorPath.EDITOR_NODE_PATH;
            string editorNodePath = editorNodeParentPath + _nodeName + "EditorNode.cs";
            if (File.Exists(editorNodePath))
            {
                File.Delete(editorNodePath);
            }

            string fileUrl = EditorPath.RUNTIME_NODE_DATA_PATH;
            string filePath = fileUrl + "/" + _nodeName + "Data" + ".cs";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            AssetDatabase.Refresh();
            RefreshFile();
        }
    }
}
