using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

internal static class Extensions
{
    public static UniTask<bool> PublishGlobal<TEventArgs>(this MonoBehaviour owner, TEventArgs eventArgs)
        where TEventArgs : EventArgs
    {
        return Context.Instance.Messager.Publish(eventArgs);
    }

    public static Action SubscribeGlobal<TEventArgs>(this MonoBehaviour owner, Func<TEventArgs, UniTask> handler) where TEventArgs : EventArgs
    {
        if (!Context.HasInstance()) return null;

        var action = Context.Instance.Messager.Subscribe(handler);
        owner.destroyCancellationToken.Register(action);
        return action;
    }

    public static void UnSubscribeGlobal<TEventArgs>(this MonoBehaviour owner, Func<TEventArgs, UniTask> handler) where TEventArgs : EventArgs
    {
        Context.Instance.Messager.Unsubscribe(handler);
    }

    public static Action Subscribe<TEventArgs>(this MonoBehaviour owner, Func<TEventArgs, UniTask> handler) where TEventArgs : EventArgs
    {
        var pubsubActor = owner.GetComponent<PubSubActor>();
        var action = pubsubActor.Messager.Subscribe(handler);
        owner.destroyCancellationToken.Register(action);
        return action;
    }

    public static bool HasSubscription<TEventArgs>(this MonoBehaviour owner) where TEventArgs : EventArgs
    {
        if (!owner)
            return false;
        var pubsubActor = owner.GetComponent<PubSubActor>();
        return pubsubActor && pubsubActor.Messager.HasSubscription<TEventArgs>();
    }

    public static UniTask<bool> Publish<TEventArgs>(this MonoBehaviour owner, TEventArgs args, bool sequential = false)
        where TEventArgs : EventArgs
    {
        var pubsubActor = owner.GetComponent<PubSubActor>();
        return pubsubActor.Messager.Publish(args, sequential);
    }

    public static Action SubscribeInput<TEventArgs>(this MonoBehaviour owner, Func<TEventArgs, UniTask> handler) where TEventArgs : EventArgs
    {
        if (!InputManager.HasInstance()) return null;

        var action = InputManager.Instance.Messager.Subscribe(handler);
        owner.destroyCancellationToken.Register(action);
        return action;
    }

}

internal static class WorldExtensions
{
    /// <summary>Grid-to-world unit scale factor (plane-agnostic).</summary>
    public static readonly float WorldToGridScale = 1;

    /// <inheritdoc cref="WorldToGridScale"/>
    [Obsolete("Use WorldToGridScale instead.")]
    public static readonly float WorldToGridZ = WorldToGridScale;

    // ── Core plane abstraction (single point of truth) ──────────────────

    /// <summary>
    /// Extract 2D plane coordinates from a world-space Vector3.
    /// Change ONLY this method (and <see cref="ToWorldPosition(Vector2,float)"/>) to switch planes.
    /// </summary>
    public static Vector2 GetCoordinates(this Vector3 self)
    {
        return new Vector2(self.x, self.y);
    }

    /// <summary>
    /// Construct world-space Vector3 from 2D plane coordinates.
    /// Change ONLY this method (and <see cref="GetCoordinates"/>) to switch planes.
    /// </summary>
    public static Vector3 ToWorldPosition(this Vector2 self, float depth = 0f)
    {
        return new Vector3(self.x, self.y, depth);
    }

    /// <inheritdoc cref="ToWorldPosition(Vector2,float)"/>
    public static Vector3 ToWorldPosition(this Vector2Int self, float depth = 0f)
    {
        return new Vector3(self.x, self.y, depth);
    }

    // ── Grid conversion (uses core abstraction internally) ──────────────

    /// <summary>Snap world-space Transform position to grid coordinates.</summary>
    /// <returns>Grid-container Vector3: (gridX, 0, gridY_as_z).</returns>
    public static Vector3 SnapToGrid(this Transform self)
    {
        var c = self.position.GetCoordinates();
        return new Vector3(Mathf.Round(c.x), Mathf.Round(c.y / WorldToGridScale), 0 );
    }

    /// <inheritdoc cref="SnapToGrid(Transform)"/>
    public static Vector3 SnapToGrid(this Vector3 self)
    {
        var c = self.GetCoordinates();
        return new Vector3(Mathf.Round(c.x), Mathf.Round(c.y / WorldToGridScale), 0);
    }

    /// <summary>Convert Vector2 to grid-container Vector3.</summary>
    public static Vector3 SnapToGrid(this Vector2 self)
    {
        return new Vector3(Mathf.Round(self.x), Mathf.Round(self.y / WorldToGridScale), 0);
    }

    /// <summary>Snap world position to grid, returning pure Vector2Int grid coordinates.</summary>
    public static Vector2Int SnapToGrid2D(this Vector3 self)
    {
        var c = self.GetCoordinates();
        return new Vector2Int(Mathf.RoundToInt(c.x), Mathf.RoundToInt(c.y / WorldToGridScale));
    }

    /// <inheritdoc cref="SnapToGrid2D(Vector3)"/>
    public static Vector2Int SnapToGrid2D(this Transform self)
    {
        return self.position.SnapToGrid2D();
    }

    /// <summary>Convert grid-container Vector3 (gridX, 0, gridZ) to world-space position.</summary>
    public static Vector3 GridToWorld(this Vector3 self)
    {
        var c = self.GetCoordinates();
        return new Vector3(c.x, c.y * WorldToGridScale, 0f);
    }

    /// <summary>Convert Vector2Int grid coordinates to world-space position.</summary>
    public static Vector3 GridToWorld(this Vector2Int self, float depth = 0f)
    {
        return new Vector3(self.x, self.y * WorldToGridScale, depth);
    }

    /// <summary>Convert Vector2 grid coordinates to world-space position.</summary>
    public static Vector3 GridToWorld(this Vector2 self, float depth = 0f)
    {
        return new Vector3(self.x, self.y * WorldToGridScale, depth);
    }

    // ── Distance / comparison (operates on grid-container format) ───────

    /// <summary>Manhattan (grid) distance between two grid-container positions.</summary>
    public static int Dist(this Vector3 self, Vector3 target)
    {
        var dx = Math.Abs(self.x - target.x);
        var dz = Math.Abs(self.z - target.z);
        return Mathf.RoundToInt(dx + dz);
    }

    /// <summary>Manhattan distance between two Vector2Int grid positions.</summary>
    public static int Dist(this Vector2Int self, Vector2Int target)
    {
        return Mathf.Abs(self.x - target.x) + Mathf.Abs(self.y - target.y);
    }

    /// <summary>Euclidean distance between two grid-container positions.</summary>
    public static float DistRadius(this Vector3 self, Vector3 target)
    {
        var dx = Math.Abs(self.x - target.x);
        var dz = Math.Abs(self.z - target.z);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>2D delta vector from fromGrid to toGrid (grid-container inputs).</summary>
    public static Vector2 GridDelta(Vector3 fromGrid, Vector3 toGrid)
    {
        return new Vector2(toGrid.x - fromGrid.x, toGrid.z - fromGrid.z);
    }

    /// <inheritdoc cref="GridDelta"/>
    [Obsolete("Use GridDelta which returns Vector2.")]
    public static Vector3 GridDeltaXZ(Vector3 fromGrid, Vector3 toGrid)
    {
        var d = GridDelta(fromGrid, toGrid);
        return new Vector3(d.x, 0f, d.y);
    }

    /// <summary>Are two grid-container positions in the same grid cell?</summary>
    public static bool SameGridCell(Vector3 a, Vector3 b)
    {
        a = a.Round();
        b = b.Round();
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.z, b.z);
    }

    /// <inheritdoc cref="SameGridCell(Vector3,Vector3)"/>
    public static bool SameGridCell(Vector2Int a, Vector2Int b)
    {
        return a == b;
    }

    // ── Grid math ───────────────────────────────────────────────────────

    /// <summary>Linear interpolation between two grid-container positions (rounds result).</summary>
    public static Vector3 Lerp(this Vector3 self, Vector3 t, float n)
    {
        var ret = self;
        ret.x += Mathf.Round((t.x - ret.x) * n);
        ret.z += Mathf.Round((t.z - ret.z) * n);
        return ret;
    }

    /// <summary>Bresenham-like line between two grid-container positions.</summary>
    public static List<Vector3> LineTo(this Vector3 self, Vector3 t)
    {
        var ret = new List<Vector3>();
        var dist = self.Dist(t);
        var step = 1f / Mathf.Max(dist, 1f);
        for (var i = 0; i <= dist; i++)
        {
            var newTile = self.Lerp(t, step * i);
            ret.Add(newTile);
        }
        return ret;
    }

    /// <summary>Round grid-container Vector3 (rounds .x and .z).</summary>
    public static Vector3 Round(this Vector3 self)
    {
        var ret = self;
        ret.x = Mathf.Round(self.x);
        ret.z = Mathf.Round(self.z);
        return ret;
    }

    /// <summary>Round a Vector2.</summary>
    public static Vector2 Round(this Vector2 self)
    {
        return new Vector2(Mathf.Round(self.x), Mathf.Round(self.y));
    }

    // ── Shape generators (grid space) ───────────────────────────────────

    /// <summary>
    /// Fan-shaped area of grid cells on the 2D plane.
    /// </summary>
    /// <param name="start">Grid-container origin.</param>
    /// <param name="direction">2D direction (grid-container format: .x = col, .z = row).</param>
    /// <param name="angleDegrees">Total fan angle in degrees.</param>
    /// <param name="radius">Chebyshev radius in grid units.</param>
    public static List<Vector3> GridSectorPoints(this Vector3 start, Vector3 direction, float angleDegrees, int radius)
    {
        return GridSectorPoints(start, new Vector2(direction.x, direction.z), angleDegrees, radius);
    }

    /// <inheritdoc cref="GridSectorPoints(Vector3,Vector3,float,int)"/>
    public static List<Vector3> GridSectorPoints(this Vector3 start, Vector2 direction, float angleDegrees, int radius)
    {
        var result = new List<Vector3>();
        if (radius < 0)
            return result;

        var origin = start.Round();
        var halfAngle = angleDegrees * 0.5f;

        if (direction.sqrMagnitude < 1e-8f)
        {
            for (var dx = -radius; dx <= radius; dx++)
            for (var dy = -radius; dy <= radius; dy++)
            {
                var point = new Vector3(origin.x + dx, origin.y, origin.z + dy);
                if (origin.DistRadius(point) > radius)
                    continue;
                result.Add(point);
            }
            return result;
        }

        var dir = direction.normalized;

        for (var dx = -radius; dx <= radius; dx++)
        for (var dy = -radius; dy <= radius; dy++)
        {
            var point = new Vector3(origin.x + dx, origin.y, origin.z + dy);
            if (origin.DistRadius(point) > radius)
                continue;

            var toPoint = new Vector2(dx, dy);
            if (toPoint.sqrMagnitude < 1e-8f)
            {
                result.Add(origin);
                continue;
            }

            if (Vector2.Angle(dir, toPoint) <= halfAngle + 1e-4f)
                result.Add(point);
        }

        return result;
    }

    /// <summary>
    /// OBB rectangle area of grid cells on the 2D plane.
    /// </summary>
    /// <param name="start">Grid-container origin (long-edge start, centered on width).</param>
    /// <param name="direction">2D direction (grid-container format: .x = col, .z = row).</param>
    /// <param name="length">Length along direction in grid units.</param>
    /// <param name="width">Width perpendicular to direction.</param>
    public static List<Vector3> GridRectPoints(this Vector3 start, Vector3 direction, int length, int width)
    {
        return GridRectPoints(start, new Vector2(direction.x, direction.z), length, width);
    }

    /// <inheritdoc cref="GridRectPoints(Vector3,Vector3,int,int)"/>
    public static List<Vector3> GridRectPoints(this Vector3 start, Vector2 direction, int length, int width)
    {
        var result = new List<Vector3>();
        if (length <= 0 || width <= 0)
            return result;

        var origin = start.Round();
        if (direction.sqrMagnitude < 1e-8f)
            return result;

        var f = direction.normalized;
        var p = new Vector2(-f.y, f.x);
        var len = (float)length;
        var halfW = width * 0.5f;
        var bound = length + width;

        for (var dx = -bound; dx <= bound; dx++)
        for (var dy = -bound; dy <= bound; dy++)
        {
            var u = f.x * dx + f.y * dy;
            var v = p.x * dx + p.y * dy;
            if (u <= 0f || u > len || v < -halfW || v > halfW)
                continue;

            result.Add(new Vector3(origin.x + dx, origin.y, origin.z + dy));
        }

        return result;
    }

    // ── Gizmos ──────────────────────────────────────────────────────────

    /// <summary>
    /// Draw a wire-cube gizmo representing a leash square on the 2D plane.
    /// </summary>
    public static void DrawLeashSquareGizmo(Vector3 originCellCenterWorld, int chebyshevRadius, Color color)
    {
        if (chebyshevRadius <= 0) return;
        Gizmos.color = color;
        var halfH = chebyshevRadius;
        var halfV = chebyshevRadius * WorldToGridScale;
        Gizmos.DrawWireCube(originCellCenterWorld, new Vector3(2f * halfH, 2f * halfV, 0.02f));
    }
}
