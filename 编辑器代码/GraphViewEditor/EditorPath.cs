using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class EditorPath
{
    public static string EDITOR_JSON_PATH = Application.dataPath + "/Editor/GraphViewEditor/EditorJson";
    public static string EDITOR_DESC_JSON_PATH = Application.dataPath + "/Editor/GraphViewEditor/EditorDescJson";

    public static string EDITOR_NODE_PATH = Application.dataPath + "/Editor/GraphViewEditor/GraphNodes/Exclass/";

    public static string RUNTIME_JSON_PATH = Application.dataPath + "/GraphRuntimeJson";
    
    public static string RUNTIME_NODE_PATH = Application.dataPath + "/Scripts/GraphView/RunningTimeNode/";
    
    public static string RUNTIME_NODE_DATA_PATH = Application.dataPath + "/Scripts/GraphView/NodeDatas/";


    public static void AddFinishCompile(Action<object> _callback)
    {
        Assembly asm = Assembly.GetAssembly(typeof(Editor));
        Type type = asm.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
        MethodInfo getAnnotations = type.GetMethod("get_Instance", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Public);
        var annotations = getAnnotations.Invoke(null, null);
        Type annotationType = annotations.GetType();
        EventInfo compilationFinished = annotationType.GetEvent("compilationFinished", BindingFlags.Public | BindingFlags.Instance);
        Delegate d = Delegate.CreateDelegate(
            compilationFinished.EventHandlerType, _callback.Target, _callback.Method);
        compilationFinished.AddEventHandler(annotations, d);
    }

}
