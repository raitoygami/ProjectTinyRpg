using System;

/// <summary>
/// 大回合索引与游戏内时间的换算。任务、日志等应使用 <see cref="TurnRoundToGameMinutes"/> 后的值作为「游戏内时间」。
/// </summary>
public static class GameTimeConverter
{
    /// <summary>每个大回合推进的游戏内分钟数（&gt;0）。调大则同回合数下经过的「游戏内时间」更长。</summary>
    public static int GameMinutesPerRound { get; set; } = 10;

    /// <summary>一个游戏内「日」的分钟数，用于把累计分钟拆成 日 + 日内时刻。默认 1440（24×60）。</summary>
    public static int MinutesPerGameDay { get; set; } = 24 * 60;

    /// <summary>
    /// 回合 → 游戏内时间：从第 0 回合起累计的分钟数。
    /// <para>公式：<c>gameMinutes = roundIndex × GameMinutesPerRound</c>。</para>
    /// </summary>
    public static int TurnRoundToGameMinutes(int roundIndex)
    {
        if (roundIndex < 0)
            roundIndex = 0;
        return roundIndex * Math.Max(1, GameMinutesPerRound);
    }

    /// <summary>
    /// 游戏内时间 → 回合：累计分钟对应的回合索引（向下取整）。
    /// <para>公式：<c>roundIndex = ⌊ gameMinutes / GameMinutesPerRound ⌋</c>。</para>
    /// </summary>
    public static int GameMinutesToTurnRound(int totalGameMinutes)
    {
        if (totalGameMinutes < 0)
            totalGameMinutes = 0;
        int per = Math.Max(1, GameMinutesPerRound);
        return totalGameMinutes / per;
    }
}

/// <summary>
/// 由累计游戏内分钟构造的只读视图，便于 UI 显示「第几天、几时几分」。
/// </summary>
public readonly struct GameWorldTime : IEquatable<GameWorldTime>
{
    public int TotalMinutes { get; }

    public GameWorldTime(int totalGameMinutes)
    {
        TotalMinutes = totalGameMinutes < 0 ? 0 : totalGameMinutes;
    }

    public static GameWorldTime FromTurnRound(int roundIndex) =>
        new GameWorldTime(GameTimeConverter.TurnRoundToGameMinutes(roundIndex));

    public int DayIndex
    {
        get
        {
            int d = Math.Max(1, GameTimeConverter.MinutesPerGameDay);
            return TotalMinutes / d;
        }
    }

    public int MinuteOfDay
    {
        get
        {
            int d = Math.Max(1, GameTimeConverter.MinutesPerGameDay);
            return TotalMinutes % d;
        }
    }

    public int HourOfDay => MinuteOfDay / 60;

    public int MinuteInHour => MinuteOfDay % 60;

    public bool Equals(GameWorldTime other) => TotalMinutes == other.TotalMinutes;

    public override bool Equals(object obj) => obj is GameWorldTime other && Equals(other);

    public override int GetHashCode() => TotalMinutes;

    public static bool operator ==(GameWorldTime a, GameWorldTime b) => a.Equals(b);

    public static bool operator !=(GameWorldTime a, GameWorldTime b) => !a.Equals(b);

    public override string ToString() =>
        $"Day {DayIndex} {HourOfDay:D2}:{MinuteInHour:D2} (total {TotalMinutes} min)";
}
