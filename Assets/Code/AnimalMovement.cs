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
    public class AnimalMovement : MonoBehaviour, ITransformable
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

        public bool IsCloseToTarget(Vector3 target)
        {
            return Mathf.Abs(transform.position.z - target.z) <= _sizeEffectY &&
                   Mathf.Abs(transform.position.x - target.x) <= 1f;
        }

        private List<PathNode> FindPath(Vector3 target)
        {
            int x = Mathf.RoundToInt(target.x);
            int z = Mathf.RoundToInt(target.z);

            Vector2Int[] points =
            {
                new Vector2Int(x, z - _sizeEffectY),
                new Vector2Int(x - 1, z - _sizeEffectY),
                new Vector2Int(x + 1, z - _sizeEffectY)
            };

            for (int i = 0; i < points.Length; i++)
            {
                List<PathNode> path =
                    _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, points[i].x, points[i].y, _sizeType);

                if (path != null)
                {
                    Debug.Log($"{gameObject.name} - - {path.Count}");
                    return path;
                }
            }

            return null;
        }

        public AnimalMovement CurrentTarget;

        public async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            List<PathNode> path = FindPath(target);

            if (path == null || path.Count == 0) return;

            if (path.Count > _tilesPerMove + 1)
                path = path.Take(_tilesPerMove + 1).ToList();

            foreach (PathNode pathNode in path)
            {
                if (_sizeType == ObjectSizeType.Medium)
                {
                    Debug.LogWarning(pathNode.name);
                }
            }
            
            ClearNodes();
            Vector3[] pathPositions = GetPathPositions(path);

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;

                    Vector3 direction = GetDirection(pathPositions[i]);
                    RotateToTarget(-direction);
                })
                .OnUpdate(() => _animator.UpdateMovementAnimation(1f)).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RotateToTarget(Vector3.forward);
                    _animator.UpdateMovementAnimation(0f);
                });

            await tween.AsyncWaitForCompletion();

            _currentPathNode = path.Last();

            List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

            _currentPathNode.IsWalkable = false;
            _nodes = neighbours;

            if (IsCloseToTarget(target))
            {
                RotateToTarget(target - transform.position);
                await reachedTargetCallback?.Invoke()!;
            }
        }

        private void RotateToTarget(Vector3 target)
        {
            if (target != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(target);
            }
        }

        private Vector3[] GetPathPositions(List<PathNode> path)
        {
            Vector3[] pathPositions = path.Select(node => node.WorldPosition).ToArray();

            for (int i = 0; i < pathPositions.Length; i++)
            {
                pathPositions[i].y = _yPos;
                pathPositions[i] += GetMovementOffset() + Offset;
            }

            return pathPositions;
        }
        private Vector3 GetDirection(Vector3 target)
        {
            Vector3 position = transform.position;
            Vector3 direction = (target - position).normalized;
            // if (Math.Abs(target.z - position.z) < 0.1f)
            //     direction = -(position - target).normalized;
            // else
            //     direction = (target - position).normalized;

            return direction;
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

        public Vector3 Position => transform.position;

        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
    }
}