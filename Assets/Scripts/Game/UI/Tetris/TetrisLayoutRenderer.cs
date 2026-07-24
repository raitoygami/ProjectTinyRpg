using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>
///     无子节点、无 <see cref="GridLayoutGroup" />：在单个 <see cref="MaskableGraphic" /> 内按
///     <see cref="cellSize" />、<see cref="LayoutWidth" />、<see cref="LayoutHeight" /> 合并绘制格子，
///     布局算法与 Unity <see cref="GridLayoutGroup" />（子锚点左上 (0,1) + inset）一致。
///     各格 sprite 需共用同一 Texture 才能正确采样（图集合批）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public class TetrisLayoutRenderer : MaskableGraphic
{
    [SerializeField] private RectOffset padding = new();
    [SerializeField] private Vector2 cellSize = new(100f, 100f);
    [SerializeField] private Vector2 spacing = Vector2.zero;
    [SerializeField] private GridLayoutGroup.Corner startCorner = GridLayoutGroup.Corner.UpperLeft;
    [SerializeField] private GridLayoutGroup.Axis startAxis = GridLayoutGroup.Axis.Horizontal;
    [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;
    [SerializeField] private GridLayoutGroup.Constraint constraint = GridLayoutGroup.Constraint.Flexible;
    [SerializeField] private int constraintCount = 2;

    [Tooltip("未单独指定 sprite 的格子使用该贴图")] [SerializeField]
    private Sprite defaultCellSprite;

    private Color[,] _cellTint;
    private bool[,] _previewActive;
    private Color _previewTint = Color.white;
    private readonly List<int> _previewedIndices = new();

    public int LayoutWidth { get; private set; }
    public int LayoutHeight { get; private set; }
    public Vector2 CellSize => cellSize;

    public RectOffset Padding => padding;
    public Vector2 Spacing => spacing;
    public GridLayoutGroup.Corner StartCorner => startCorner;
    public GridLayoutGroup.Axis StartAxis => startAxis;
    public TextAnchor ChildAlignment => childAlignment;
    public GridLayoutGroup.Constraint Constraint => constraint;
    public int ConstraintCount => constraintCount;

    public override Texture mainTexture
    {
        get
        {
            if (LayoutWidth <= 0 || LayoutHeight <= 0)
                return Texture2D.whiteTexture;

            return defaultCellSprite != null ? defaultCellSprite.texture : Texture2D.whiteTexture;
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || LayoutWidth <= 0) return;
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (isActiveAndEnabled && LayoutWidth > 0 && LayoutHeight > 0)
            SetVerticesDirty();
    }
#endif

    public void Init(int width, int height, ITetrisLayoutOwner _)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        constraintCount = width;

        LayoutWidth = width;
        LayoutHeight = height;

        _cellTint = new Color[width, height];
        _previewActive = new bool[width, height];
        for (var c = 0; c < width; c++)
        for (var r = 0; r < height; r++)
        {
            _cellTint[c, r] = Color.white;
            _previewActive[c, r] = false;
        }

        _previewedIndices.Clear();

        ApplyGridSizeToRectTransform();
        Canvas.ForceUpdateCanvases();
        SetVerticesDirty();
    }

    /// <summary>
    ///     由列数、行数与 <see cref="cellSize" /> / <see cref="spacing" /> / <see cref="padding" /> 计算网格外接尺寸
    ///     （与 <see cref="GridLayoutGroup" /> 的 preferred 尺寸一致），并写入当前 <see cref="RectTransform" />。
    ///     使用 <see cref="RectTransform.SetSizeWithCurrentAnchors" />，可随锚点（居中、拉伸等）正确更新宽高。
    /// </summary>
    private void ApplyGridSizeToRectTransform()
    {
        if (LayoutWidth <= 0 || LayoutHeight <= 0) return;

        var bounds = ComputeGridOuterSize(LayoutWidth, LayoutHeight, cellSize, spacing, padding);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bounds.y);
    }

    /// <summary>内容区域宽高：padding + 格子与间距（与 GridLayoutGroup 计算方式一致）。</summary>
    public static Vector2 ComputeGridOuterSize(int columns, int rows, Vector2 cell, Vector2 gap, RectOffset pad)
    {
        var w = (float) pad.horizontal;
        if (columns > 0)
            w += columns * cell.x + Mathf.Max(0, columns - 1) * gap.x;

        var h = (float) pad.vertical;
        if (rows > 0)
            h += rows * cell.y + Mathf.Max(0, rows - 1) * gap.y;

        return new Vector2(w, h);
    }

    /// <summary>持久修改某一格的顶点乘色（与 <see cref="Preview" /> 独立，不受 ClearPreview 影响）。</summary>
    public void SetCellColor(int col, int row, Color tint)
    {
        if (_cellTint == null || col < 0 || col >= LayoutWidth || row < 0 || row >= LayoutHeight)
            return;
        _cellTint[col, row] = tint;
        SetVerticesDirty();
    }

    public Color GetCellColor(int col, int row)
    {
        if (_cellTint == null || col < 0 || col >= LayoutWidth || row < 0 || row >= LayoutHeight)
            return Color.white;
        return _cellTint[col, row];
    }

    public Sprite GetCellSprite(int col, int row)
    {
        return defaultCellSprite;
    }

    /// <summary>格子在自身 <see cref="RectTransform" /> 本地空间中的矩形（与顶点绘制一致），便于换算世界坐标。</summary>
    public bool TryGetCellLocalRect(int col, int row, out Rect localRect)
    {
        localRect = default;
        if (LayoutWidth <= 0 || LayoutHeight <= 0) return false;
        if (col < 0 || col >= LayoutWidth || row < 0 || row >= LayoutHeight) return false;
        var index = row * LayoutWidth + col;
        return TryComputeCellLocalRect(index, LayoutWidth * LayoutHeight, rectTransform.rect, out localRect);
    }

    /// <summary>
    ///     将指定格子四角的世界坐标写入数组，顺序与 <see cref="RectTransform.GetWorldCorners" /> 相同：<br />
    ///     0 左下，1 左上，2 右上，3 右下。
    /// </summary>
    /// <param name="fourCornersArray">长度至少为 4。</param>
    public void GetWorldCorners(int col, int row, Vector3[] fourCornersArray)
    {
        if (fourCornersArray == null || fourCornersArray.Length < 4)
        {
            Debug.LogError(
                $"{nameof(TetrisLayoutRenderer)}.{nameof(GetWorldCorners)} requires an array of length at least 4.");
            return;
        }

        if (!TryGetCellLocalRect(col, row, out var local))
        {
            for (var i = 0; i < 4; i++)
                fourCornersArray[i] = Vector3.zero;
            return;
        }

        var rt = rectTransform;
        fourCornersArray[0] = rt.TransformPoint(new Vector3(local.xMin, local.yMin, 0f));
        fourCornersArray[1] = rt.TransformPoint(new Vector3(local.xMin, local.yMax, 0f));
        fourCornersArray[2] = rt.TransformPoint(new Vector3(local.xMax, local.yMax, 0f));
        fourCornersArray[3] = rt.TransformPoint(new Vector3(local.xMax, local.yMin, 0f));
    }

    public void Preview(int pivotCol, int pivotRow, int itemWidthCells, int itemHeightCells, Color previewColor)
    {
        ClearPreviewFlagsFromTrackedList();
        if (_previewActive == null || _cellTint == null) return;

        _previewTint = previewColor;

        for (var r = 0; r < itemHeightCells; r++)
        for (var c = 0; c < itemWidthCells; c++)
        {
            var col = pivotCol + c;
            var row = pivotRow + r;

            if (col < 0 || col >= LayoutWidth || row < 0 || row >= LayoutHeight)
                continue;

            var index = row * LayoutWidth + col;
            _previewActive[col, row] = true;
            _previewedIndices.Add(index);
        }

        SetVerticesDirty();
    }

    public void ClearPreview()
    {
        ClearPreviewFlagsFromTrackedList();
        SetVerticesDirty();
    }

    private void ClearPreviewFlagsFromTrackedList()
    {
        if (_previewActive == null)
        {
            _previewedIndices.Clear();
            return;
        }

        foreach (var index in _previewedIndices)
        {
            var col = index % LayoutWidth;
            var row = index / LayoutWidth;
            if (col >= LayoutWidth || row >= LayoutHeight) continue;
            _previewActive[col, row] = false;
        }

        _previewedIndices.Clear();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (LayoutWidth <= 0 || LayoutHeight <= 0) return;

        var total = LayoutWidth * LayoutHeight;
        var parentRect = rectTransform.rect;

        for (var i = 0; i < total; i++)
        {
            if (!TryComputeCellLocalRect(i, total, parentRect, out var cellRect))
                continue;

            var col = i % LayoutWidth;
            var row = i / LayoutWidth;

            var sprite = GetCellSprite(col, row);
            if (sprite == null) continue;

            var v0 = new Vector3(cellRect.xMin, cellRect.yMin, 0f);
            var v1 = new Vector3(cellRect.xMin, cellRect.yMax, 0f);
            var v2 = new Vector3(cellRect.xMax, cellRect.yMax, 0f);
            var v3 = new Vector3(cellRect.xMax, cellRect.yMin, 0f);

            var uv = DataUtility.GetOuterUV(sprite);
            var tint = _cellTint != null ? _cellTint[col, row] : Color.white;
            var previewMul = _previewActive != null && _previewActive[col, row] ? _previewTint : Color.white;
            var vertColor = (Color32) (color * tint * previewMul);

            var vi = vh.currentVertCount;
            var vert = UIVertex.simpleVert;
            vert.color = vertColor;
            vert.position = v0;
            vert.uv0 = new Vector2(uv.x, uv.y);
            vh.AddVert(vert);
            vert.position = v1;
            vert.uv0 = new Vector2(uv.x, uv.w);
            vh.AddVert(vert);
            vert.position = v2;
            vert.uv0 = new Vector2(uv.z, uv.w);
            vh.AddVert(vert);
            vert.position = v3;
            vert.uv0 = new Vector2(uv.z, uv.y);
            vh.AddVert(vert);

            vh.AddTriangle(vi, vi + 1, vi + 2);
            vh.AddTriangle(vi + 2, vi + 3, vi);
        }
    }

    /// <summary>与 <see cref="GridLayoutGroup" /> 中子 Rect（锚点左上）一致的本地矩形。</summary>
    private bool TryComputeCellLocalRect(int cellIndex, int count, Rect rect, out Rect localRect)
    {
        localRect = default;
        if (cellIndex < 0 || cellIndex >= count) return false;

        var width = rect.size.x;
        var height = rect.size.y;

        int cellCountX;
        int cellCountY;
        if (constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            cellCountX = constraintCount;
            cellCountY = Mathf.CeilToInt(count / (float) cellCountX - 0.001f);
        }
        else if (constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            cellCountY = constraintCount;
            cellCountX = Mathf.CeilToInt(count / (float) cellCountY - 0.001f);
        }
        else
        {
            cellCountX = Mathf.Max(1,
                Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            cellCountY = Mathf.Max(1,
                Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
        }

        var cornerX = (int) startCorner % 2;
        var cornerY = (int) startCorner / 2;

        int cellsPerMainAxis, actualCellCountX, actualCellCountY;
        if (startAxis == GridLayoutGroup.Axis.Horizontal)
        {
            cellsPerMainAxis = cellCountX;
            actualCellCountX = Mathf.Clamp(cellCountX, 1, count);
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(count / (float) cellsPerMainAxis));
        }
        else
        {
            cellsPerMainAxis = cellCountY;
            actualCellCountY = Mathf.Clamp(cellCountY, 1, count);
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(count / (float) cellsPerMainAxis));
        }

        var requiredSpace = new Vector2(
            actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
            actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
        );
        var startOffset = new Vector2(
            GetStartOffset(0, requiredSpace.x),
            GetStartOffset(1, requiredSpace.y)
        );

        int positionX;
        int positionY;
        if (startAxis == GridLayoutGroup.Axis.Horizontal)
        {
            positionX = cellIndex % cellsPerMainAxis;
            positionY = cellIndex / cellsPerMainAxis;
        }
        else
        {
            positionX = cellIndex / cellsPerMainAxis;
            positionY = cellIndex % cellsPerMainAxis;
        }

        if (cornerX == 1)
            positionX = actualCellCountX - 1 - positionX;
        if (cornerY == 1)
            positionY = actualCellCountY - 1 - positionY;

        var insetLeft = startOffset.x + (cellSize.x + spacing.x) * positionX;
        var insetTop = startOffset.y + (cellSize.y + spacing.y) * positionY;

        var left = rect.xMin + insetLeft;
        var right = left + cellSize.x;
        var top = rect.yMax - insetTop;
        var bottom = top - cellSize.y;

        localRect = Rect.MinMaxRect(left, bottom, right, top);
        return true;
    }

    private float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
    {
        var rect = rectTransform.rect;
        var requiredSpace = requiredSpaceWithoutPadding +
                            (axis == 0 ? padding.horizontal : padding.vertical);
        var availableSpace = axis == 0 ? rect.width : rect.height;
        var surplusSpace = availableSpace - requiredSpace;
        var alignmentOnAxis = axis == 0
            ? (int) childAlignment % 3 * 0.5f
            : (int) childAlignment / 3 * 0.5f;
        return (axis == 0 ? padding.left : padding.top) + surplusSpace * alignmentOnAxis;
    }
}