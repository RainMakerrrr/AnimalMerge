using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Facades;
using Code.Infrastructure.Factories.Animals;
using Code.Pathfinding;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
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

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfinder _pathfinder;
        private Grid _grid;
        private Grid _mergeGrid;

        public PathNode _currentPathNode;

        public ObjectSizeType ObjectSizeType => _sizeType;

        public int SizeEffect => _sizeEffectY;

        public PathNode CurrentPathNode => _currentPathNode;

        public Vector3 Position => transform.position;


        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        public Vector3 Direction => _direction;

        public ITarget CurrentTarget { get; set; }

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
            _mergeGrid = GameObject.Find("Merge Grid").GetComponent<Grid>();

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
                    animal._nodes.ForEach(node => node.IsWalkable = true);
                    animal._nodes.Clear();
                });
            }

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
                    out RaycastHit hit))
            {
                var raycastable = hit.collider.GetComponent<IRaycastable>();
                
                return raycastable != null && raycastable.Accept(GetComponent<AnimalFacade>());
            }

            return false;
        }

        public void SetNewNode(PathNode pathNode)
        {
            if (_currentPathNode != null) _currentPathNode.IsWalkable = true;

            ClearNodes();

            Place(pathNode.WorldPosition, Utilities.GetMovementOffset(pathNode, _sizeType, _direction));
            List<PathNode> neighbours = pathNode.GetNeighbours(_sizeType, _direction);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

            _currentPathNode = pathNode;
            _currentPathNode.IsWalkable = false;
            _nodes = neighbours;
        }

        public bool IsCloseToTarget(Vector3 target)
        {
            int sizeEffect = GetSizeOffset();

            return Mathf.Abs(_currentPathNode.y - target.z) <= sizeEffect &&
                   Mathf.Abs(_currentPathNode.x - target.x) <= 1f;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + new Vector3(0f, 0f, _raycastOffset), Vector3.down);
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
            
            if (path == null) return;

            _animator.JumpAnimation();

            Vector3[] pathPositions = GetPathPositions(path);

            Tween tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;

                    Vector3 direction = GetDirection(pathPositions[i]);
                    RotateToTarget(-direction);
                })
                .OnComplete(() => RotateToTarget(Vector3.forward));

            _currentPathNode.IsWalkable = true;
            _nodes.ForEach(node => node.IsWalkable = true);
            _nodes.Clear();
            
            _currentPathNode = path.LastOrDefault();
            _currentPathNode.IsWalkable = false;
            List<PathNode> neighbours = _currentPathNode.GetNeighbours(_sizeType, _direction);
            neighbours.ForEach(neighbour => neighbour.IsWalkable = false);
            FillNodes(neighbours);

            await tween.AsyncWaitForCompletion();
        }


        public async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            Vector2Int[] points = GetPossibleMoves(target);

            List<PathNode> path = FindPath(points);

            if (path == null || path.Count == 0) return;

            if (path.Count > _tilesPerMove + 1)
                path = path.Take(_tilesPerMove + 1).ToList();

            Vector3[] pathPositions = GetPathPositions(path);

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