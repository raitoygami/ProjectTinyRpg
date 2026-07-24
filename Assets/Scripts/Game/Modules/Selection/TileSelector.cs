using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class TileSelector : Singleton<TileSelector>
{
    private Transform m_MarkParent;

    public struct VertexDescription
    {
        public float3 position;
        public Color vertexColor;
        public float2 texCoord0;
    }

    private GameObject m_Navigation;
    private GameObject m_SkillPreview;
    
    private NavigationSettings m_Settings;
    private GameObject m_MarkEnd;

    public void Setup(NavigationSettings t_Settings)
    {
        m_MarkParent = new GameObject("Mark Parent").transform;
        m_MarkEnd = Instantiate(t_Settings.NavigationMarkEnd, m_MarkParent, true);
        m_MarkEnd.SetActive(false);
        m_MarkParent.SetParent(transform);
        m_MarkParent.transform.position = Vector3.zero;
        m_Settings = t_Settings;
    }

    public void Hide()
    {
        if (m_Navigation != null)
        {
            m_Navigation.SetActive(false);
        }

        if (m_SkillPreview != null)
        {
            m_SkillPreview.SetActive(false);
        }
    }

    public void HidePath()
    {
        m_MarkParent.gameObject.SetActive(false);
        m_MarkEnd.SetActive(false);
        ClearPath();
    }

    public void ShowPath()
    {
        m_MarkParent.gameObject.SetActive(true);
    }

    public void DrawPath(Vector3 t_GridPosition, bool t_moveable)
    {
        var spriteRenderer = m_MarkEnd.GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = t_moveable ? 0 : 1000;
        spriteRenderer.color = t_moveable ? Color.green : Color.red;
        
        m_MarkEnd.transform.position = t_GridPosition.GridToWorld();
        m_MarkEnd.transform.localScale = Vector3.one * 0.25f;
        m_MarkEnd.transform.DOScale(Vector3.one, 0.2f).SetTarget(m_MarkEnd);
        m_MarkEnd.SetActive(true);
    }

    public void ClearPath()
    {
        m_MarkEnd.transform.DOKill(false);
        m_MarkEnd.SetActive(false);
    }

    /// <summary>按 Ability 配置的 <see cref="SelectParam"/> 高亮技能范围；起点与朝向由 <see cref="Ability.TryGetSkillPreviewFrame"/> 得到。</summary>
    public void ShowSkillRangePreview(SelectParam param, Vector3 ownerGrid, Vector3 previewOriginGrid,
        Vector3 skillFaceDirection, LayerMask t_LayerMask)
    {
        if (param == null)
        {
            HideSkillRangePreview();
            return;
        }

        var cells = SelectParamPreview.EnumeratePreviewCells(param, ownerGrid, previewOriginGrid,
            skillFaceDirection);
        if (cells == null || cells.Count == 0)
        {
            HideSkillRangePreview();
            return;
        }

        if (m_SkillPreview == null)
        {
            m_SkillPreview = new GameObject("Skill Range Preview");
            m_SkillPreview.AddComponent<MeshFilter>();
            m_SkillPreview.AddComponent<MeshRenderer>();
            m_SkillPreview.GetComponent<MeshRenderer>().material = m_Settings.SkillMaterial;
            m_SkillPreview.transform.position = new Vector3(0, 0.11f, -0.1f);
        }

        var nodes = new Dictionary<Vector2Int, PathCell>();
        foreach (var g in cells)
        {
            var x = Mathf.RoundToInt(g.x);
            var z = Mathf.RoundToInt(g.z);
            var target = PathFinder.Instance.GetNode(x, z);
            if (param is SelectPointParam pointParam)
            {
                if (pointParam.requireMoveableInLayer)
                {
                    if (!IsMoveableForMask(target, null))
                        continue;
                }
                else if (target == null)
                {
                    continue;
                }
            }
            else if (!IsMoveableForMask(target, null))
            {
                continue;
            }

            var local = new Vector2Int(x, z);
            if (!nodes.ContainsKey(local))
                nodes.Add(local, target);
        }

        if (nodes.Count == 0)
        {
            HideSkillRangePreview();
            return;
        }

        var mesh = Generate(nodes, 1, WorldExtensions.WorldToGridScale);
        if (m_SkillPreview.GetComponent<MeshFilter>().mesh != null)
        {
            Destroy(m_SkillPreview.GetComponent<MeshFilter>().mesh);
        }

        m_SkillPreview.GetComponent<MeshFilter>().mesh = mesh;
        m_SkillPreview.SetActive(true);
    }

    public void HideSkillRangePreview()
    {
        if (m_SkillPreview != null)
        {
            m_SkillPreview.SetActive(false);
        }
    }

    private static bool IsMoveableForMask(PathCell cell, IPathNodeAgent owner)
    {
        return cell != null && PathFinder.IsWalkableCell(cell, owner);
    }

    public List<Vector2Int> Select(int range, IPathNodeAgent owner, bool t_ShowRange = true)
    {
        var Ret = new List<Vector2Int>();
        if (owner == null)
            return Ret;

        var sx = owner.X;
        var sy = owner.Y;

        if (t_ShowRange && m_Navigation == null)
        {
            m_Navigation = new GameObject("Navigation Scope");
            m_Navigation.AddComponent<MeshFilter>();
            m_Navigation.AddComponent<MeshRenderer>();
            m_Navigation.GetComponent<MeshRenderer>().material = m_Settings.NavigationMaterial;
            m_Navigation.transform.position = new Vector3(0, 0.1f, -0.1f);
        }

        var nodes = new Dictionary<Vector2Int, PathCell>();

        var xMin = sx - range;
        var xMax = sx + range;
        var yMin = sy - range;
        var yMax = sy + range;
        for (var x = xMin; x <= xMax; x++)
        {
            for (var y = yMin; y <= yMax; y++)
            {
                var target = PathFinder.Instance.GetNode(x, y);
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
                Ret.Add(local);
            }
        }

        if (!t_ShowRange) return Ret;

        var mesh = Generate(nodes, 1, WorldExtensions.WorldToGridScale);
        if (m_Navigation.GetComponent<MeshFilter>().mesh != null)
        {
            Destroy(m_Navigation.GetComponent<MeshFilter>().mesh);
        }

        m_Navigation.GetComponent<MeshFilter>().mesh = mesh;
        m_Navigation.SetActive(true);

        return Ret;
    }


    private static Mesh Generate(Dictionary<Vector2Int, PathCell> grids, float EdgeWidth, float EdgeHeight)
    {
        var mesh = new Mesh
        {
            name = "Navigation Selector"
        };

        var meshDataArray = Mesh.AllocateWritableMeshData(1);
        var meshData = meshDataArray[0];

        SetupMesh(meshData, grids.Count * 9, grids.Count * 24);

        var vertices = meshData.GetVertexData<VertexDescription>();
        var triangleIndices = meshData.GetIndexData<uint>();

        ProduralVerticesIndices(vertices, triangleIndices, grids, EdgeWidth, EdgeHeight);

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.OptimizeReorderVertexBuffer();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static void SetupMesh(
        Mesh.MeshData meshData, int vertexCount, int indexCount
    )
    {
        var descriptor = new NativeArray<VertexAttributeDescriptor>(
            3, Allocator.Temp,
            NativeArrayOptions.UninitializedMemory
        );
        descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
        descriptor[1] = new VertexAttributeDescriptor(
            VertexAttribute.Color, VertexAttributeFormat.Float32, 4
        );
        descriptor[2] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension: 2
        );

        meshData.SetVertexBufferParams(vertexCount, descriptor);
        descriptor.Dispose();

        meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
        meshData.subMeshCount = 1;

        if (vertexCount > 0)
        {
            meshData.SetSubMesh(
                0, new SubMeshDescriptor(0, indexCount)
                {
                    vertexCount = vertexCount
                }
                , MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices
            );
        }
    }

    //   => Z+
    // X
    // X 0       1
    // + 3       2
    private static readonly Vector2[] _Offsets =
    {
        new(0, 0),
        new(0, 0.5f),
        new(0, 1),
        new(0.5f, 1),
        new(1, 1),
        new(1, 0.5f),
        new(1, 0),
        new(0.5f, 0),
        new(0.5f, 0.5f),
    };

    private static readonly Vector2[] _UVs =
    {
        new(0, 0),
        new(0, 0.5f),
        new(0, 1),
        new(0.5f, 1),
        new(1, 1),
        new(1, 0.5f),
        new(1, 0),
        new(0.5f, 0),
        new(0.5f, 0.5f),
    };

    private static void ProduralVerticesIndices(NativeArray<VertexDescription> vertices,
        NativeArray<uint> triangleIndices,
        Dictionary<Vector2Int, PathCell> Grids, float EdgeWidth, float EdgeHeight)
    {
        int gridCount = 0;
        int triangleIndex = 0;

        foreach (var (key, value) in Grids)
        {
            var originPosition = new Vector3(value.X, 0, value.Y).GridToWorld();

            for (var vertexIndex = 0; vertexIndex < 9; vertexIndex++)
            {
                var offset = _Offsets[vertexIndex] - Vector2.one * 0.5f;
                var vertex = new VertexDescription
                {
                    position = new float3(
                        originPosition.x + offset.x * EdgeWidth,
                        originPosition.y + offset.y * EdgeHeight,
                        originPosition.z
                    ),
                    // texCoord0 = _UVs[vertexIndex], //GetVertexTexcoord(value, vertexIndex, Grids),
                    vertexColor = GetVertexColor(value, vertexIndex, Grids),
                };

                vertices[gridCount * 9 + vertexIndex] = vertex;
            }

            int a = gridCount * 9;
            int b = gridCount * 9 + 1;
            int c = gridCount * 9 + 2;
            int d = gridCount * 9 + 3;
            int e = gridCount * 9 + 4;
            int f = gridCount * 9 + 5;
            int g = gridCount * 9 + 6;
            int h = gridCount * 9 + 7;
            int i = gridCount * 9 + 8;


            triangleIndices[triangleIndex++] = (ushort)a;
            triangleIndices[triangleIndex++] = (ushort)b;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)b;
            triangleIndices[triangleIndex++] = (ushort)c;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)c;
            triangleIndices[triangleIndex++] = (ushort)d;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)d;
            triangleIndices[triangleIndex++] = (ushort)e;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)e;
            triangleIndices[triangleIndex++] = (ushort)f;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)f;
            triangleIndices[triangleIndex++] = (ushort)g;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)g;
            triangleIndices[triangleIndex++] = (ushort)h;
            triangleIndices[triangleIndex++] = (ushort)i;

            triangleIndices[triangleIndex++] = (ushort)h;
            triangleIndices[triangleIndex++] = (ushort)a;
            triangleIndices[triangleIndex++] = (ushort)i;
            gridCount++;
        }
    }

    private static readonly Dictionary<int, List<Vector2Int>> NeighborOffset = new()
    {
        { 0, new List<Vector2Int> { new(0, -1), new(-1, -1), new(-1, 0) } },
        { 1, new List<Vector2Int> { new(-1, 0), new(-1, 1), new(0, 1) } },
        { 2, new List<Vector2Int> { new(0, 1), new(1, 1), new(1, 0) } },
        { 3, new List<Vector2Int> { new(1, 0), new(1, -1), new(0, -1) } },
    };

    //  00     01 
    //
    //  10     11
    // 0,1,2,3
    private static Color GetVertexColor(PathCell grid, int vertexIndex, Dictionary<Vector2Int, PathCell> t_Grids)
    {
        if (vertexIndex == 8)
        {
            return Color.black;
        }

        int Index = vertexIndex / 2;
        var isCorner = vertexIndex % 2 == 0;
        var location = new Vector2Int(grid.X, grid.Y);

        if (isCorner)
        {
            var offsets = NeighborOffset[Index];
            if (offsets.Any(offset => !t_Grids.ContainsKey(location + offset)))
            {
                return Color.white;
            }
        }
        else
        {
            var offsets = NeighborOffset[Index];
            if (!t_Grids.ContainsKey(location + offsets[2]))
            {
                return Color.white;
            }
        }

        return Color.black;
    }

    private void OnDestroy()
    {
        if (m_Navigation != null)
        {
            Destroy(m_Navigation);
        }

        m_Navigation = null;
    }
}