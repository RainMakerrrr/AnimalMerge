using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Animals;
using Code.Pathfinding;
using DG.Tweening;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code
{
    public class AnimalMovement : MonoBehaviour
    {
        private const string NodeLayerName = "PathNode";

        [SerializeField] private float _yPos;
        [SerializeField] private float _zOffset;
        [SerializeField] private ObjectSizeType _sizeType;
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private List<PathNode> _nodes = new List<PathNode>();
        [SerializeField] private float _raycastOffset = 0.4f;
        [SerializeField] private int _tilesPerMove = 2;

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfinder _pathfinder;
        private Grid _grid;

        private PathNode _currentPathNode;
        [SerializeField] private int _sizeEffectX;
        [SerializeField] private int _sizeEffectY;

        public ObjectSizeType ObjectSizeType => _sizeType;

        public PathNode CurrentPathNode => _currentPathNode;

        public bool IsReachedTarget { get; private set; } = false;


        [Inject]
        private void Construct(IPathfinder pathfinder, Grid grid)
        {
            _pathfinder = pathfinder;
            _grid = grid;
        }

        public void Place(Vector3 position)
        {
            position.y = _yPos;
            transform.position = position + GetMovementOffset() + Offset;
        }

        private void Place(Vector3 position, Vector3 offset)
        {
            position.y = _yPos;
            transform.position = position + offset + Offset;
        }

        public void ClearNodes()
        {
            if (_currentPathNode != null)
                _currentPathNode.IsWalkable = true;

            _nodes.ForEach(node => node.IsWalkable = true);
            _nodes.Clear();
        }

        public void SetCurrentNode(PathNode node)
        {
            _currentPathNode = node;
        }

        public void FillNodes(List<PathNode> nodes)
        {
            _nodes = nodes;
        }

        public bool TryPlace()
        {
            if (Physics.Raycast(transform.position + new Vector3(0f, 0f, _raycastOffset), Vector3.down,
                    out RaycastHit hit, 20f,
                    LayerMask.GetMask(NodeLayerName)))
            {
                var pathNode = hit.collider.GetComponent<PathNode>();

                Debug.Log(pathNode.name);

                if (pathNode.CanPlace && pathNode.IsWalkable && pathNode.HasNeighbours(_sizeType))
                {
                    SetNewNode(pathNode);
                    return true;
                }

                PathNode lowerNeighbour = _grid.GetGridObject(pathNode.x, pathNode.y - 1);

                if (lowerNeighbour != null)
                {
                    if (lowerNeighbour.CanPlace && pathNode.IsWalkable && lowerNeighbour.HasNeighbours(_sizeType))
                    {
                        SetNewNode(lowerNeighbour);
                        return true;
                    }
                }


                PathNode leftNeighbour = _grid.GetGridObject(pathNode.x - 1, pathNode.y);

                if (leftNeighbour != null)
                {
                    if (leftNeighbour.CanPlace && pathNode.IsWalkable && leftNeighbour.HasNeighbours(_sizeType))
                    {
                        SetNewNode(leftNeighbour);
                        return true;
                    }
                }


                return false;
            }

            return false;
        }

        private void RotateToTarget(Vector3 target)
        {
            if (target != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(target);
        }

        private void SetNewNode(PathNode pathNode)
        {
            _nodes.Clear();

            Place(pathNode.WorldPosition, GetMovementOffset(pathNode));
            List<PathNode> neighbours = pathNode.GetNeighbours(_sizeType);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

            _currentPathNode = pathNode;
            _currentPathNode.IsWalkable = false;
            _nodes = neighbours;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + new Vector3(0f, 0f, _raycastOffset), Vector3.down);
        }

        public bool CanMove(Vector3 target)
        {
            Vector2Int targetPoint =
                new Vector2Int(Mathf.RoundToInt(target.x), Mathf.RoundToInt(target.z) - _sizeEffectY);

            ClearNodes();

            List<PathNode> path =
                _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, targetPoint.x, targetPoint.y,
                    _sizeType);

            if (path == null) return false;

            if (path.Count > 3)
                path.RemoveRange(_tilesPerMove + 1, path.Count - _tilesPerMove - 1);

            return path.Count <= 1;
        }

        public async Task Move(Vector3 target)
        {
            Vector2Int targetPoint =
                new Vector2Int(Mathf.RoundToInt(target.x), Mathf.RoundToInt(target.z) - _sizeEffectY);

            ClearNodes();

            List<PathNode> path =
                _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, targetPoint.x, targetPoint.y,
                    _sizeType);

            if (path == null) return;

            if (path.Count > 3)
                path.RemoveRange(_tilesPerMove + 1, path.Count - _tilesPerMove - 1);

            IsReachedTarget = path.Count <= 1;

            Debug.Log($"Path count {path.Count}");

            Vector3[] pathPositions = path.Select(node => node.WorldPosition).ToArray();

            for (int i = 0; i < pathPositions.Length; i++)
            {
                pathPositions[i].y = _yPos;
                pathPositions[i] += GetMovementOffset() + Offset;
            }

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange((i) =>
                {
                    Vector3 direction = (pathPositions[i] - transform.position).normalized;
                    RotateToTarget(-direction);
                })
                .OnUpdate(() => _animator.UpdateMovementAnimation(1f)).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RotateToTarget(Vector3.forward);
                    _animator.UpdateMovementAnimation(0f);
                });

            await tween.AsyncWaitForCompletion();
            //yield return tween.WaitForCompletion();

            ClearNodes();

            _currentPathNode = path.Last();

            List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

            _currentPathNode.IsWalkable = false;
            _nodes = neighbours;
        }

        public async Task Move()
        {
            var targetPoint = new Vector2Int(_currentPathNode.x, _currentPathNode.y + _tilesPerMove);

            List<PathNode> path =
                _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, targetPoint.x, targetPoint.y,
                    _sizeType);

            if (path == null) return;

            Vector3[] pathPositions = path.Select(node => node.WorldPosition).ToArray();

            for (int i = 0; i < pathPositions.Length; i++)
            {
                pathPositions[i].y = _yPos;
                pathPositions[i] += GetMovementOffset() + Offset;
            }

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange((i) =>
                {
                    Vector3 direction = (pathPositions[i] - transform.position).normalized;
                    RotateToTarget(-direction);
                })
                .OnUpdate(() => _animator.UpdateMovementAnimation(1f)).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RotateToTarget(Vector3.forward);
                    _animator.UpdateMovementAnimation(0f);
                });

            await tween.AsyncWaitForCompletion();
            //yield return tween.WaitForCompletion();

            _currentPathNode.IsWalkable = true;
            _currentPathNode = path.Last();

            ClearNodes();

            List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

            _currentPathNode.IsWalkable = false;
            _nodes = neighbours;
        }

        private Vector3 GetMovementOffset()
        {
            switch (_sizeType)
            {
                case ObjectSizeType.Small:
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    return new Vector3(0f, 0f, 0.5f);
                case ObjectSizeType.Big:
                    return new Vector3(0.5f, 0f, 0.5f);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Vector3 GetMovementOffset(PathNode node)
        {
            switch (_sizeType)
            {
                case ObjectSizeType.Small:
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    return new Vector3(0f, 0f, 0.5f);
                case ObjectSizeType.Big:
                    return node.x == 7 ? new Vector3(-0.5f, 0f, 0.5f) : new Vector3(0.5f, 0f, 0.5f);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public interface ITransformable
    {
        Vector3 Position { get; }
        Vector2Int IntPosition { get; }
    }
}