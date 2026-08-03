/*
using System;

/// <summary>
/// 通用计数目标：<c>required &lt;= 0</c> 或 <c>current &gt;= required</c> 视为完成。
/// </summary>
[Serializable]
public class QuestGoalCounter : QuestGoal
{
    public int current;
    public int required;

    public override bool IsCompleted => required <= 0 || current >= required;

    /// <summary>增加进度；若因此达成条件会调用 <see cref="OnComplete"/>。</summary>
    public void AddProgress(int amount = 1)
    {
        if (amount <= 0 || required <= 0) return;
        if (IsCompleted) return;
        current = Math.Min(current + amount, required);
        if (IsCompleted)
            OnComplete();
    }

    public override void Reset()
    {
        current = 0;
    }
}
*/
