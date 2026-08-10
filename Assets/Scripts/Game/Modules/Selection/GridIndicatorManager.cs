using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.Tilemaps;

public class GridIndicatorManager : Singleton<GridIndicatorManager>
{

    private Grid _indicatorGrid;
    // 新增：三个 Tilemap 引用
    
    private Tilemap _tilemapAbilityCastRange;      // 施法范围
    private Tilemap _tilemapAbilityAffectRange;    // 技能生效范围
    private Tilemap _tilemapTelegraph;      // 敌方预警
    
    private Transform _root;
    private GameObject _cursorMark;

    private RuleTile _tileAbilityCastRange;
    private Tile _tileAbilityAffectRange;
    private Tile _tileAbilityTelegraph;
    
    private readonly Dictionary<Vector3Int, int> _telegraphRefCount = new();
   
    public void Setup(TileAssetTable tileAssetTable)
    {
        _root = new GameObject("Root").transform;
        _cursorMark = Instantiate(tileAssetTable.NavigationMarkEnd, _root, true);
        _cursorMark.SetActive(false);
        _root.SetParent(transform);
        _root.transform.position = Vector3.zero;

        
        _tileAbilityCastRange = tileAssetTable.TileAbilityCastRange;
        _tileAbilityAffectRange = tileAssetTable.TileAbilityAffectRange;
        _tileAbilityTelegraph = tileAssetTable.TileAbilityTelegraph;
        
        var gridObj = new GameObject("IndicatorGrid");
        gridObj.transform.SetParent(transform);
        gridObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0);

        _indicatorGrid = gridObj.AddComponent<Grid>();
        _indicatorGrid.cellGap = Vector3.zero;
        _indicatorGrid.cellSize = new Vector3(1, 1, 0);
        _indicatorGrid.cellLayout = GridLayout.CellLayout.Rectangle;
        _indicatorGrid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        
        var defaultLayer = SortingLayer.NameToID("Default");
        _tilemapAbilityAffectRange = CreateTilemap(gridObj.transform, "Layer Ability Affect Range", defaultLayer, -2);
        _tilemapAbilityCastRange = CreateTilemap(gridObj.transform, "Layer Ability Cast Range", defaultLayer, -3);
        _tilemapTelegraph = CreateTilemap(gridObj.transform, "Layer Ability Telegraph", defaultLayer, -4);
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
            _tilemapTelegraph.SetTileFlags(cell, _telegraphRefCount[cell] == 1 ? TileFlags.None : TileFlags.LockColor);
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
            _tilemapTelegraph.SetTileFlags(cell, _telegraphRefCount[cell] == 1 ? TileFlags.None : TileFlags.LockColor);
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

    public void ShowCastableRange(List<Vector3Int> castableRange)
    {
        foreach (var cell in castableRange.Select(location => _indicatorGrid.WorldToCell(location)))
        {
            _tilemapAbilityCastRange.SetTile(cell, _tileAbilityCastRange);
        }
    }

    public void ShowAffectableRange(List<Vector3Int> affectableRange)
    {
        foreach (var cell in affectableRange.Select(location => _indicatorGrid.WorldToCell(location)))
        {
            _tilemapAbilityAffectRange.SetTile(cell, _tileAbilityAffectRange);
        }
    }

    public void ClearAffectableRange()
    {
        _tilemapAbilityAffectRange.ClearAllTiles();
    }
    
    public void HideAbilityPreview()
    {
        _tilemapAbilityCastRange.ClearAllTiles();
        _tilemapAbilityAffectRange.ClearAllTiles();
    }
    
    public void DrawCursorMark(Vector3 location, bool moveable)
    {
        var spriteRenderer = _cursorMark.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = moveable ? 0 : 1000;
        spriteRenderer.color = moveable ? Color.green : Color.red;
        
        _cursorMark.transform.position = location.GridToWorld();
        _cursorMark.transform.localScale = Vector3.one * 0.25f;
        _cursorMark.transform.DOScale(Vector3.one, 0.2f).SetTarget(_cursorMark);
        _cursorMark.SetActive(true);
    }
    
    public void HideCursorMark()
    {
        _cursorMark.transform.DOKill();
        _cursorMark.SetActive(false);
    }

    
    
    
    protected override void OnRelease()
    {
        if (_root != null)
            Destroy(_root);
        _root = null;
        if (_cursorMark)
            _cursorMark.transform.DOKill();
        _cursorMark = null;
    }

    
}