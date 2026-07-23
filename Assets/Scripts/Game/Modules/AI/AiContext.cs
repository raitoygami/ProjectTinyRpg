using cfg;

/// <summary>单回合 AI 执行上下文。</summary>
public struct AiContext
{
    public AIEntity Owner;
    public Blackboard Board;
    public t_AI AiConfig;
}

/// <summary>敌人 AI 行为阶段（与配置表策略配合，运行时状态）。</summary>
public enum AiPhaseDefault
{
    Idle,
    /// <summary>敌人在视野内但未满足接战条件，按 ThreatTime 倒计时。</summary>
    Suspicious,
    /// <summary>追击 / 攻击（行为树 Selector）。</summary>
    Engaged,
    /// <summary>追击厌倦且目标已脱离出生点周围范围，沿路径返回出生格。</summary>
    ReturningHome,
}

