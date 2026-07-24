using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>选择区域参数类型，与 <see cref="SelectParam"/> 派生类一一对应。</summary>
public enum SelectParamKind
{
    Sector,
    Rect,
    /// <summary>以施法点为中心，切比雪夫 Dist ≤ <see cref="SelectCircleParam.radius"/>。</summary>
    Circle,
    /// <summary>施法范围内沿玩家→目标方向最远一格为唯一目标点（与 <see cref="Ability.TryGetSkillPreviewFrame"/> 一致）。</summary>
    Point,
}

/// <summary>选择区域参数基类。</summary>
[Serializable]
public abstract class SelectParam
{
    [SerializeField] private SelectParamKind _kind;

    public SelectParamKind Kind => _kind;

    protected SelectParam(SelectParamKind kind)
    {
        _kind = kind;
    }
}

/// <summary>扇形选择参数（格坐标）。运行时：顶点为鼠标格，对称轴为施法者→鼠标。</summary>
[Serializable]
public class SelectSectorParam : SelectParam
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

    public SelectSectorParam() : base(SelectParamKind.Sector)
    {
    }
}

/// <summary>矩形选择参数（格坐标）。运行时：起点为鼠标格，长边沿施法者→鼠标方向。</summary>
[Serializable]
public class SelectRectParam : SelectParam
{
    [Header("矩形（Grid 坐标）")]
    [Tooltip("起点参考（格）；运行时起点为鼠标格")]
    public Vector3 rectGizmoStartPos;

    [Tooltip("2D 朝向（施法者→鼠标，编辑器参考）")]
    public Vector3 rectGizmoDirection;
    
    [Tooltip("沿方向边长（格）")]
    public int rectGizmoLength;

    [Tooltip("垂直于方向边长（格）")]
    public int rectGizmoWidth;

    public SelectRectParam() : base(SelectParamKind.Rect)
    {
    }
}

/// <summary>圆形（格）：中心由运行时上下文给出，仅半径参与判定。</summary>
[Serializable]
public class SelectCircleParam : SelectParam
{
    [Header("圆形（Grid / 切比雪夫 Dist）")]
    [Tooltip("包含 Dist ≤ radius 的格（与 WorldExtensions.Dist 一致）")]
    public int radius;

    public SelectCircleParam() : base(SelectParamKind.Circle)
    {
    }
}

/// <summary>单格点（格坐标）。运行时目标格与 <see cref="Ability.TryGetSkillPreviewFrame"/> 的 <c>previewOrigin</c> 相同，且须在 <see cref="Ability.SelectionRange"/> 内；可选要求该格在 <see cref="layerMask"/> 下可选取。</summary>
[Serializable]
public class SelectPointParam : SelectParam
{
    [Tooltip("为 true 时用 layerMask 判定格是否可选取（与 TileSelector 一致）；否则仅校验落在施法范围内")]
    public bool requireMoveableInLayer = true;

    [Tooltip("requireMoveableInLayer 为 true 时生效；为 0 时不按层屏蔽（仅 NavigationNode.IsMoveabled）")]
    public LayerMask layerMask;

    public SelectPointParam() : base(SelectParamKind.Point)
    {
    }
}

/// <summary>根据 <see cref="SelectParam"/> 生成区域格点（与预览/AOE 共用几何）。</summary>
/// <remarks>
/// 预览原点与朝向由 <see cref="Ability.TryGetSkillPreviewFrame"/> 计算；
/// 与 Ability 技能展示、<see cref="E_AOE"/> 等使用相同 <see cref="SelectParam"/> 配置时，传入相同 frame 则范围一致。
/// </remarks>
public static class SelectParamPreview
{
    /// <summary>完整形状格点（含施法者格、原点格），供 AOE 等判定；与 <see cref="EnumeratePreviewCells"/> 几何一致。</summary>
    public static List<Vector3> EnumerateShapeCells(SelectParam param, Vector3 previewOriginGrid,
        Vector3 skillFaceDirection)
    {
        if (param == null)
            return null;

        var origin = previewOriginGrid.Round();
        var face = skillFaceDirection;
        face.y = 0f;
        if (param is not SelectPointParam && face.sqrMagnitude < 1e-8f)
            return null;

        switch (param)
        {
            case SelectCircleParam c:
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
            case SelectSectorParam s:
                return origin.GridSectorPoints(face, s.sectorGizmoAngle, s.sectorGizmoRadius);
            case SelectRectParam r:
                return origin.GridRectPoints(face, r.rectGizmoLength, r.rectGizmoWidth);
            case SelectPointParam:
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
    /// <returns>未配置时返回 null；否则返回格点（<see cref="SelectPointParam"/> 不剔除施法者格；其它形状去掉施法者格与预览原点格上的高亮）。</returns>
    public static List<Vector3> EnumeratePreviewCells(SelectParam param, Vector3 ownerGrid, Vector3 previewOriginGrid,
        Vector3 skillFaceDirection)
    {
        var cells = EnumerateShapeCells(param, previewOriginGrid, skillFaceDirection);
        if (cells == null)
            return null;

        if (param is SelectPointParam)
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
