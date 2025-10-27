using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Facades;
using Code.Infrastructure.Factories.Animals;
using Code.Pathfinding;
using DG.Tweening;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code.Animals.Movement
{
    public class AnimalMovement : MonoBehaviour, ITransformable
    {
        private const string NodeLayerName = "PathNode";
        private const string GameGridId = "Game Grid";

        [SerializeField] private float _yPos;
        [SerializeField] private float _zOffset;
        [SerializeField] private ObjectSizeType _sizeType;
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private List<PathNode> _nodes = new List<PathNode>();
        [SerializeField] private float _raycastOffset = 0.4f;
        [SerializeField] private int _tilesPerMove = 2;
        [SerializeField] private int _sizeEffectY;
        [SerializeField] private Vector3 _direction = Vector3.forward;
        [SerializeField] private bool _debugDrawPath = true;

        private Vector3[] _debugPathPoints;

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfinder _pathfinder;
        private Grid _grid;

        public PathNode _currentPathNode;

        public ObjectSizeType ObjectSizeType => _sizeType;

        public int SizeEffect => _sizeEffectY;

        public PathNode CurrentPathNode => _currentPathNode;

        public Vector3 Position => transform.position;


        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        public Vector3 Direction => _direction;

        public ITarget CurrentTarget { get; set; }

        public List<PathNode> Nodes => _nodes;

        private IAbility _ability;

        private IAnimalFactory _animalFactory;

        private readonly List<AnimalMovement> _additionalAnimals = new List<AnimalMovement>();

        public void Upgrade(int multiplier) => _tilesPerMove *= multiplier;


        [Inject]
        private void Construct(IPathfinder pathfinder, Grid grid, IAnimalFactory animalFactory)
        {
            _pathfinder = pathfinder;
            _grid = grid;
            _animalFactory = animalFactory;
        }

        public void AddAdditionalAnimalsRange(IEnumerable<AnimalMovement> animals)
        {
            _additionalAnimals.AddRange(animals);
        }

        private void Start()
        {
            RotateToTarget(_direction);
            _ability = new MultipleCharacters(this, _grid, _animalFactory, AnimalType.Chicken, 3);
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
            if (_additionalAnimals.Count > 0)
            {
                _additionalAnimals.ForEach(animal =>
                {
                    animal.CurrentPathNode.IsWalkable = true;
                    animal.CurrentPathNode.UpdateVisual();
                    animal._nodes.ForEach(node => { node.IsWalkable = true; node.UpdateVisual(); });
                    animal._nodes.Clear();
                });
            }

            _nodes.ForEach(node => { node.IsWalkable = true; node.UpdateVisual(); });
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
            if (Physics.Raycast(
                    transform.position + new Vector3(0f, 2f, _raycastOffset),
                    Vector3.down,
                    out RaycastHit hit,
                    Mathf.Infinity,
                    LayerMask.GetMask(NodeLayerName)))
            {
                var raycastable = hit.collider.GetComponent<IRaycastable>();
                
                return raycastable != null && raycastable.Accept(GetComponent<AnimalFacade>());
            }

            return false;
        }

        public void SetNewNode(PathNode pathNode)
        {
            if (_currentPathNode != null)
            {
                _currentPathNode.IsWalkable = true;
                _currentPathNode.UpdateVisual();
            }

            ClearNodes();

            Place(pathNode.WorldPosition, Utilities.GetMovementOffset(pathNode, _sizeType, _direction));
            List<PathNode> neighbours = pathNode.GetNeighbours(_sizeType, _direction);
            neighbours.ForEach(neighbour => { neighbour.IsWalkable = false; neighbour.UpdateVisual(); });

            _currentPathNode = pathNode;
            _currentPathNode.IsWalkable = false;
            _currentPathNode.UpdateVisual();
            _nodes = neighbours;
        }

        public bool IsCloseToTarget(Vector3 target)
        {
            if (_currentPathNode == null) return false;

            int sizeEffect = GetSizeOffset();

            // Compute closeness in the local basis of movement: forward = _direction, right = perpendicular
            Vector2Int current = new Vector2Int(_currentPathNode.x, _currentPathNode.y);
            int targetX = Mathf.RoundToInt(target.x);
            int targetZ = Mathf.RoundToInt(target.z);
            Vector2Int delta = new Vector2Int(targetX - current.x, targetZ - current.y);

            Vector2Int forward = new Vector2Int(Mathf.RoundToInt(_direction.x), Mathf.RoundToInt(_direction.z));
            if (forward == Vector2Int.zero) forward = Vector2Int.up;
            Vector2Int right = new Vector2Int(forward.y, -forward.x);

            int longitudinal = Mathf.Abs(delta.x * forward.x + delta.y * forward.y);
            int lateral = Mathf.Abs(delta.x * right.x + delta.y * right.y);

            return longitudinal <= sizeEffect && lateral <= 1;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + new Vector3(0f, 0f, _raycastOffset), Vector3.down);

            if (_debugDrawPath && _debugPathPoints != null && _debugPathPoints.Length > 1)
            {
                Color prev = Gizmos.color;
                Gizmos.color = Color.green;

                for (int i = 1; i < _debugPathPoints.Length; i++)
                {
                    Gizmos.DrawLine(_debugPathPoints[i - 1], _debugPathPoints[i]);
                }

                Gizmos.color = Color.yellow;
                for (int i = 0; i < _debugPathPoints.Length; i++)
                {
                    Gizmos.DrawSphere(_debugPathPoints[i], 0.05f);
                }

                Gizmos.color = prev;
            }
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

        public async Task Shift()
        {
            Vector2Int[] possibleMoves =
            {
                new Vector2Int(_currentPathNode.x + 1, _currentPathNode.y),
                new Vector2Int(_currentPathNode.x - 1, _currentPathNode.y),
                new Vector2Int(_currentPathNode.x, _currentPathNode.y - 1),
            };

            List<PathNode> path = FindPath(possibleMoves);
            
            if (path == null || path.Count == 0) return;

            _animator.JumpAnimation();

            Vector3[] pathPositions = GetPathPositions(path);
            _debugPathPoints = _debugDrawPath ? pathPositions : null;

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;

                    Vector3 direction = GetDirection(pathPositions[i]);
                    RotateToTarget(-direction);
                })
                .OnComplete(() =>
                {
                    RotateToTarget(Vector3.forward);
                    _debugPathPoints = null;
                });

            if (_currentPathNode != null)
            {
                _currentPathNode.IsWalkable = true;
                _currentPathNode.UpdateVisual();
            }
            _nodes.ForEach(node => { node.IsWalkable = true; node.UpdateVisual(); });
            _nodes.Clear();
            
            _currentPathNode = path.LastOrDefault();
            if (_currentPathNode != null)
            {
                _currentPathNode.IsWalkable = false;
                _currentPathNode.UpdateVisual();
                List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType, _direction);
                neighbours.ForEach(neighbour => { neighbour.IsWalkable = false; neighbour.UpdateVisual(); });
                FillNodes(neighbours);
            }

            await tween.AsyncWaitForCompletion();
        }

        //надо находить врага и просто брать среди их тайлов ближайший ко мне

        public async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            Vector2Int[] points = GetPossibleMoves(target);

            List<PathNode> path = FindPath(points);

            Debug.Log($"[PathFindDebug] is path is null - {path}, count - {path?.Count ?? 0}");
            if (path == null || path.Count == 0) return;

            if (path.Count > _tilesPerMove + 1)
                path = path.Take(_tilesPerMove + 1).ToList();

            Vector3[] pathPositions = GetPathPositions(path);
            _debugPathPoints = _debugDrawPath ? pathPositions : null;

            // Free previous node and neighbours before moving so the tile color reverts to green
            if (_currentPathNode != null)
            {
                _currentPathNode.IsWalkable = true;
                _currentPathNode.UpdateVisual();
            }
            if (_nodes != null && _nodes.Count > 0)
            {
                _nodes.ForEach(node => { node.IsWalkable = true; node.UpdateVisual(); });
                _nodes.Clear();
            }

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;

                    Vector3 direction = (transform.position - pathPositions[i]);
                    RotateToTarget(direction);
                })
                .OnUpdate(() => _animator.UpdateMovementAnimation(1f)).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RotateToTarget(_direction);
                    _animator.UpdateMovementAnimation(0f);
                    _debugPathPoints = null;
                });

            await tween.AsyncWaitForCompletion();

            _currentPathNode = path.Last();

            if (_currentPathNode != null)
            {
                List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType, _direction);
                neighbours.ForEach(neighbour => { neighbour.IsWalkable = false; neighbour.UpdateVisual(); });

                _currentPathNode.IsWalkable = false;
                _currentPathNode.UpdateVisual();
                _nodes = neighbours;
            }

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
                    return CurrentTarget.Transformable.SizeEffect;
                case ObjectSizeType.Medium:
                    switch (CurrentTarget.Transformable.ObjectSizeType)
                    {
                        case ObjectSizeType.Small:
                            return _sizeEffectY;
                        case ObjectSizeType.Medium:
                            return _sizeEffectY;
                        case ObjectSizeType.Big:
                            return _sizeEffectY + 1;
                    }

                    break;
                case ObjectSizeType.Big:
                    switch (CurrentTarget.Transformable.ObjectSizeType)
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