using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Facades;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using Code.Pathfinding;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Code.Animals.Movement
{
    [RequireComponent(typeof(UnitOccupancy))]
    public class AnimalMovement : MonoBehaviour, ITransformable
    {
        private const string NodeLayerName = "PathNode";
        private const string GameGridId = "Game Grid";

        [SerializeField] private float _yPos;
        [SerializeField] private float _zOffset;
        [SerializeField] private UnitSize _unitSize = UnitSize.Small;
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private List<GridCell> _nodes = new List<GridCell>();
        [SerializeField] private float _raycastOffset = 0.4f;
        [SerializeField] private int _tilesPerMove = 2;
        [SerializeField] private int _sizeEffectY;
        [SerializeField] private Direction _direction = Direction.North;
        [SerializeField] private bool _debugDrawPath = true;

        private Vector3[] _debugPathPoints;

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfindingService _pathfinder;
        private IGridManager _gridManager;
        private ITargetDetector _targetDetector;
        private IUnitOccupancy _unitOccupancy;
        private IMovementAnimator _movementAnimator;

        public GridCell _currentPathNode;

        public UnitSize UnitSize => _unitSize;

        public int SizeEffect => _sizeEffectY;

        public GridCell CurrentPathNode => _currentPathNode;

        public Vector3 Position => transform.position;


        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        public Direction Direction => _direction;

        public ITarget CurrentTarget { get; set; }

        public List<GridCell> Nodes => _nodes;

        private IAbility _ability;

        private IAnimalFactory _animalFactory;

        private readonly List<AnimalMovement> _additionalAnimals = new List<AnimalMovement>();

        public void Upgrade(int multiplier) => _tilesPerMove *= multiplier;

        private void Awake()
        {
            _unitOccupancy = GetComponent<UnitOccupancy>();
            _movementAnimator = new AnimalMovementAnimator(_animator);
        }

        [Inject]
        private void Construct(
            IPathfindingService pathfinder,
            IGridManager gridManager,
            IAnimalFactory animalFactory,
            ITargetDetector targetDetector)
        {
            _pathfinder = pathfinder;
            _gridManager = gridManager;
            _animalFactory = animalFactory;
            _targetDetector = targetDetector;
        }

        public void AddAdditionalAnimalsRange(IEnumerable<AnimalMovement> animals)
        {
            _additionalAnimals.AddRange(animals);
            _unitOccupancy.AddAdditionalUnits(animals);
        }

        private void Start()
        {
            RotateToTarget(DirectionToVector3(_direction));
            _ability = new MultipleCharacters(this, _gridManager, _animalFactory, AnimalType.Chicken, 3);
        }

        public void Place(Vector3 position)
        {
            position.y = _yPos;
            transform.position = position + Utilities.GetMovementOffset(_unitSize, _direction) + Offset;
        }

        private void Place(Vector3 position, Vector3 offset)
        {
            position.y = _yPos;
            transform.position = position + offset + Offset;
        }

        public void ClearNodes()
        {
            _unitOccupancy.ClearOccupancy();
        }

        public void SetCurrentNode(GridCell node)
        {
            _currentPathNode = node;
            _unitOccupancy.SetCurrentCell(node);
        }

        public void FillNodes(List<GridCell> nodes)
        {
            _nodes = nodes;
            _unitOccupancy.SetOccupiedCells(nodes);
        }

        public bool IsCloseToTarget(Vector3 target)
        {
            return _targetDetector.IsCloseToTarget(_currentPathNode, _nodes, CurrentTarget);
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

        public void SetNewNode(GridCell gridCell)
        {
            ClearNodes();

            Place(gridCell.WorldPosition, Utilities.GetMovementOffset(gridCell, _unitSize, _direction));
            var neighbours = _gridManager.GetNeighborCells(gridCell.GridPosition, _unitSize, _direction);

            _currentPathNode = gridCell;
            _nodes = neighbours;
            _unitOccupancy.MarkCellsAsOccupied(gridCell, neighbours);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + new Vector3(0f, 0f, _raycastOffset), Vector3.down);

            if (_debugDrawPath && _debugPathPoints != null && _debugPathPoints.Length > 1)
            {
                var prev = Gizmos.color;
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

        private List<GridCell> FindPath(Vector2Int[] points)
        {
            ClearNodes();

            var startPos = new Vector2Int(_currentPathNode.X, _currentPathNode.Y);

            for (int i = 0; i < points.Length; i++)
            {
                var result = _pathfinder.FindPath(startPos, points[i], _unitSize, _direction);

                if (result.Success && result.Path.Count > 0)
                {
                    // Convert Vector2Int path to GridCell path
                    var path = result.Path
                        .Select(pos => _gridManager.GetCell(pos))
                        .Where(cell => cell != null)
                        .ToList();

                    if (path.Count > 0) return path;
                }
            }

            return null;
        }

        public async Task Shift()
        {
            var dodgePositions = GetDodgePositions();
            var path = FindPath(dodgePositions);

            if (path == null || path.Count == 0) return;

            await ExecuteDodgeMovement(path);
            UpdateNodeOccupancy(path.Last());
        }

        public async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            var possiblePositions = _targetDetector.GetPossibleAttackPositions(
                _currentPathNode, CurrentTarget, _unitSize, _direction);

            var path = FindPath(possiblePositions);
            if (path == null || path.Count == 0) return;

            path = LimitPathBySpeed(path);

            await ExecuteMovement(path);

            UpdateNodeOccupancy(path.Last());

            if (_targetDetector.IsCloseToTarget(_currentPathNode, _nodes, CurrentTarget))
            {
                RotateToTarget(target - transform.position);
                await reachedTargetCallback?.Invoke()!;
            }
        }




        public void RotateToTarget(Vector3 target)
        {
            if (target != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(target);
            }
        }

        private Vector3[] GetPathPositions(List<GridCell> path)
        {
            var pathPositions = path.Select(node => node.WorldPosition).ToArray();

            for (int i = 0; i < pathPositions.Length; i++)
            {
                pathPositions[i].y = _yPos;
                pathPositions[i] += Utilities.GetMovementOffset(_unitSize, _direction) + Offset;
            }

            return pathPositions;
        }

        private Vector3 GetDirection(Vector3 target)
        {
            var position = transform.position;
            var direction = (target - position).normalized;

            return direction;
        }

        private Vector3 DirectionToVector3(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Vector3.forward;
                case Direction.South:
                    return Vector3.back;
                case Direction.East:
                    return Vector3.right;
                case Direction.West:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }

        private List<GridCell> LimitPathBySpeed(List<GridCell> path)
        {
            if (path.Count > _tilesPerMove + 1)
                return path.Take(_tilesPerMove + 1).ToList();
            return path;
        }

        private async Task ExecuteMovement(List<GridCell> path)
        {
            var pathPositions = GetPathPositions(path);
            _debugPathPoints = _debugDrawPath ? pathPositions : null;

            _unitOccupancy.ClearOccupancy();

            var tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;
                    var direction = (transform.position - pathPositions[i]);
                    RotateToTarget(direction);
                })
                .OnUpdate(() => _movementAnimator.PlayMovementAnimation(1f))
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    RotateToTarget(DirectionToVector3(_direction));
                    _movementAnimator.StopMovementAnimation();
                    _debugPathPoints = null;
                });

            await tween.AsyncWaitForCompletion();
        }

        private void UpdateNodeOccupancy(GridCell finalCell)
        {
            if (finalCell == null) return;

            var neighbours = _gridManager.GetNeighborCells(
                finalCell.GridPosition, _unitSize, _direction);

            _currentPathNode = finalCell;
            _nodes = neighbours;
            _unitOccupancy.MarkCellsAsOccupied(finalCell, neighbours);
        }

        private Vector2Int[] GetDodgePositions()
        {
            return new Vector2Int[]
            {
                new Vector2Int(_currentPathNode.X + 1, _currentPathNode.Y),
                new Vector2Int(_currentPathNode.X - 1, _currentPathNode.Y),
                new Vector2Int(_currentPathNode.X, _currentPathNode.Y - 1),
            };
        }

        private async Task ExecuteDodgeMovement(List<GridCell> path)
        {
            _movementAnimator.PlayJumpAnimation();

            var pathPositions = GetPathPositions(path);
            _debugPathPoints = _debugDrawPath ? pathPositions : null;

            var tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;
                    var direction = GetDirection(pathPositions[i]);
                    RotateToTarget(-direction);
                })
                .OnComplete(() =>
                {
                    RotateToTarget(Vector3.forward);
                    _debugPathPoints = null;
                });

            _unitOccupancy.ClearOccupancy();

            await tween.AsyncWaitForCompletion();
        }

    }
}
