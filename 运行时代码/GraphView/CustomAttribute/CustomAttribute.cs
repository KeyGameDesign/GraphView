using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field|AttributeTargets.Property | AttributeTargets.Method)]
public class CustomAttribute : Attribute
{
    /// <summary>
    /// 构造方法初始化
    /// </summary>
    /// <param name="DisplayName"></param>
    public CustomAttribute(string DisplayName)
    {
        this.DisplayName = DisplayName;
    }
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; }
}
