using System;
using System.Collections.Generic;
using cfg;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 场景中可拾取单位；持有 <see cref="ItemStack"/> 列表。外观仅在 Awake 时由预制体上的 _sprite 生成网格与碰撞体。
/// </summary>
public class LootUnit : MonoBehaviour
{
    [Tooltip("抛物线顶点相对起点与终点连线的高度")]
    [SerializeField] private float _arcHeight = 2f;
    [Tooltip("掉落动画时长")]
    [SerializeField] private float _dropDuration = 0.4f;
    [Tooltip("抛物线水平段缓动")]
    [SerializeField] private Ease _dropEase = Ease.OutQuad;
    [Tooltip("水平位移非零时：绕轴 Up×水平掉落方向 旋转的圈数")]
    [SerializeField] private float _dropSpinTurns = 2f;
    [Tooltip("仅 Awake 时使用：据此 Sprite 生成显示网格与 MeshCollider")]
    [SerializeField] private Sprite _sprite;
    [Header("合并反馈")]
    [Tooltip("相对当前 localScale 的放大峰值倍数")]
    [SerializeField] private float _mergePulseScale = 1.18f;
    [Tooltip("放大阶段时长（秒）")]
    [SerializeField] private float _mergePulseUpDuration = 0.12f;
    [Tooltip("缩回阶段时长（秒）")]
    [SerializeField] private float _mergePulseDownDuration = 0.18f;
    [SerializeField] private Ease _mergePulseEaseUp = Ease.OutQuad;
    [SerializeField] private Ease _mergePulseEaseDown = Ease.InOutQuad;

    private readonly List<ItemStack> _stacks = new();
    private Sequence _mergePulseSequence;

    /// <summary>与 <see cref="InventoryMgr.AllocateItemStackUid"/> 同一规则，保证全场景堆叠 uid 唯一。</summary>
    public static long AllocateUidForNewStack(int itemId)
    {
        return UidGenerator.Generate(itemId);
    }

    // ── Public accessors ────────────────────────────────────────────────

    public List<ItemStack> Stacks => _stacks;
    public IReadOnlyList<ItemStack> LootStacks => _stacks;

    /// <summary>主堆叠道具 id；无堆叠时为 0。</summary>
    public int ItemId => _stacks.Count > 0 ? _stacks[0].ItemId : 0;

    /// <summary>主堆叠数量；无堆叠时为 0。</summary>
    public int Count => _stacks.Count > 0 ? _stacks[0].Count : 0;

    public float DropDuration => _dropDuration;

    // ── Query helpers ───────────────────────────────────────────────────

    public ItemStack FindByUid(long uid)
    {
        foreach (var s in _stacks)
            if (s != null && s.Uid == uid)
                return s;
        return null;
    }

    public bool RemoveByUid(long uid)
    {
        for (var i = 0; i < _stacks.Count; i++)
        {
            if (_stacks[i] == null || _stacks[i].Uid != uid) continue;
            _stacks.RemoveAt(i);
            return true;
        }
        return false;
    }

    /// <summary>添加一条 ItemStack（已设好 UID 等字段）到列表末尾。</summary>
    public void AddItemStack(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return;
        _stacks.Add(stack);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        ApplyPrefabSpriteToMeshAndCollider();
    }

    private void ApplyPrefabSpriteToMeshAndCollider()
    {
        var avatar = GetComponent<AgentAvatar>();
        if (avatar == null)
            return;
        avatar.SetSprite(_sprite);
        RefreshPickupCollider();
    }

    // ── Set / Add / Merge ───────────────────────────────────────────────

    /// <summary>清空并设为单条掉落。</summary>
    public void SetLootItem(int itemId, int count)
    {
        _stacks.Clear();
        AddOrMergeLoot(itemId, count);
    }

    /// <summary>增加一条掉落；可堆叠 id 与已有同 id 合并数量，否则新建一条。</summary>
    public void AddOrMergeLoot(int itemId, int count)
    {
        if (itemId <= 0) return;
        var n = count > 0 ? count : 1;
        if (IsItemStackable(itemId))
        {
            foreach (var s in _stacks)
            {
                if (s.ItemId != itemId) continue;
                s.Count += n;
                return;
            }
        }

        _stacks.Add(new ItemStack
        {
            Uid = AllocateUidForNewStack(itemId),
            ItemId = itemId,
            Count = n,
            PivotCol = 0,
            PivotRow = 0
        });
    }

    private static bool IsItemStackable(int itemId)
    {
        if (itemId <= 0) return false;
        if (!ConfigManager.HasInstance()) return false;
        var def = ConfigManager.Instance.GetItemBase(itemId);
        return def != null && def.Stackable;
    }

    // ── Pickup (old click-list flow, still used) ────────────────────────

    public void GetPickup(out int itemId, out int count)
    {
        itemId = ItemId;
        count = Count;
    }

    public void Pickup(int index)
    {
        if (index < 0 || index >= _stacks.Count) return;
        if (!InventoryMgr.HasInstance())
        {
            DevLog.LogWarning("[Loot] Pickup failed: no_inventory_manager");
            return;
        }

        var stack = _stacks[index];
        if (stack == null || stack.IsEmpty) return;

        if (!InventoryMgr.Instance.TryAddItemStack(stack))
        {
            DevLog.LogWarning($"[Loot] Pickup failed: itemId={stack.ItemId} count={stack.Count}");
            return;
        }

        _stacks.RemoveAt(index);
    }

    // ── Grid slot calculation ───────────────────────────────────────────

    // ── Collider ────────────────────────────────────────────────────────

    private void RefreshPickupCollider()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return;
        var mc = GetComponent<MeshCollider>();
        if (mc == null)
            mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = false;
    }

    // ── Merge pulse ─────────────────────────────────────────────────────

    private void OnDisable()
    {
        KillMergePulse();
        var er = GetComponent<AgentAvatar>();
        if (er != null)
            er.Cover(false);
    }

    public void PlayMergePulse()
    {
        if (!isActiveAndEnabled || _mergePulseScale <= 1f) return;
        KillMergePulse();
        var baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 1e-6f)
            baseScale = Vector3.one;
        var peak = baseScale * _mergePulseScale;
        _mergePulseSequence = DOTween.Sequence();
        _mergePulseSequence.Append(transform.DOScale(peak, _mergePulseUpDuration).SetEase(_mergePulseEaseUp));
        _mergePulseSequence.Append(transform.DOScale(baseScale, _mergePulseDownDuration).SetEase(_mergePulseEaseDown));
        _mergePulseSequence.SetUpdate(UpdateType.Normal);
        _mergePulseSequence.SetTarget(gameObject);
    }

    private void KillMergePulse()
    {
        if (_mergePulseSequence != null && _mergePulseSequence.IsActive())
            _mergePulseSequence.Kill();
        _mergePulseSequence = null;
    }

    // ── Drop animation ──────────────────────────────────────────────────

    private static bool TryGetDropSpinAxis(Vector3 fromWorld, Vector3 toWorld, out Vector3 axis)
    {
        axis = Vector3.zero;
        var delta = toWorld - fromWorld;
        var horizontal = new Vector3(delta.x, delta.y, 0f);
        if (horizontal.sqrMagnitude < 1e-8f)
            return false;
        axis = Vector3.Cross(Vector3.forward, horizontal.normalized);
        if (axis.sqrMagnitude < 1e-10f)
            return false;
        axis.Normalize();
        return true;
    }

    public void Drop(Vector3 fromWorld, Vector3 toWorld, Action onComplete = null)
    {
        KillMergePulse();
        transform.DOKill();
        transform.position = fromWorld;
        var rotationAtStart = transform.rotation;
        var canSpin = TryGetDropSpinAxis(fromWorld, toWorld, out var spinAxis);
        float t = 0f;
        DOTween.To(() => t, x => t = x, 1f, _dropDuration)
            .SetEase(_dropEase)
            .SetTarget(gameObject)
            .SetUpdate(UpdateType.Normal)
            .OnUpdate(() =>
            {
                var linear = Vector3.Lerp(fromWorld, toWorld, t);
                var arc = _arcHeight * 4f * t * (1f - t);
                transform.position = linear + Vector3.up * arc;

                if (canSpin)
                {
                    var spinDegrees = -360f * _dropSpinTurns * t;
                    transform.rotation = Quaternion.AngleAxis(spinDegrees, spinAxis) * rotationAtStart;
                }
                else
                    transform.rotation = rotationAtStart;
            })
            .OnComplete(() =>
            {
                transform.rotation = rotationAtStart;
                onComplete?.Invoke();
            });
    }

    /// <summary>跨容器迁入：类型是否可接受（有合法配置行）。</summary>
    public bool CanAcceptItemStackData(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
            return false;
        if (!ConfigManager.HasInstance())
            return false;
        return ConfigManager.Instance.GetItemBase(stack.ItemId) != null;
    }

    /// <summary>跨容器迁入：复制堆叠并加入 <see cref="Stacks"/>；保留源 <see cref="ItemStack.Uid"/>（与网格块 Id 一致）。</summary>
    public bool TryReceiveTransferredStack(ItemStack sourceStack)
    {
        if (sourceStack == null || sourceStack.IsEmpty)
            return false;
        if (!CanAcceptItemStackData(sourceStack))
            return false;
        var copy = sourceStack.Clone();
        if (copy.Uid <= 0)
            copy.Uid = AllocateUidForNewStack(sourceStack.ItemId);
        copy.PivotCol = 0;
        copy.PivotRow = 0;
        AddItemStack(copy);
        return true;
    }

    /// <summary>回收到对象池前清空堆叠并杀掉本物体上的 DOTween。</summary>
    public void ClearForPool()
    {
        KillMergePulse();
        transform.DOKill();
        _stacks.Clear();
    }
}
