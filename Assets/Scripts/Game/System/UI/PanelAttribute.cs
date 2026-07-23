using System;

public enum EscBehavior
{
    /// <summary>
    /// 不响应ESC（常驻界面，如 StatBar、HUD 主信息条等）
    /// </summary>
    None = 0,

    /// <summary>
    /// 只能被ESC关闭（最常见的普通界面）
    /// </summary>
    CloseOnly = 1,

    /// <summary>
    /// 既可以被ESC打开，也可以被ESC关闭（例如 Setting、Pause 菜单）
    /// </summary>
    OpenAndClose = 2,
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PanelAttribute : Attribute
{
    public string PanelKey { get; }
    public string Address { get; }
    public string Root { get; }
    
    /// <summary>
    /// 互斥组名称：同一个组内的面板互斥（同一时间只能打开一个）
    /// 为空则不参与互斥
    /// </summary>
    public string MuteGroup { get; set; } = "";
    public EscBehavior EscBehavior { get; set; } = EscBehavior.None;
    
    public PanelAttribute(string panelKey, string address, string root)
    {
        PanelKey = panelKey;
        Address = address;
        Root = root;
    }
}