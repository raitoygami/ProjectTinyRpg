using System.Collections.Generic;
using UnityEngine;


public static class SpriteMeshGenerator
{
    /// <summary>
    /// 基于 Sprite 的 triangles、uv、vertices 直接创建 Mesh，不修改几何。
    /// </summary>
    public static Mesh CreateMeshFromSpriteGeometry(Sprite sprite)
    {
        if (sprite == null) return null;
        Vector2[] v2 = sprite.vertices;
        ushort[] tri = sprite.triangles;
        Vector2[] uv2 = sprite.uv;
        if (v2 == null || v2.Length == 0 || tri == null || tri.Length == 0)
            return null;
        if (uv2 == null || uv2.Length != v2.Length)
            uv2 = null;

        var mesh = new Mesh { name = sprite.name + "_Mesh" };
        var v3 = new Vector3[v2.Length];
        for (var i = 0; i < v2.Length; i++)
            v3[i] = new Vector3(v2[i].x, v2[i].y, 0f);
        mesh.SetVertices(v3);
        if (uv2 != null)
            mesh.SetUVs(0, uv2);
        var triInt = new int[tri.Length];
        for (var i = 0; i < tri.Length; i++)
            triInt[i] = tri[i];
        mesh.SetTriangles(triInt, 0);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }
 
}
