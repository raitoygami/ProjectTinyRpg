using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Rendering;

public class AgentMover : MonoBehaviour
{
    public class MoveStartEvent : EventArgs
    {
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public bool Forced;

        public float Duration;
    }

    public class MoveFinishEvent : EventArgs
    {
        public Vector3 LastPosition;
        public Vector3 CurrPosition;
    }

    public class MoveForcedFinishEvent : EventArgs
    {
        public Vector3 LastPosition;
        public Vector3 CurrPosition;
    }

    private readonly MoveStartEvent _moveStartEvent = new();
    private readonly MoveForcedFinishEvent _forcedMoveFinishEvent = new();
    private readonly MoveFinishEvent _moveFinishEvt = new();
    private bool _IsMoving;
    private bool _MovePending;
    private Vector3 _pendingParallelMoveWorldPosition;

    private void Awake()
    {
        _IsMoving = false;
    }


    private TweenerCore<Vector3, Vector3, VectorOptions> _tweenMove;
    public async UniTask<bool> Move(Vector3 gridPosition, bool forced = false, float velocityMulti = 1.0f,
        Ease moveEase = Ease.Linear)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();
        if (_IsMoving)
            try {
                await UniTask.WaitUntil(() => !_IsMoving, cancellationToken: destroyToken);
            }
            catch (Exception)
            {
                return true;
            }

        _IsMoving = true;

        var worldPosition = gridPosition.GridToWorld();
        var duration = transform.SnapToGrid().Dist(gridPosition) / Mathf.Max(velocityMulti, 1.0f) * 0.25f;
        
        _moveStartEvent.StartPosition = transform.position;
        _moveStartEvent.TargetPosition = worldPosition;
        _moveStartEvent.Forced = forced;
        _moveStartEvent.Duration = duration;
        
        await this.Publish(_moveStartEvent);

        try
        {
            _tweenMove = transform.DOMove(worldPosition, duration).SetEase(moveEase);
            await _tweenMove.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, destroyToken);

            _IsMoving = false;
            _moveFinishEvt.LastPosition = transform.position;
            _moveFinishEvt.CurrPosition = worldPosition;
        }
        
        catch (Exception)
        {
            return true;
        }

        if (!forced) return await this.Publish(_moveFinishEvt);
        
        _forcedMoveFinishEvent.LastPosition = transform.position;
        _forcedMoveFinishEvent.CurrPosition = worldPosition;
        return await this.Publish(_forcedMoveFinishEvent);

    }

    public bool IsMoving()
    {
        return _IsMoving;
    }

    public bool Moveable(PathNode target)
    {
        if (target == null) return false;

        var mover = GetComponent<IPathNodeAgent>();
        if (mover == null) return false;

        var anchorX = target.X;
        var anchorY = target.Y;
        var sizeX = Mathf.Max(1, mover.GridSizeX);
        var sizeY = Mathf.Max(1, mover.GridSizeY);

        // 检查自身 footprint 放在目标锚点后占据的所有格子
        for (var ox = 0; ox < sizeX; ox++)
        for (var oy = 0; oy < sizeY; oy++)
        {
            var checkX = anchorX + ox;
            var checkY = anchorY + oy;

            var cell = PathFinder.Instance.GetCell(checkX, checkY);

            if (cell == null)
                return false;

            // 空地可以停留
            if (cell.Logical == null)
                continue;

            // 自身不阻挡自身
            if (ReferenceEquals(cell.Logical, mover))
                continue;
            // 【核心逻辑】只要有任何一格包含 ObstacleForNavi 就不能停留
            if ((Const.Layer.ObstacleForNavi.value & cell.Logical.Layer.value) != 0)
                return false;
        }

        return true;
    }
    
    public const int  ChunkBound = 20;

    public bool ShouldTransitionChunk(PathNode target, out Vector2Int chunkIndex, out Vector3Int spawnLocation)
    {
        var mapName = PlayerManager.Instance.GetCurrentMap();
        var mapInfo = MapManager.Instance.GetMapInfo(mapName);
        chunkIndex = mapInfo.ChunkIndex;
        spawnLocation = new Vector3Int(target.X, target.Y, 0);
        if (mapInfo.MapType == MapConfig.MapType.Dungeon)
            return false;

        // chunk内部
        if (Mathf.Abs(target.X) < ChunkBound && Mathf.Abs(target.Y) < ChunkBound)
            return false;
        // chunk边界
        if (Mathf.Abs(target.X) == ChunkBound)
        {
            var dir = (target.X > 0) ? 1 : -1;
            chunkIndex.x += dir;
            spawnLocation.x = -dir * (ChunkBound - 1);
        }
        
        if (Mathf.Abs(target.Y) == ChunkBound)
        {
            var dir = (target.Y > 0) ? 1 : -1;
            chunkIndex.y += dir;
            spawnLocation.y = -dir * (ChunkBound - 1);
        }
        var chunkInfo = MapManager.Instance.GetChunkInfo(chunkIndex);
        return chunkInfo != null;
    }
    
    public bool Moveable(Vector3 gridPosition)
    {
        var entity = GetComponent<Entity>();
        var tx = (int)gridPosition.x;
        var ty = (int)gridPosition.y;
        var cell = PathFinder.Instance.GetCell(tx, ty);
        if (!PathFinder.IsWalkableCell(cell, entity, tx, ty))
            return false;

        var source = transform.position.SnapToGrid();
        var navi = PathFinder.Instance.Navigate(entity, (int)source.x, (int)source.y, tx, ty);

        return navi is { Count: 1 };
    }

    public List<PathNode> FindPath(Vector3 destination, int range = -1)
    {
        if (!PathFinder.HasInstance())
            return null;
        var sx = (int)GetComponent<Entity>().GridPosition.x;
        var sy = (int)GetComponent<Entity>().GridPosition.y;
        var dx = (int)destination.x;
        var dy = (int)destination.y;
        try
        {
            var pathBuffer = new List<PathNode>();
            var nav = PathFinder.Instance.Navigate(GetComponent<Entity>(), sx, sy, dx, dy, range);
            if (nav == null)
                return null;
            foreach (var step in nav)
                pathBuffer.Add(new PathNode(step.x, step.y, true));
            return pathBuffer;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(transform);
    }

    private void OnDestroy()
    {
        _tweenMove.Kill();
        DOTween.Kill(transform);
    }
}