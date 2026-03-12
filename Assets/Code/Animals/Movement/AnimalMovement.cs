using System;
using System.Collections;
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
        private const string AnimalLayerName = "Animal";
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
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _turnAnimationDuration = 0.5f;
        [SerializeField] private float _turnDirectionSmoothSpeed = 4f;
        [SerializeField] private float _turnAmplification = 1.3f;
        [SerializeField] private float _turnAngleThreshold = 10f;

        private Vector3[] _debugPathPoints;
        private Quaternion _targetRotation;
        private bool _isMoving;
        private Vector3 _previousMoveDirection;
        private float _turnAnimationTimer;
        private float _currentTurnDirection;
        private float _targetTurnDirection;

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfindingService _pathfinder;
        private IGridManager _gridManager;
        private ITargetDetector _targetDetector;
        private IUnitOccupancy _unitOccupancy;
        private IMovementAnimator _movementAnimator;

        public GridCell _currentPathNode;

        // Debug: target cell where unit is moving to
        [SerializeField] private GridCell _debugTargetCell;

        public UnitSize UnitSize => _unitSize;

        public int SizeEffect => _sizeEffectY;

        public GridCell CurrentPathNode => _currentPathNode;

        // Explicit interface implementation for ITransformable.CurrentPathNode
        IGridCell ITransformable.CurrentPathNode => _currentPathNode;

        public Vector3 Position => transform.position;


        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        public Direction Direction => _direction;

        public ITarget CurrentTarget { get; set; }

        public List<GridCell> Nodes => _nodes;

        /// <summary>
        /// Returns all grid cells occupied by this unit (CurrentPathNode + Nodes)
        /// </summary>
        public List<IGridCell> GetOccupiedCells()
        {
            var occupiedCells = new List<IGridCell>();

            if (_currentPathNode != null)
            {
                occupiedCells.Add(_currentPathNode);
            }

            if (_nodes != null && _nodes.Count > 0)
            {
                occupiedCells.AddRange(_nodes.Cast<IGridCell>());
            }

            return occupiedCells;
        }

        private IAbility _ability;

        private IAnimalFactory _animalFactory;

        private readonly List<AnimalMovement> _additionalAnimals = new List<AnimalMovement>();

        public void Upgrade(int multiplier) => _tilesPerMove *= multiplier;

        /// <summary>
        /// Sets the debug target cell for visualization
        /// </summary>
        public void SetDebugTargetCell(GridCell targetCell)
        {
            _debugTargetCell = targetCell;
        }

        private void Awake()
        {
            _unitOccupancy = GetComponent<UnitOccupancy>();
            _movementAnimator = new AnimalMovementAnimator(_animator);
        }

        [Inject]
        private void Construct(
            IPathfindingService pathfinder,
            [Inject(Id = GridIdentifier.GameGrid)] IGridManager gridManager,
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
            _targetRotation = transform.rotation;
            RotateToTarget(DirectionToVector3(_direction));
        }

        private void Update()
        {
            UpdateTurnDirectionAnimation();
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
            return _targetDetector.IsCloseToTarget(_currentPathNode, _nodes.Cast<IGridCell>().ToList(), CurrentTarget);
        }

        public bool TryPlace()
        {
            var hits = Physics.RaycastAll(
                transform.position + new Vector3(0f, 20f, _raycastOffset),
                Vector3.down,
                Mathf.Infinity,
                LayerMask.GetMask(NodeLayerName, AnimalLayerName));

            // Sort by distance (closest first) and filter out self
            var validHits = hits
                .Where(h => h.collider.transform != transform && !h.collider.transform.IsChildOf(transform))
                .OrderBy(h => h.distance)
                .ToList();

            if (validHits.Count == 0)
                return false;

            var hit = validHits.FirstOrDefault();
            var raycastable = hit.collider.GetComponent<IRaycastable>();

            if (raycastable != null)
            {
                Debug.Log($"[Merge] try place on {raycastable}");
            }

            return raycastable != null && raycastable.Accept(GetComponent<AnimalFacade>());
        }

        public void SetNewNode(GridCell gridCell)
        {
            ClearNodes();

            Place(gridCell.WorldPosition, Utilities.GetMovementOffset(gridCell, _unitSize, _direction));
            var neighbours = _gridManager.GetNeighborCells(gridCell.GridPosition, _unitSize, _direction).Cast<GridCell>().ToList();

            _currentPathNode = gridCell;
            _nodes = neighbours;
            _unitOccupancy.MarkCellsAsOccupied(gridCell, neighbours);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + new Vector3(0f, 2f, _raycastOffset), Vector3.down);

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

            // Draw debug target cell
            if (_debugTargetCell != null)
            {
                var targetPos = _debugTargetCell.WorldPosition;
                targetPos.y = _yPos + 0.5f; // Lift it up to be visible

                // Draw a bright red wire cube
                Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                Gizmos.DrawWireCube(targetPos, Vector3.one);

                // Draw a semi-transparent red cube
                
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawCube(targetPos, Vector3.one);
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
                        .Cast<GridCell>()
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

        public virtual async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            Debug.Log($"[PathfindingDebug][Move] Input target: {target}, CurrentTarget: {CurrentTarget?.Transformable?.CurrentPathNode?.GridPosition}, Current position: {_currentPathNode?.GridPosition}");

            var possiblePositions = _targetDetector.GetPossibleAttackPositions(
                _currentPathNode, CurrentTarget, _unitSize, _direction);

            Debug.Log($"[PathfindingDebug][Move] Possible positions: {string.Join(", ", possiblePositions.Select(p => $"({p.x},{p.y})"))}");

            var path = FindPath(possiblePositions);
            if (path == null || path.Count == 0) return;

            Debug.Log($"[PathfindingDebug][Move] Path found: {string.Join(" -> ", path.Select(p => p.GridPosition))}");

            path = LimitPathBySpeed(path);

            Debug.Log($"[PathfindingDebug][Move] Path after speed limit: {string.Join(" -> ", path.Select(p => p.GridPosition))}");

            await ExecuteMovement(path);

            UpdateNodeOccupancy(path.Last());

            if (_targetDetector.IsCloseToTarget(_currentPathNode, _nodes.Cast<IGridCell>().ToList(), CurrentTarget))
            {
                RotateToTarget(CurrentTarget!.Transformable!.Position - transform.position);
                await reachedTargetCallback?.Invoke()!;
            }
        }



        private void UpdateTurnDirectionAnimation()
        {
            if (Mathf.Abs(_currentTurnDirection - _targetTurnDirection) > 0.01f)
            {
                _currentTurnDirection = Mathf.Lerp(
                    _currentTurnDirection,
                    _targetTurnDirection,
                    Time.deltaTime * _turnDirectionSmoothSpeed
                );

                _movementAnimator.UpdateTurnDirection(_currentTurnDirection);
            }
        }

        private void CalculateTurnDirection(Vector3 currentWaypoint, Vector3 nextWaypoint)
        {
            var direction = (nextWaypoint - currentWaypoint).normalized;

            if (direction.sqrMagnitude < 0.01f) return;

            var baseDirection = DirectionToVector3(_direction);
            var angle = Vector3.SignedAngle(baseDirection, direction, Vector3.up);

            if (Mathf.Abs(angle) > _turnAngleThreshold)
            {
                _targetTurnDirection = Mathf.Clamp((angle / 90f) * _turnAmplification, -1f, 1f);
                _turnAnimationTimer = _turnAnimationDuration;
            }
            else
            {
                _targetTurnDirection = 0f;
            }

            _previousMoveDirection = direction;
            _targetRotation = Quaternion.LookRotation(direction);
        }

        private void UpdateTurnTimer()
        {
            if (_turnAnimationTimer > 0f)
            {
                _turnAnimationTimer -= Time.deltaTime;

                if (_turnAnimationTimer <= 0f)
                {
                    _targetTurnDirection = 0f;
                }
            }
        }

        private void ApplyRotation()
        {
            if (Quaternion.Angle(transform.rotation, _targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    _targetRotation,
                    Time.deltaTime * _rotationSpeed
                );
            }
        }

        public void RotateToTarget(Vector3 target)
        {
            if (target == Vector3.zero) return;

            transform.rotation = Quaternion.LookRotation(target);
        }

        private IEnumerator RotateToTargetAsync(Vector3 target)
        {
            if (target == Vector3.zero) yield break;

            var targetRotation = Quaternion.LookRotation(target);

            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * _rotationSpeed
                );

                yield return null;
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
            _isMoving = true;
            _previousMoveDirection = Vector3.zero;
            _currentTurnDirection = 0f;
            _targetTurnDirection = 0f;

            var tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    var nextIndex = i + 1;
                    if (nextIndex >= pathPositions.Length) return;

                    CalculateTurnDirection(pathPositions[i], pathPositions[nextIndex]);
                })
                .OnUpdate(() =>
                {
                    ApplyRotation();
                    UpdateTurnTimer();
                    _movementAnimator.PlayMovementAnimation(1f);
                })
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _isMoving = false;
                    RotateToTarget(DirectionToVector3(_direction));
                    //StartCoroutine(RotateToTargetAsync(DirectionToVector3(_direction)));
                    _movementAnimator.StopMovementAnimation();
                    _targetTurnDirection = 0f;
                    _debugPathPoints = null;
                });

            await tween.AsyncWaitForCompletion();
        }

        private void UpdateNodeOccupancy(GridCell finalCell)
        {
            if (finalCell == null) return;

            var neighbours = _gridManager.GetNeighborCells(
                finalCell.GridPosition, _unitSize, _direction).Cast<GridCell>().ToList();

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

            _isMoving = true;

            var tween = transform.DOPath(pathPositions, pathPositions.Length / 2f)
                .OnWaypointChange(i =>
                {
                    if (i >= pathPositions.Length) return;
                    var direction = GetDirection(pathPositions[i]);
                    RotateToTarget(-direction);
                })
                .OnComplete(() =>
                {
                    _isMoving = false;
                    RotateToTarget(Vector3.forward);
                    _debugPathPoints = null;
                });

            _unitOccupancy.ClearOccupancy();

            await tween.AsyncWaitForCompletion();
        }

    }
}
