using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TilemapUI : MaskableGraphic
{
    private Tilemap m_Tilemap;
    private BoundsInt m_Bounds; // 外部传入的统一 bounds
    private float m_TileSizeX = 32f;
    private float m_TileSizeY = 32f;
    private Texture m_MainTexture;

    /// <summary>
    ///     初始化接口（推荐使用此重载）
    /// </summary>
    /// <param name="tilemap">源 Tilemap</param>
    /// <param name="bounds">统一的格子范围（三个 Tilemap 共用同一份）</param>
    /// <param name="tileSizeX">UI 中每个格子宽度</param>
    /// <param name="tileSizeY">UI 中每个格子高度</param>
    public void Initialize(Tilemap tilemap, BoundsInt bounds, float tileSizeX, float tileSizeY)
    {
        m_Tilemap = tilemap;
        m_Bounds = bounds;
        m_TileSizeX = Mathf.Max(0.01f, tileSizeX);
        m_TileSizeY = Mathf.Max(0.01f, tileSizeY);
        m_MainTexture = null;

        // 使用统一 bounds 计算 UI 尺寸
        rectTransform.sizeDelta = new Vector2(
            m_Bounds.size.x * m_TileSizeX,
            m_Bounds.size.y * m_TileSizeY
        );

        SetAllDirty();
    }

    /// <summary>
    ///     手动刷新（Tilemap 内容变化后调用）
    /// </summary>
    public void Refresh()
    {
        m_MainTexture = null;
        SetVerticesDirty();
        SetMaterialDirty();
    }

    public override Texture mainTexture
    {
        get
        {
            if (m_MainTexture != null)
                return m_MainTexture;
            return s_WhiteTexture;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (m_Tilemap == null || m_TileSizeX <= 0f || m_TileSizeY <= 0f)
            return;

        if (m_Bounds.size.x <= 0 || m_Bounds.size.y <= 0)
            return;

        var rect = GetPixelAdjustedRect();
        // 把地图左下角对齐到 Rect 的左下角
        var origin = new Vector2(rect.xMin, rect.yMin);

        Color32 graphicColor = color;
        Texture firstTexture = null;
        var textureMismatchWarned = false;

        // 使用外部传入的统一 bounds 进行遍历
        for (var x = m_Bounds.xMin; x < m_Bounds.xMax; x++)
        for (var y = m_Bounds.yMin; y < m_Bounds.yMax; y++)
        {
            var cellPos = new Vector3Int(x, y, 0);
            var sprite = m_Tilemap.GetSprite(cellPos);
            if (sprite == null)
                continue;

            Texture tex = sprite.texture;
            if (firstTexture == null)
            {
                firstTexture = tex;
                m_MainTexture = tex;
            }
            else if (tex != firstTexture)
            {
                if (!textureMismatchWarned)
                {
                    Debug.LogWarning(
                        "[TilemapUI] 检测到不同 Texture 的 Sprite，已跳过后续不同纹理的 Tile。建议使用 Sprite Atlas。", this);
                    textureMismatchWarned = true;
                }

                continue;
            }

            // UV
            var outerUV = DataUtility.GetOuterUV(sprite);

            // 格子在 UI 中的位置（相对统一 bounds 的左下角）
            var localX = (x - m_Bounds.xMin) * m_TileSizeX;
            var localY = (y - m_Bounds.yMin) * m_TileSizeY;

            var bottomLeft = origin + new Vector2(localX, localY);
            var topRight = bottomLeft + new Vector2(m_TileSizeX, m_TileSizeY);

            // 合并 Tile 自身颜色
            var tileColor = m_Tilemap.GetColor(cellPos);
            Color32 finalColor = graphicColor * tileColor;

            AddQuad(vh, bottomLeft, topRight, outerUV, finalColor);
        }
    }

    private static void AddQuad(VertexHelper vh, Vector2 bottomLeft, Vector2 topRight, Vector4 uv, Color32 color)
    {
        var startIndex = vh.currentVertCount;

        // 顶点顺序：BL → TL → TR → BR
        vh.AddVert(new Vector3(bottomLeft.x, bottomLeft.y), color, new Vector2(uv.x, uv.y));
        vh.AddVert(new Vector3(bottomLeft.x, topRight.y), color, new Vector2(uv.x, uv.w));
        vh.AddVert(new Vector3(topRight.x, topRight.y), color, new Vector2(uv.z, uv.w));
        vh.AddVert(new Vector3(topRight.x, bottomLeft.y), color, new Vector2(uv.z, uv.y));

        vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (m_Tilemap != null)
            SetAllDirty();
    }
#endif
}