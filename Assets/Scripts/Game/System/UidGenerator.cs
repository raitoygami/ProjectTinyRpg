using System;
using System.Collections.Generic;

/// <summary>
///     全局道具 Uid 生成器（无限版）。
///     Uid 格式：高 32 位存储 itemId，低 32 位存储序号（从 1 开始递增）。
/// </summary>
public static class UidGenerator
{
    private static SaveData.UidGeneratorState State => Persist.Instance.GetState().State;

    /// <summary>
    ///     根据 itemId 生成新的全局唯一 Uid。
    /// </summary>
    public static long Generate(int itemId, bool stackable = false)
    {
        var map = State.NextSeqMap;
        if (!map.TryGetValue(itemId, out var nextSeq)) nextSeq = 1;

        // 低 32 位最大值约 42.9 亿，足够无限使用

        var uid = ((long) itemId << 32) | (uint) nextSeq;

        map[itemId] = stackable ? nextSeq : nextSeq + 1;
        return uid;
    }

    /// <summary>
    ///     从 Uid 反向解析出 itemId。
    /// </summary>
    public static int GetItemIdFromUid(long uid)
    {
        return (int) (uid >> 32);
    }

    /// <summary>
    ///     从 Uid 反向解析出序号。
    /// </summary>
    public static int GetSeqFromUid(long uid)
    {
        return (int) (uid & 0xFFFFFFFF);
    }

    /// <summary>
    ///     重置生成器状态（用于新游戏）。
    /// </summary>
    public static void Reset()
    {
        State.NextSeqMap.Clear();
    }
}