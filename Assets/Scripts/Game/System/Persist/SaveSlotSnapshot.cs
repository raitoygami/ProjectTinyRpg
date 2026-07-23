using System;
using System.Globalization;

/// <summary>
/// 单个存档槽的快照元数据（与 <c>slot_{n}.json</c> 同目录的 <c>slot_{n}.snapshot.json</c>），用于 UI 展示存档时的游戏内时间，无需反序列化整份 <see cref="GameData"/>。
/// </summary>
[Serializable]
public class SaveSlotSnapshot
{
    /// <summary>存档时的大回合索引（<see cref="TurnManager.CurrentGameTime"/>）。</summary>
    public int turnRound;

    /// <summary>存档时的累计游戏内分钟（与 <see cref="GameTimeConverter.TurnRoundToGameMinutes"/> 一致）。</summary>
    public int gameMinutes;

    /// <summary>设备 UTC 写入时间（ISO 8601），可选用于「何时存的档」。</summary>
    public string savedAtUtcIso;

    /// <summary>基于当前 <see cref="GameTimeConverter.MinutesPerGameDay"/> 等设置，从 <see cref="gameMinutes"/> 生成展示文案。</summary>
    public string BuildGameTimeDisplay()
    {
        var t = new GameWorldTime(gameMinutes);
        return t.ToString();
    }

    /// <summary>存档时刻 UTC；解析失败返回 <c>null</c>。</summary>
    public DateTime? TryGetSavedAtUtc()
    {
        if (string.IsNullOrEmpty(savedAtUtcIso)) return null;
        return DateTime.TryParse(savedAtUtcIso, null, DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
    }
}
