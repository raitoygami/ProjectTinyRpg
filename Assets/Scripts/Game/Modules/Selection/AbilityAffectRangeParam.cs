using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>选择区域参数基类。</summary>
[Serializable]
public abstract class AbilityAffectRangeParam
{

}

/// <summary>扇形选择参数（格坐标）。运行时：顶点为鼠标格，对称轴为施法者→鼠标。</summary>
[Serializable]
public class RangeSectorParam : AbilityAffectRangeParam
{
    [Header("扇形（Grid 坐标）")]
    [Tooltip("扇形顶点参考（格）；运行时顶点为鼠标格")]
    public Vector3 sectorGizmoStartPos;

    [Tooltip("2D 朝向（施法者→鼠标，编辑器参考；可与运行时一致）")]
    [FormerlySerializedAs("sectorGizmoEndPos")]
    public Vector3 sectorGizmoDirection;

    [Tooltip("扇形半径（格）")]
    public int sectorGizmoRadius;

    [Tooltip("扇形总张角（度）")]
    public int sectorGizmoAngle;
    
}

/// <summary>矩形选择参数（格坐标）。运行时：起点为鼠标格，长边沿施法者→鼠标方向。</summary>
[Serializable]
public class RangeRectParam : AbilityAffectRangeParam
{
    [Tooltip("沿方向边长（格）")]
    public int rectGizmoLength;

    [Tooltip("垂直于方向边长（格）")]
    public int rectGizmoWidth;

}

/// <summary>圆形（格）：中心由运行时上下文给出，仅半径参与判定。</summary>
[Serializable]
public class RangeCircleParam : AbilityAffectRangeParam
{
    [Header("圆形（Grid / 切比雪夫 Dist）")]
    [Tooltip("包含 Dist ≤ radius 的格（与 WorldExtensions.Dist 一致）")]
    public int radius;

}

[Serializable]
public class RangePointParam : AbilityAffectRangeParam
{
    [Tooltip("为 true 时用 layerMask 判定格是否可选取（与 TileSelector 一致）；否则仅校验落在施法范围内")]
    public bool requireMoveableInLayer = true;
}

public static class AbilityAffectRangeParamPreview
{
    /// <summary>完整形状格点（含施法者格、原点格），供 AOE 等判定；与 <see cref="EnumeratePreviewCells"/> 几何一致。</summary>
    public static List<Vector3> EnumerateShapeCells(AbilityAffectRangeParam param, Vector3 previewOriginGrid,
        Vector3 skillFaceDirection)
    {
        if (param == null)
            return null;

        var origin = previewOriginGrid.Round();
        var face = skillFaceDirection;
        face.y = 0f;
        if (param is not RangePointParam && face.sqrMagnitude < 1e-8f)
            return null;

        switch (param)
        {
            case RangeCircleParam c:
            {
                if (c.radius <= 0)
                    return new List<Vector3>();

                var cells = new List<Vector3>();
                var r = c.radius;
                for (var x = (int)origin.x - r; x <= (int)origin.x + r; x++)
                for (var y = (int)origin.y - r; y <= (int)origin.y + r; y++)
                {
                    var p = new Vector3(x, 0, y);
                    if (origin.Dist(p) <= c.radius)
                        cells.Add(p);
                }

                return cells;
            }
            case RangeSectorParam s:
                return origin.GridSectorPoints(face, s.sectorGizmoAngle, s.sectorGizmoRadius);
            case RangeRectParam r:
                return origin.GridRectPoints(face, r.rectGizmoLength, r.rectGizmoWidth);
            case RangePointParam:
            {
                var p = origin;
                return new List<Vector3> { p };
            }
            default:
                return new List<Vector3>();
        }
    }

    /// <param name="previewOriginGrid">扇形/矩形起点或圆心（施法范围内沿玩家→目标最远格）。</param>
    /// <param name="skillFaceDirection">技能朝向（玩家格→目标格，2D方向）。</param>
    /// <returns>未配置时返回 null；否则返回格点（<see cref="RangePointParam"/> 不剔除施法者格；其它形状去掉施法者格与预览原点格上的高亮）。</returns>
    public static List<Vector3> EnumeratePreviewCells(AbilityAffectRangeParam param, Vector3 ownerGrid, Vector3 previewOriginGrid,
        Vector3 skillFaceDirection)
    {
        var cells = EnumerateShapeCells(param, previewOriginGrid, skillFaceDirection);
        if (cells == null)
            return null;

        if (param is RangePointParam)
            return cells;

        var owner = ownerGrid.Round();
        var origin = previewOriginGrid.Round();
        RemovePreviewExcludedGrids(cells, owner, origin);
        return cells;
    }
    

    static void RemovePreviewExcludedGrids(List<Vector3> cells, Vector3 ownerGrid, Vector3 mouseGrid)
    {
        if (cells == null || cells.Count == 0)
            return;

        var o = ownerGrid.Round();
        var m = mouseGrid.Round();
        cells.RemoveAll(p =>
        {
            var q = p.Round();
            return WorldExtensions.SameGridCell(q, o) || WorldExtensions.SameGridCell(q, m);
        });
    }
}
