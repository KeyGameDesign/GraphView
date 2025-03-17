using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// 用来序列化蓝图编辑器的数据
/// </summary>
public class SerilizeNode
{
    //每个节点的名称
    public string m_Name;
    //每个节点的数据
    public GraphViewRuntimeNodeBase m_Data;
    //所有子节点的名称
    public Dictionary<string, List<SerilizeNode>> m_Childs;
    //当前节点的坐标
    public float m_XPos;
    public float m_YPos;
    public float m_Width;
    public float m_Height;

}
public class SerilizeEditorNode
{   
    //每个节点的名称
    public string m_EditorClassName;
    //每个节点的数据
    public GraphViewRuntimeNodeBase m_Data;
    //所有子节点的名称
    public Dictionary<string, List<int>> m_Childs;
    //当前节点的坐标
    public float m_XPos;
    public float m_YPos;
    public float m_Width;
    public float m_Height;

}
public class GraphEditorNode
{
    /// <summary>
    /// 根节点
    /// </summary>
    public SerilizeEditorNode m_Root;

    public SerilizeEditorNode m_Update;
    //所有节点的集合
    public Dictionary<int, SerilizeEditorNode> m_AllDic = new Dictionary<int, SerilizeEditorNode>();
    /// <summary>
    /// 当前这个蓝图的最大GUID
    /// </summary>
    public int m_MaxGUID;
}
public class SerilizeEditorDescNode
{
    /// <summary>
    /// 描述节点文本内容
    /// </summary>
    public string m_Text;
    //当前节点的坐标
    public float m_XPos;
    public float m_YPos;
    public float m_Width;
    public float m_Height;
}
public class GraphEditorDescList
{
    public List<SerilizeEditorDescNode> m_List = new List<SerilizeEditorDescNode>();
}
#endif


public class MyJsonConverter_Runtime : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(GraphComponentData).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            JObject jsonObject = JObject.Load(reader);
            GraphComponentData target = new GraphComponentData();
            JToken gameName;
            if (jsonObject.TryGetValue("m_Root", out gameName))
            {
                var classItem = gameName["m_Name"];
                string className = classItem.Value<string>();
            
                var dataClassItem = gameName["m_Data"]["m_ClassName"];
                string dataClassName = dataClassItem.Value<string>();
                
                Type typeOfStateName = Type.GetType(className);
                target.m_Root = (GraphViewRuntimeNodeBase)Activator.CreateInstance(typeOfStateName);
                Type dataClassNameType = Type.GetType(dataClassName);
                target.m_Root.m_Data = (NodeDataBase)Activator.CreateInstance(dataClassNameType);
                target.m_Root.m_Childs.Add("Out",new List<int>());
                foreach (var VARIABLE in gameName["m_Childs"]["Out"])
                {
                    target.m_Root.m_Childs["Out"].Add(int.Parse(VARIABLE.ToString()));
                }
                
                var guidName = gameName["m_Guid"];
                string guidValue = guidName.Value<string>();
                target.m_Root.m_Guid = int.Parse(guidValue);
            
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
            
                        GraphViewRuntimeNodeBase node = null;
                        NodeDataBase nodeData = null;
                        foreach (var vale1 in loopItem.Values())
                        {
                            JProperty jProperty1 = vale1.ToObject<JProperty>();
                            string name1 = jProperty1.Name;
                            if (string.Equals(name1,"m_Name"))
                            {
                                //创建运行实例
                                foreach (var value2 in vale1.Values())
                                {
                                    //当前是要创建的实例对象
                                    string name2 = value2.Value<string>();
                                    Type tempType = Type.GetType(name2);
                                    node = (GraphViewRuntimeNodeBase)Activator.CreateInstance(tempType);
                                    if (target.m_AllDic.ContainsKey(int.Parse(id)) == false && node != null)
                                    {
                                        target.m_AllDic.Add(int.Parse(id),node);
                                    }
                                }
                            }
                            if (string.Equals(name1,"m_Childs"))
                            {
                                //创建运行实例
                                foreach (var value2 in vale1.Values())
                                {
                                    string beforeName = value2.ToObject<JProperty>().Name;
                                    if (node != null && node.m_Childs.ContainsKey(beforeName) == false)
                                    {
                                        node.m_Childs.Add(beforeName,new List<int>());
                                    }
                                    
                                    foreach (var portChilds in value2.Values())
                                    {
                                        if (node != null)
                                        {
                                            node.m_Childs[beforeName].Add(int.Parse(portChilds.ToString()));
                                        }
                                    }
                                }
                            }
                            if (string.Equals(name1,"m_ParamInPortDic"))
                            {
                                foreach (var value3 in vale1.Values())
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
                            if (string.Equals(name1,"m_Guid"))
                            {
                                //创建运行实例
                                foreach (var value2 in vale1.Values())
                                {
                                    //当前是要创建的实例对象
                                    string name2 = value2.Value<string>();
                                    if (node != null)
                                    {
                                        node.m_Guid = int.Parse(name2);
                                    }
                                }
                            }
                            if (string.Equals(name1,"m_Data"))
                            {
                                foreach (var value2 in vale1.Values())
                                {
                                    string nodeDataClassName = value2.ToObject<JProperty>().Name;
                                    foreach (var value3 in value2.Values())
                                    {
                                        if (string.Equals(nodeDataClassName,"m_ClassName"))
                                        {
                                            //创建数据实例
                                            Type tempClass = Type.GetType(value3.Value<string>());
                                            nodeData = (NodeDataBase)Activator.CreateInstance(tempClass);
                                            break;
                                        }
                                    }

                                }
                                if (nodeData != null)
                                {
                                    foreach (var value2 in vale1.Values())
                                    {
                                        string nodeDataClassName = value2.ToObject<JProperty>().Name;
                                        foreach (var value3 in value2.Values())
                                        {
                                            var propertyList = nodeData.GetType().GetFields();
                                            foreach (System.Reflection.FieldInfo info in propertyList)
                                            {
                                                if (info.Name == nodeDataClassName) 
                                                {
                                                    //通过属性名称获取属性的值
                                                    var result = Convert.ChangeType(value3.Value<string>(),GetTypeByStr(value3.Type));
                                                    info.SetValue(nodeData,result);
                                                }            
                                            }
                                        }

                                    }

                                }

                            }

                        }
            
                        if (node != null && nodeData != null)
                        {
                            node.m_Data = nodeData;
                        }
                        
                        loopItem = loopItem.Next;
                    }
                }
                
            
            }
            //serializer.Populate(jsonObject.CreateReader(), target); 
            
            
            if (jsonObject.TryGetValue("m_UpdateRoot", out gameName))
            {
                var classItem = gameName["m_Name"];
                string className = classItem.Value<string>();
            
                var dataClassItem = gameName["m_Data"]["m_ClassName"];
                string dataClassName = dataClassItem.Value<string>();
                
                Type typeOfStateName = Type.GetType(className);
                target.m_UpdateRoot = (GraphViewRuntimeNodeBase)Activator.CreateInstance(typeOfStateName);
                Type dataClassNameType = Type.GetType(dataClassName);
                target.m_UpdateRoot.m_Data = (NodeDataBase)Activator.CreateInstance(dataClassNameType);
                target.m_UpdateRoot.m_Childs.Add("Out",new List<int>());
                foreach (var VARIABLE in gameName["m_Childs"]["Out"])
                {
                    target.m_UpdateRoot.m_Childs["Out"].Add(int.Parse(VARIABLE.ToString()));
                }
                
                var guidName = gameName["m_Guid"];
                string guidValue = guidName.Value<string>();
                target.m_UpdateRoot.m_Guid = int.Parse(guidValue);
            
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
