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
        [SerializeField] private int _sizeEffectY;

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfinder _pathfinder;
        private Grid _grid;

        public PathNode _currentPathNode;

        public ObjectSizeType ObjectSizeType => _sizeType;

        public PathNode CurrentPathNode => _currentPathNode;

        public Vector3 Position => transform.position;

        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        public Vector3 Direction => _direction;

        public AnimalMovement CurrentTarget;
        [SerializeField] private Vector3 _direction = Vector3.forward;


        [Inject]
        private void Construct(IPathfinder pathfinder, Grid grid)
        {
            _pathfinder = pathfinder;
            _grid = grid;
        }

        private void Start()
        {
            RotateToTarget(_direction);
        }

        public void Place(Vector3 position)
        {
            position.y = _yPos;
            transform.position = position + Utilities.GetMovementOffset(_sizeType, _direction) + Offset;
        }

        private void Place(Vector3 position, Vector3 offset)
        {
            position.y = _yPos;
            transform.position = position + offset + Offset;
        }

        public void ClearNodes()
        {
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

                if (pathNode.CanPlace && pathNode.IsWalkable && pathNode.HasNeighbours(_sizeType, _direction))
                {
                    SetNewNode(pathNode);
                    return true;
                }

                PathNode lowerNeighbour = _grid.GetGridObject(pathNode.x, pathNode.y - 1);

                if (lowerNeighbour != null)
                {
                    if (lowerNeighbour.CanPlace && pathNode.IsWalkable &&
                        lowerNeighbour.HasNeighbours(_sizeType, _direction))
                    {
                        SetNewNode(lowerNeighbour);
                        return true;
                    }
                }


                PathNode leftNeighbour = _grid.GetGridObject(pathNode.x - 1, pathNode.y);

                if (leftNeighbour != null)
                {
                    if (leftNeighbour.CanPlace && pathNode.IsWalkable &&
                        leftNeighbour.HasNeighbours(_sizeType, _direction))
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

            Place(pathNode.WorldPosition, Utilities.GetMovementOffset(pathNode, _sizeType, _direction));
            List<PathNode> neighbours = pathNode.GetNeighbours(_sizeType, _direction);
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
            int sizeEffect = GetSizeOffset();

            // int sizeEffect = ObjectSizeType == ObjectSizeType.Medium || ObjectSizeType == ObjectSizeType.Big
            //     ? _sizeEffectY
            //     : CurrentTarget._sizeEffectY;
            //
            // if (CurrentTarget.ObjectSizeType == ObjectSizeType.Big && ObjectSizeType == ObjectSizeType.Medium)
            // {
            //     sizeEffect = _sizeEffectY + 1;
            // }
            //
            // if (ObjectSizeType == ObjectSizeType.Big  && CurrentTarget.ObjectSizeType == ObjectSizeType.Medium)
            // {
            //     sizeEffect = _sizeEffectY + 1;
            // }
            //
            // if (ObjectSizeType == ObjectSizeType.Big && CurrentTarget.ObjectSizeType == ObjectSizeType.Small)
            // {
            //     sizeEffect = _sizeEffectY;
            // }
            
            return Mathf.Abs(_currentPathNode.y - target.z) <= sizeEffect &&
                   Mathf.Abs(_currentPathNode.x - target.x) <= 1f;
        }

        private List<PathNode> FindPath(Vector2Int[] points)
        {
            ClearNodes();

            for (int i = 0; i < points.Length; i++)
            {
                List<PathNode> path =
                    _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, points[i].x, points[i].y, _sizeType,
                        _direction);

                if (path != null) return path;
            }

            return null;
        }

        public void Shift()
        {
            Vector2Int[] possibleMoves =
            {
                new Vector2Int(_currentPathNode.x + 1, _currentPathNode.y),
                new Vector2Int(_currentPathNode.x - 1, _currentPathNode.y),
                new Vector2Int(_currentPathNode.x, _currentPathNode.y - 1),
            };

            List<PathNode> path = FindPath(possibleMoves);

            if (path == null) return;
            
            _animator.JumpAnimation();

            Vector3[] pathPositions = GetPathPositions(path);

            transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;

                    Vector3 direction = GetDirection(pathPositions[i]);
                    RotateToTarget(-direction);
                })
                .OnComplete(() => RotateToTarget(Vector3.forward));

            _currentPathNode.IsWalkable = true;
            _currentPathNode = path.LastOrDefault();
            _currentPathNode.IsWalkable = false;
        }


        public async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            Vector2Int[] points = GetPossibleMoves(target);

            List<PathNode> path = FindPath(points);

            if (path == null || path.Count == 0) return;

            Debug.LogError($"Me - {name}, last node - {path.Last().name}");

            if (path.Count > _tilesPerMove + 1)
                path = path.Take(_tilesPerMove + 1).ToList();


            Vector3[] pathPositions = GetPathPositions(path);

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;

                    Vector3 direction = (transform.position - pathPositions[i]);
                    //Vector3 direction = Utilities.GetDirection(transform.position, pathPositions[i]);
                    RotateToTarget(direction);
                })
                .OnUpdate(() => _animator.UpdateMovementAnimation(1f)).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RotateToTarget(_direction);
                    _animator.UpdateMovementAnimation(0f);
                });

            await tween.AsyncWaitForCompletion();

            _currentPathNode = path.Last();

            List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType, _direction);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

            _currentPathNode.IsWalkable = false;
            _nodes = neighbours;

            if (IsCloseToTarget(target))
            {
                RotateToTarget(target - transform.position);
                //RotateToTarget(target - transform.position);
                await reachedTargetCallback?.Invoke()!;
            }
        }

        private Vector2Int[] GetPossibleMoves(Vector3 target)
        {
            int x = Mathf.RoundToInt(target.x);
            int z = Mathf.RoundToInt(target.z);

            int sizeEffect = GetSizeOffset();
            
            // int sizeEffect = ObjectSizeType == ObjectSizeType.Medium
            //     ? _sizeEffectY
            //     : CurrentTarget._sizeEffectY;
            //
            // if (CurrentTarget.ObjectSizeType == ObjectSizeType.Big && ObjectSizeType == ObjectSizeType.Medium)
            // {
            //     sizeEffect = _sizeEffectY + 1;
            // }
            //
            // if (ObjectSizeType == ObjectSizeType.Big  && CurrentTarget.ObjectSizeType == ObjectSizeType.Medium)
            // {
            //     sizeEffect = _sizeEffectY + 1;
            // }
            //
            // if (ObjectSizeType == ObjectSizeType.Big && CurrentTarget.ObjectSizeType == ObjectSizeType.Small)
            // {
            //     sizeEffect = _sizeEffectY;
            // }
            //
            int zOffset = target.z > _currentPathNode.y ? z - sizeEffect : z + sizeEffect;

            Vector2Int[] points =
            {
                new Vector2Int(x, zOffset),
                new Vector2Int(x - 1, zOffset),
                new Vector2Int(x + 1, zOffset)
            };
            return points;
        }

        public void RotateToTarget(Vector3 target)
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
                pathPositions[i] += Utilities.GetMovementOffset(_sizeType, _direction) + Offset;
            }

            return pathPositions;
        }

        private Vector3 GetDirection(Vector3 target)
        {
            Vector3 position = transform.position;
            Vector3 direction = (target - position).normalized;

            return direction;
        }

        private int GetSizeOffset()
        {
            switch (_sizeType)
            {
                case ObjectSizeType.Small:
                    return CurrentTarget._sizeEffectY;
                case ObjectSizeType.Medium:
                    switch (CurrentTarget.ObjectSizeType)
                    {
                        case ObjectSizeType.Small:
                            return _sizeEffectY;
                        case ObjectSizeType.Medium:
                            return _sizeEffectY;
                        case ObjectSizeType.Big:
                            return  _sizeEffectY + 1;
                    }
                    break;
                case ObjectSizeType.Big:
                    switch (CurrentTarget.ObjectSizeType)
                    {
                        case ObjectSizeType.Small:
                            return _sizeEffectY;
                        case ObjectSizeType.Medium:
                            return _sizeEffectY + 1;
                        case ObjectSizeType.Big:
                            return _sizeEffectY + 1;
                    }
                    break;
            }

            return -1;
        }
    }
}