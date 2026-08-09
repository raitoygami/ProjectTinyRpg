using System;
using UnityEngine;

// 技能包括
// 1：施法范围 cast range
// 2：施法类型 指向型， 非指向型
// 3：技能范围类型
// 4: 技能范围
// eg : CastRange 1 :刀普通攻击=》CastType.Positional & CastCenterType.Owner & AffectType.SelectPoint & Range = 0
// eg : CastRange 0 :自身buff => CastType.Auto & CastCenterType.Owner & AffectType.Self & Range = 0 (当然也可以是N 表示自身范围N)

[Serializable]
public enum AbilityTargetType
{
    None,
    Self,
    Enemy,
    Any,
    EmptyGround,
}

// 施放目标模式 确定技能施法范围框框
public enum CastTargetingMode
{
    Auto,        // 点击自动释放
    Directed,    // 有明确方向/目标（比如从施法者指向目标）
    Positional   // 选择位置/区域，不关心方向
}

// 施法中心
public enum CastCenterType
{
    Owner, // 施法者 
    Cursor,// 光标
}

// 作用类型
public enum AffectType
{
    SelectPoint = 0, //选取的点
    SelectSquare,
    Self, //自身
    Custom,
}

// 结合和