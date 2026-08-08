using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Tilemaps;

public class GridIndicatorManager : Singleton<GridIndicatorManager>
{

    private Grid _indicatorGrid;
    // 新增：三个 Tilemap 引用
    
    private Tilemap _tilemapAbilityRange;    // 技能生效范围
    private Tilemap _tilemapAbilityCastRange;      // 施法范围
    private Tilemap _tilemapTelegraph;      // 敌方预警
    
    private Transform _root;
    private GameObject _cursorMark;

    private RuleTile _tileAbilityRange;
    private Tile _tileAbilityCastRange;
    private Tile _tileAbilityTelegraph;
    
    private readonly Dictionary<Vector3Int, int> _telegraphRefCount = new();
   
    public void Setup(NavigationSettings t_Settings)
    {
        _root = new GameObject("Root").transform;
        _cursorMark = Instantiate(t_Settings.NavigationMarkEnd, _root, true);
        _cursorMark.SetActive(false);
        _root.SetParent(transform);
        _root.transform.position = Vector3.zero;

        _tileAbilityRange = t_Settings.TileAbilityRange;
        _tileAbilityCastRange = t_Settings.TileAbilityCastRange;
        _tileAbilityTelegraph = t_Settings.TileAbilityTelegraph;
        
        var gridObj = new GameObject("IndicatorGrid");
        gridObj.transform.SetParent(transform);
        gridObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0);

        _indicatorGrid = gridObj.AddComponent<Grid>();
        _indicatorGrid.cellGap = Vector3.zero;
        _indicatorGrid.cellSize = new Vector3(1, 1, 0);
        _indicatorGrid.cellLayout = GridLayout.CellLayout.Rectangle;
        _indicatorGrid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        
        var defaultLayer = SortingLayer.NameToID("Default");
        _tilemapAbilityRange = CreateTilemap(gridObj.transform, "Layer Ability Range", defaultLayer, -2);
        _tilemapAbilityCastRange = CreateTilemap(gridObj.transform, "Layer Ability Cast Range", defaultLayer, -3);
        _tilemapTelegraph = CreateTilemap(gridObj.transform, "Layer Ability Telegraph", defaultLayer, -1);
    }
    
    // 辅助方法：创建 Tilemap 并设置 Sorting Order
    private Tilemap CreateTilemap(Transform parent, string tilemapName, int sortingLayerID, int order)
    {
        var child = new GameObject(tilemapName);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = Vector3.zero;

        var tilemap = child.AddComponent<Tilemap>();
        var tilemapRenderer = child.AddComponent<TilemapRenderer>();

        // 设置排序
        tilemapRenderer.sortingLayerID = sortingLayerID;
        tilemapRenderer.sortingOrder = order;

        // 可选：设置材质（使用默认或者透明材质）
        // renderer.material = ...;

        return tilemap;
    }

    private const int _maxTelegraphCount = 5; // 达到此数量时为灰色

    public void AddTelegraph(Vector3Int[] worldPositions)
    {
        if (_indicatorGrid == null)
            return;
        foreach (var worldPos in worldPositions)
        {
            // 将世界坐标转换为格子坐标
            var cell = _indicatorGrid.WorldToCell(worldPos);
        
            // 引用计数 +1
            _telegraphRefCount.TryAdd(cell, 0);
            _telegraphRefCount[cell]++;
            // 只有第一个添加的才绘制 Tile
            if (_telegraphRefCount[cell] == 1)
            {
                _tilemapTelegraph.SetTile(cell, _tileAbilityTelegraph);
            }
            
            // 计算插值因子 t：0 → 白色，1 → 灰色
            var t = Mathf.Clamp01((_telegraphRefCount[cell] - 1) / (float)_maxTelegraphCount);
            // 从白色渐变到中性灰 (0.5, 0.5, 0.5)，可改为 Color.gray
            var color = Color.Lerp(Color.white, Color.black, t);
            // 可选：让透明度也随数量稍微降低（不强制）
            // color.a = 1f - t * 0.3f; 
            _tilemapTelegraph.SetColor(cell, color);
            
        }
    }

    public void RemoveTelegraph(Vector3Int[] worldPositions)
    {
        if (_indicatorGrid == null)
            return;
        foreach (var worldPos in worldPositions)
        {
            // 将世界坐标转换为格子坐标
            var cell = _indicatorGrid.WorldToCell(worldPos);

            if (!_telegraphRefCount.ContainsKey(cell) || _telegraphRefCount[cell] <= 0)
            {
                Debug.LogWarning($"试图移除未预警的格子 {cell}，请检查逻辑配对");
                continue;
            }

            _telegraphRefCount[cell]--;
            
            // 计算插值因子 t：0 → 白色，1 → 灰色
            var t = Mathf.Clamp01((_telegraphRefCount[cell] - 1) / (float)_maxTelegraphCount);
            // 从白色渐变到中性灰 (0.5, 0.5, 0.5)，可改为 Color.gray
            var color = Color.Lerp(Color.white, Color.black, t);
            // 可选：让透明度也随数量稍微降低（不强制）
            // color.a = 1f - t * 0.3f; 
            _tilemapTelegraph.SetColor(cell, color);
            
            // 计数归零时清除 Tile
            if (_telegraphRefCount[cell] > 0) continue;
            
            _tilemapTelegraph.SetTile(cell, null);
            _telegraphRefCount.Remove(cell); // 释放内存
        }
    }

    public void ClearAll()
    {
        ClearAllTelegraphs();
    }
    
    // 可选：紧急清理所有预警（比如战斗结束）
    private void ClearAllTelegraphs()
    {
        _tilemapTelegraph.ClearAllTiles();
        _telegraphRefCount.Clear();
    }
    
    public void Hide()
    {
    }
    
    public void DrawCursorMark(Vector3 t_GridPosition, bool t_moveable)
    {
        var spriteRenderer = _cursorMark.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = t_moveable ? 0 : 1000;
        spriteRenderer.color = t_moveable ? Color.green : Color.red;
        
        _cursorMark.transform.position = t_GridPosition.GridToWorld();
        _cursorMark.transform.localScale = Vector3.one * 0.25f;
        _cursorMark.transform.DOScale(Vector3.one, 0.2f).SetTarget(_cursorMark);
        _cursorMark.SetActive(true);
    }

    public void ClearPath()
    {
        _cursorMark.transform.DOKill(false);
        _cursorMark.SetActive(false);
    }

    /// <summary>按 Ability 配置的 <see cref="SelectParam"/> 高亮技能范围；起点与朝向由 <see cref="Ability.TryGetSkillPreviewFrame"/> 得到。</summary>
    public void ShowSkillRangePreview(SelectParam param, Vector3 ownerGrid, Vector3 previewOriginGrid,
        Vector3 skillFaceDirection, LayerMask t_LayerMask)
    {
       
    }

    public void HideSkillRangePreview()
    {
        
    }

    private static bool IsMoveableForMask(PathCell cell, IPathNodeAgent owner)
    {
        return cell != null && PathFinder.IsWalkableCell(cell, owner);
    }

    // 目前只实现了直线方向型的技能(获取直线上的格子)
    // 后去会根据技能类型返回技能预览
    public List<Vector3Int> Preview(Entity owner, Vector3 targetPosition, Ability ability)
    {
        if (owner == null && ability == null)
            return null;
        var direction = owner.GridPosition.Direction(targetPosition);
        var ret = owner.GridPosition.Line(direction, ability.GetRange());
        return ret;
    }
    
    public List<Vector2Int> Preview(int range, IPathNodeAgent owner, bool t_ShowRange = true)
    {
        var ret = new List<Vector2Int>();
        if (owner == null)
            return ret;

        var sx = owner.X;
        var sy = owner.Y;

        var nodes = new Dictionary<Vector2Int, PathCell>();
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));

        var xMin = sx - range;
        var xMax = sx + range;
        var yMin = sy - range;
        var yMax = sy + range;
        for (var x = xMin; x <= xMax; x++)
        {
            for (var y = yMin; y <= yMax; y++)
            {
                var target = PathFinder.Instance.GetCell(x, y);
                if (!IsMoveableForMask(target, owner))
                    continue;

                if (target.X != sx || target.Y != sy)
                {
                    var path = PathFinder.Instance.Navigate(owner, sx, sy, target.X, target.Y);
                    var navigate = path?.Count ?? 0;
                    if (navigate > range || navigate == 0)
                    {
                        continue;
                    }
                }

                var local = new Vector2Int(x, y);
                nodes.Add(local, target);
                ret.Add(local);
            }
        }
        return ret;
    }

    private void OnDestroy()
    {
        if (_root != null)
            Destroy(_root);
        _root = null;
    }
}