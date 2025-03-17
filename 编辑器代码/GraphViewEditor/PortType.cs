using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Experimental.GraphView;

public class InAndOutPortType
{

}

public class PortType
{
    public static string FieldOutPortFlag = "HadFieldPort";
    public static string FieldOutPortCheck = "FieldPortFuncName:";
    public static string ParamInPortFlag = "ParamInPortFlag:";
    public static string ParamOutPortFlag = "ParamOutPortFlag:";
    public static Type GetTypeByString(string type)
    {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
        switch (type.ToLower())
        {
            case "bool":
                return Type.GetType("System.Boolean", true, true);
            case "byte":
                return Type.GetType("System.Byte", true, true);
            case "sbyte":
                return Type.GetType("System.SByte", true, true);
            case "char":
                return Type.GetType("System.Char", true, true);
            case "decimal":
                return Type.GetType("System.Decimal", true, true);
            case "double":
                return Type.GetType("System.Double", true, true);
            case "float":
                return Type.GetType("System.Single", true, true);
            case "int":
                return Type.GetType("System.Int32", true, true);
            case "uint":
                return Type.GetType("System.UInt32", true, true);
            case "long":
                return Type.GetType("System.Int64", true, true);
            case "ulong":
                return Type.GetType("System.UInt64", true, true);
            case "object":
                return Type.GetType("System.Object", true, true);
            case "short":
                return Type.GetType("System.Int16", true, true);
            case "ushort":
                return Type.GetType("System.UInt16", true, true);
            case "string":
                return Type.GetType("System.String", true, true);
            case "date":
            case "datetime":
                return Type.GetType("System.DateTime", true, true);
            case "guid":
                return Type.GetType("System.Guid", true, true);
            default:
                if (type == "List<int>")
                {
                    return typeof(List<int>);
                }
                System.Reflection.Assembly assembly2 = System.Reflection.Assembly.Load("Assembly-CSharp");
                return assembly2.GetType(type, true, true);
        }
    }

    public static string GetTransformStr(string _typeStr)
    {
        switch (_typeStr)
        {
            case "Single":
                return "float";
            case "String":
                return "string";
            case "Int32":
                return "int";
            case "Boolean":
                return "bool";
        }
        return _typeStr;
    }
}


public class MyJsonConverter_Editor : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(GraphEditorNode).IsAssignableFrom(objectType);
    }
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
            JObject jsonObject = JObject.Load(reader);
            GraphEditorNode target = new GraphEditorNode();
            JToken gameName;
            if (jsonObject.TryGetValue("m_Root", out gameName))
            {
                target.m_Root = new SerilizeEditorNode();
                var runtimeNodeClassItem = gameName["m_Data"]["m_Name"];
                string runtimeNodeClassName = runtimeNodeClassItem.Value<string>();
                target.m_Root.m_Data = (GraphViewRuntimeNodeBase)assembly.CreateInstance(runtimeNodeClassName);
            
                var dataClassItem = gameName["m_Data"]["m_Data"]["m_ClassName"];
                string dataClassName = dataClassItem.Value<string>();
                target.m_Root.m_Data.m_Data = (NodeDataBase)assembly.CreateInstance(dataClassName);

                var childsItem = gameName["m_Data"]["m_Childs"];
                
                var loopItem = childsItem.First;
                while (loopItem != null)
                {
                    JProperty jProperty = loopItem.ToObject<JProperty>();
                    string id = jProperty.Name;
                    if (target.m_Root.m_Data.m_Childs.ContainsKey(id) == false)
                    {
                        target.m_Root.m_Data.m_Childs.Add(id,new List<int>());
                    }
                    foreach (var value3 in loopItem.Values())
                    {
                        target.m_Root.m_Data.m_Childs[id].Add(int.Parse(value3.ToString())); 
                    }
                    loopItem = loopItem.Next;
                }
                
                var guidName = gameName["m_Data"]["m_Guid"];
                string guidValue = guidName.Value<string>();
                target.m_Root.m_Data.m_Guid = int.Parse(guidValue);
                
                //这里开始设置坐标以及长宽等数据
                var xPosItem = gameName["m_XPos"];
                float xPos = xPosItem.Value<float>();
                var yPosItem = gameName["m_YPos"];
                float yPos = yPosItem.Value<float>();
                var widthItem = gameName["m_Width"];
                float width = widthItem.Value<float>();
                var heightItem = gameName["m_Height"];
                float height = heightItem.Value<float>();

                target.m_Root.m_XPos = xPos;
                target.m_Root.m_YPos = yPos;
                target.m_Root.m_Width = width;
                target.m_Root.m_Height = height;


            }
            
            JToken allDic;
            if (jsonObject.TryGetValue("m_AllDic",out allDic))
            {
                if (allDic.Count() > 0)
                {
                    var loopItem = allDic.First;
                    while (loopItem != null)
                    {
                        JProperty jProperty = loopItem.ToObject<JProperty>();
                        string id = jProperty.Name;

                        SerilizeEditorNode editorNode = new SerilizeEditorNode();
                        GraphViewRuntimeNodeBase node = null;
                        NodeDataBase nodeData = null;
                        string guid = "";
                        foreach (var vale1 in loopItem.Values())
                        {
                            JProperty jProperty1 = vale1.ToObject<JProperty>();
                            string name1 = jProperty1.Name;
                            //每个值对应的Key判断
                            if (string.Equals(name1, "m_EditorClassName"))
                            {
                                foreach (var itemTemp in vale1.Values())
                                {
                                    string nameTemp = itemTemp.Value<string>();
                                    editorNode.m_EditorClassName = nameTemp;
                                }

                                // editorNode = (SerilizeEditorNode)assembly.CreateInstance(nameTemp);
                                // if (target.m_AllDic.ContainsKey(id) == false)
                                // {
                                //     target.m_AllDic.Add(id,editorNode);
                                // }
                            }
                            if (string.Equals(name1, "m_Data"))
                            {
                                foreach (var value2 in vale1.Values())
                                {
                                    JProperty jProperty2 = value2.ToObject<JProperty>();
                                    string name2 = jProperty2.Name;
                                    if (string.Equals(name2,"m_Name"))
                                    {
                                        //当前是要创建的实例对象
                                        foreach (var itemTemp in value2.Values())
                                        {
                                            string nameTemp = itemTemp.Value<string>();
                                            node = (GraphViewRuntimeNodeBase)assembly.CreateInstance(nameTemp);
                                        }
                                    }
                                    if (string.Equals(name2,"m_Guid"))
                                    {
                                        //当前是要创建的实例对象
                                        foreach (var itemTemp in value2.Values())
                                        {
                                            guid = itemTemp.Value<string>();
                                        }
                                    }
                                    if (string.Equals(name2,"m_Childs"))
                                    {
                                        //创建运行实例
                                        foreach (var value3 in value2.Values())
                                        {
                                            string beforeName = value3.ToObject<JProperty>().Name;
                                            // if (node.m_Childs.ContainsKey(beforeName) == false)
                                            // {
                                            //     node.m_Childs.Add(beforeName,new List<string>());
                                            // }
                                            if (node != null && node.m_Childs.ContainsKey(beforeName))
                                            {
                                                foreach (var portChilds in value3.Values())
                                                {
                                                    node.m_Childs[beforeName].Add(int.Parse(portChilds.ToString())); 
                                                }
                                            }
                                        }
                                    }
                                    if (string.Equals(name2,"m_ParamInPortDic"))
                                    {
                                        //创建运行实例
                                        foreach (var value3 in value2.Values())
                                        {
                                            string beforeName = value3.ToObject<JProperty>().Name;
                                            ParamPreNodeInfo temp = new ParamPreNodeInfo(0,"","");
                                            foreach (var portChilds in value3.Values())
                                            {
                                                var checkName = portChilds.ToObject<JProperty>().Name;
                                                if (string.Equals(checkName,"m_PreNodeUid"))
                                                {
                                                    foreach (var jValue in portChilds.Values())
                                                    {
                                                        temp.m_PreNodeUid = int.Parse(jValue.ToString());
                                                    }
                                                }
                                                if (string.Equals(checkName,"m_PreNodeFuncName"))
                                                {
                                                    foreach (var jValue in portChilds.Values())
                                                    {
                                                        temp.m_PreNodeFuncName = jValue.ToString();
                                                    }
                                                }
                                                if (string.Equals(checkName,"m_PreNodeTypeName"))
                                                {
                                                    foreach (var jValue in portChilds.Values())
                                                    {
                                                        temp.m_PreNodeTypeName = jValue.ToString();
                                                    };
                                                }
                                                if (string.Equals(checkName,"m_PreNodeParamType"))
                                                {
                                                    foreach (var jValue in portChilds.Values())
                                                    {
                                                        temp.m_PreNodeParamType = int.Parse(jValue.ToString());
                                                    };
                                                }
                                            }
                                            if (node != null && node.m_ParamInPortDic.ContainsKey(beforeName))
                                            {
                                                node.m_ParamInPortDic[beforeName] = temp;
                                            }
                                        }
                                    }
                                    if (string.Equals(name2,"m_Data"))
                                    {
                                        string nodeDataClassName = value2.ToObject<JProperty>().Name;
                                        foreach (var value3 in value2.Values())
                                        {
                                            JProperty jProperty3 = value3.ToObject<JProperty>();
                                            string name3 = jProperty3.Name;
                                            if (string.Equals(name3,"m_ClassName"))
                                            {
                                                //创建数据实例
                                                foreach (var itemTemp in value3.Values())
                                                {
                                                    nodeData = (NodeDataBase)assembly.CreateInstance(itemTemp.Value<string>()); 
                                                }
                                                break;
                                            }
                                        }
                                        if (nodeData != null)
                                        {
                                            foreach (var value3 in value2.Values())
                                            {
                                                var propertyList = nodeData.GetType().GetFields();
                                                string fieldDataClassName = value3.ToObject<JProperty>().Name;
                                                foreach (System.Reflection.FieldInfo info in propertyList)
                                                {
                                                    if (info.Name == fieldDataClassName) 
                                                    {
                                                        //通过属性名称获取属性的值
                                                        foreach (var VARIABLE in value3.Values())
                                                        {
                                                            var result = Convert.ChangeType(VARIABLE.Value<string>(),GetTypeByStr(VARIABLE.Type));
                                                            info.SetValue(nodeData,result);
                                                        }

                                                    }            
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (string.Equals(name1, "m_XPos"))
                            {
                                foreach (var itemTemp in vale1.Values())
                                {
                                    float nameTemp = itemTemp.Value<float>();
                                    editorNode.m_XPos = nameTemp;
                                }
                            }
                            if (string.Equals(name1, "m_YPos"))
                            {
                                foreach (var itemTemp in vale1.Values())
                                {
                                    float nameTemp = itemTemp.Value<float>();
                                    editorNode.m_YPos = nameTemp;
                                }
                            }
                            if (string.Equals(name1, "m_Width"))
                            {
                                foreach (var itemTemp in vale1.Values())
                                {
                                    float nameTemp = itemTemp.Value<float>();
                                    editorNode.m_Width = nameTemp;
                                }
                            }
                            if (string.Equals(name1, "m_Height"))
                            {
                                foreach (var itemTemp in vale1.Values())
                                {
                                    float nameTemp = itemTemp.Value<float>();
                                    editorNode.m_Height = nameTemp;
                                }
                            }
                        }


                        if (node != null && nodeData != null)
                        {
                            node.m_Data = nodeData;
                        }
                        if (editorNode != null && node != null)
                        {
                            editorNode.m_Data = node;
                            target.m_AllDic.Add(int.Parse(id),editorNode);
                        }

                        if (node != null)
                        {
                            node.m_Guid = int.Parse(guid);
                        }
                        
                        loopItem = loopItem.Next;
                    }
                }
                
            
            }
            //serializer.Populate(jsonObject.CreateReader(), target); 

            JToken maxGUID;
            if (jsonObject.TryGetValue("m_MaxGUID",out maxGUID))
            {
                target.m_MaxGUID = maxGUID.Value<int>();
            }
            
            if (jsonObject.TryGetValue("m_Update", out gameName))
            {
                target.m_Update = new SerilizeEditorNode();
                var runtimeNodeClassItem = gameName["m_Data"]["m_Name"];
                string runtimeNodeClassName = runtimeNodeClassItem.Value<string>();
                target.m_Update.m_Data = (GraphViewRuntimeNodeBase)assembly.CreateInstance(runtimeNodeClassName);
            
                var dataClassItem = gameName["m_Data"]["m_Data"]["m_ClassName"];
                string dataClassName = dataClassItem.Value<string>();
                target.m_Update.m_Data.m_Data = (NodeDataBase)assembly.CreateInstance(dataClassName);

                var childsItem = gameName["m_Data"]["m_Childs"];
                
                var loopItem = childsItem.First;
                while (loopItem != null)
                {
                    JProperty jProperty = loopItem.ToObject<JProperty>();
                    string id = jProperty.Name;
                    if (target.m_Update.m_Data.m_Childs.ContainsKey(id) == false)
                    {
                        target.m_Update.m_Data.m_Childs.Add(id,new List<int>());
                    }
                    foreach (var value3 in loopItem.Values())
                    {
                        target.m_Update.m_Data.m_Childs[id].Add(int.Parse(value3.ToString())); 
                    }
                    loopItem = loopItem.Next;
                }
                
                var guidName = gameName["m_Data"]["m_Guid"];
                string guidValue = guidName.Value<string>();
                target.m_Update.m_Data.m_Guid = int.Parse(guidValue);
                
                //这里开始设置坐标以及长宽等数据
                var xPosItem = gameName["m_XPos"];
                float xPos = xPosItem.Value<float>();
                var yPosItem = gameName["m_YPos"];
                float yPos = yPosItem.Value<float>();
                var widthItem = gameName["m_Width"];
                float width = widthItem.Value<float>();
                var heightItem = gameName["m_Height"];
                float height = heightItem.Value<float>();

                target.m_Update.m_XPos = xPos;
                target.m_Update.m_YPos = yPos;
                target.m_Update.m_Width = width;
                target.m_Update.m_Height = height;


            }
            return target;
        }
        catch (Exception ex)
        {
            throw new Exception("解析异常：" + ex.Message);
        }
    }
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {

    }
    public Type GetTypeByStr(JTokenType _type)
    {
        switch (_type)
        {
            case JTokenType.Float:
            {
                return typeof(float);
            }
            case JTokenType.Integer:
            {
                return typeof(int);
            }
            case JTokenType.Boolean:
            {
                return typeof(bool);
            }
            case JTokenType.String:
            {
                return typeof(string);
            }
        }

        return null;
    }
}


public class MyJsonConverter_Copy : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(GraphViewRuntimeNodeBase).IsAssignableFrom(objectType);
    }
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Assembly-CSharp");
            JObject jsonObject = JObject.Load(reader);
            GraphViewRuntimeNodeBase node = null;
            JToken gameName;
            if (jsonObject.TryGetValue("m_Name", out gameName))
            {
                string className = gameName.Value<string>();
                
                node = (GraphViewRuntimeNodeBase)assembly.CreateInstance(className);
            }
            if (jsonObject.TryGetValue("m_Data", out gameName))
            {
                var classItem = gameName["m_ClassName"];
                string className = classItem.Value<string>();
                
                node.m_Data = (NodeDataBase)assembly.CreateInstance(className);
                
                if (node.m_Data != null)
                {
                    var loopItem = gameName.First;
                    while (loopItem != null)
                    {
                        string nodeDataClassName = loopItem.ToObject<JProperty>().Name;
                        foreach (var value3 in loopItem.Values())
                        {
                            var propertyList = node.m_Data.GetType().GetFields();
                            foreach (System.Reflection.FieldInfo info in propertyList)
                            {
                                if (info.Name == nodeDataClassName) 
                                {
                                    //通过属性名称获取属性的值
                                    var result = Convert.ChangeType(value3.Value<string>(),GetTypeByStr(value3.Type));
                                    info.SetValue(node.m_Data,result);
                                }            
                            }
                        }
                        loopItem = loopItem.Next;
                    }
                }
            }
            if (jsonObject.TryGetValue("m_ParamInPortDic", out gameName))
            {
                var loopItem = gameName.First;
                while (loopItem != null)
                {
                    string beforeName = loopItem.ToObject<JProperty>().Name;
                    ParamPreNodeInfo temp = new ParamPreNodeInfo(0,"","");
                    foreach (var portChilds in loopItem.Values())
                    {
                        var checkName = portChilds.ToObject<JProperty>().Name;
                        if (string.Equals(checkName,"m_PreNodeUid"))
                        {
                            foreach (var jValue in portChilds.Values())
                            {
                                temp.m_PreNodeUid = int.Parse(jValue.ToString());
                            }
                        }
                        if (string.Equals(checkName,"m_PreNodeFuncName"))
                        {
                            foreach (var jValue in portChilds.Values())
                            {
                                temp.m_PreNodeFuncName = jValue.ToString();
                            }
                        }
                        if (string.Equals(checkName,"m_PreNodeTypeName"))
                        {
                            foreach (var jValue in portChilds.Values())
                            {
                                temp.m_PreNodeTypeName = jValue.ToString();
                            };
                        }
                        if (string.Equals(checkName,"m_PreNodeParamType"))
                        {
                            foreach (var jValue in portChilds.Values())
                            {
                                temp.m_PreNodeParamType = int.Parse(jValue.ToString());
                            };
                        }
                    }
                    if (node != null && node.m_ParamInPortDic.ContainsKey(beforeName))
                    {
                        node.m_ParamInPortDic[beforeName] = temp;
                    }
                    loopItem = loopItem.Next;
                }
            }
            if (jsonObject.TryGetValue("m_Guid", out gameName))
            {
                node.m_Guid = int.Parse(gameName.Value<string>());
            }
            return node;
        }
        catch (Exception ex)
        {
            throw new Exception("解析异常：" + ex.Message);
        }
    }
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {

    }
    public Type GetTypeByStr(JTokenType _type)
    {
        switch (_type)
        {
            case JTokenType.Float:
            {
                return typeof(float);
            }
            case JTokenType.Integer:
            {
                return typeof(int);
            }
            case JTokenType.Boolean:
            {
                return typeof(bool);
            }
            case JTokenType.String:
            {
                return typeof(string);
            }
        }

        return null;
    }
}

