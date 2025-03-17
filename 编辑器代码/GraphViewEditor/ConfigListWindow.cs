using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ConfigListWindow : EditorWindow
{
    [MenuItem("蓝图/界面列表界面")]
    public static void OpenConfigListWindow()
    {
        // 定义了创建并打开Window的方法
        var window = GetWindow<ConfigListWindow>();
        window.titleContent = new GUIContent("可视化列表界面");
    }
    List<string> m_FileName = new List<string>();
    Vector2 scroll = Vector2.zero;

    private string m_SearchName = "";
    private void OnEnable()
    {
        RefreshFile();

    }
    void RefreshFile()  
    {
        m_FileName.Clear();
        string directoryPath = EditorPath.EDITOR_JSON_PATH;
        if (Directory.Exists(directoryPath))
        {
            DirectoryInfo folder = new DirectoryInfo(directoryPath);
            foreach (FileInfo file in folder.GetFiles("*"))
            {
                if (file.Name.EndsWith(".meta"))
                    continue;
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
        EditorGUILayout.LabelField("新建一个空白流程图:");
        if (GUILayout.Button("打开"))
        {
            OpenGraphView("");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical();
        GUILayout.Space(30);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        m_SearchName = EditorGUILayout.TextField(m_SearchName, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginVertical();
        GUILayout.Space(30);
        EditorGUILayout.EndVertical();

        scroll = GUILayout.BeginScrollView(scroll, "Box");
        for (int i = 0; i < m_FileName.Count; i++)
        {
            if (string.IsNullOrEmpty(m_SearchName) == false)
            {
                if (Regex.IsMatch(m_FileName[i].ToLower(),m_SearchName.ToLower()) == false)
                {
                    continue;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_FileName[i],GUILayout.Width(200));
            if (GUILayout.Button("打开"))
            {
                OpenGraphView(m_FileName[i]);
            }
            if (GUILayout.Button("删除"))
            {
                if (UnityEditor.EditorUtility.DisplayDialog("标题", "确定删除蓝图？？？？？？？删除后不可恢复", "确定","取消"))
                {
                    DeleteGraphViewConfigData(m_FileName[i]);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
    }
    void OpenGraphView(string configName)
    {
        var window = CreateWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.m_ConfigName = configName;  
        window.RefreshView();
    }

    void DeleteGraphViewConfigData(string _configName)
    {
        string directoryPath = EditorPath.EDITOR_JSON_PATH;
        string runtimeDirectoryPath = EditorPath.RUNTIME_JSON_PATH;

        string editorFilePath = directoryPath + "/" + _configName + ".json";
        string runtimeConfigPath = runtimeDirectoryPath + "/" + _configName + ".json";
        if (File.Exists(editorFilePath))
        {
            File.Delete(editorFilePath);
        }
        if (File.Exists(runtimeConfigPath))
        {
            File.Delete(runtimeConfigPath);
        }
        AssetDatabase.Refresh();
        
        RefreshFile();
    }
}
