/*
using UnityEngine;

/// <summary>
/// Debug 用：在 Scene 视图用 Gizmos 绘制 AStarPathFinder Grid 每个格子的可移动状态，每帧绘制。
/// 可指定 LayerMask，与寻路时一致地判断“对该 Mask 是否可移动”。
/// </summary>
public class NavigationMonitor : MonoBehaviour
{
    [Tooltip("留空则使用 AStarPathFinder.Instance")]
    public Navigation pathFinder;

    [Tooltip("用于判断格子是否可移动的 LayerMask，与 Navigate 时一致")]
    public LayerMask layerMask = -1;

    [Tooltip("格子立方体边长")]
    public float cubeSize = 0.4f;

    [Tooltip("绘制高度偏移，避免贴地看不清")]
    public float heightOffset = 0.2f;

    private static Vector3 GridToWorld(int x, int z, float depthOffset)
    {
        return new Vector3(x, z * WorldExtensions.WorldToGridScale, depthOffset);
    }

    private static bool IsMoveableForMask(NavigationNode node, LayerMask mask)
    {
        if (node == null || !node.IsMoveabled()) return false;
        return (mask.value & node.Layer.value) == 0;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        var pf = pathFinder != null ? pathFinder : (Navigation.Instance);
        if (pf == null) return;

        var grid = pf.Grid;
        if (grid == null) return;

        int ox = grid.OriginX;
        int oz = grid.OriginZ;
        int w = grid.Width;
        int h = grid.Height;

        for (int ix = 0; ix < w; ix++)
        {
            for (int iz = 0; iz < h; iz++)
            {
                int x = ox + ix;
                int z = oz + iz;
                var node = grid.Get(x, z);
                bool moveable = IsMoveableForMask(node, layerMask);

                Gizmos.color = moveable ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
                Vector3 center = GridToWorld(x, z, heightOffset);
                Gizmos.DrawCube(center, Vector3.one * cubeSize);
            }
        }
    }
}
*/
