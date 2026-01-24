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
    public class AnimalMovement : MonoBehaviour, ITransformable
    {
        private const string NodeLayerName = "PathNode";
        private const string GameGridId = "Game Grid";

        [SerializeField] private float _yPos;
        [SerializeField] private float _zOffset;
        [SerializeField] private ObjectSizeType _sizeType;
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private List<GridCell> _nodes = new List<GridCell>();
        [SerializeField] private float _raycastOffset = 0.4f;
        [SerializeField] private int _tilesPerMove = 2;
        [SerializeField] private int _sizeEffectY;
        [SerializeField] private Vector3 _direction = Vector3.forward;
        [SerializeField] private bool _debugDrawPath = true;

        private Vector3[] _debugPathPoints;

        public Vector3 Offset => new Vector3(0f, 0f, _zOffset);

        private IPathfindingService _pathfinder;
        private IGridManager _gridManager;

        public GridCell _currentPathNode;

        public ObjectSizeType ObjectSizeType => _sizeType;

        public int SizeEffect => _sizeEffectY;

        public GridCell CurrentPathNode => _currentPathNode;

        public Vector3 Position => transform.position;


        public Vector2Int IntPosition =>
            new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        public Vector3 Direction => _direction;

        public ITarget CurrentTarget { get; set; }

        public List<GridCell> Nodes => _nodes;

        private IAbility _ability;

        private IAnimalFactory _animalFactory;

        private readonly List<AnimalMovement> _additionalAnimals = new List<AnimalMovement>();

        public void Upgrade(int multiplier) => _tilesPerMove *= multiplier;


        [Inject]
        private void Construct(IPathfindingService pathfinder, IGridManager gridManager, IAnimalFactory animalFactory)
        {
            _pathfinder = pathfinder;
            _gridManager = gridManager;
            _animalFactory = animalFactory;
        }

        public void AddAdditionalAnimalsRange(IEnumerable<AnimalMovement> animals)
        {
            _additionalAnimals.AddRange(animals);
        }

        private void Start()
        {
            RotateToTarget(_direction);
            _ability = new MultipleCharacters(this, _gridManager, _animalFactory, AnimalType.Chicken, 3);
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
                    if (animal.CurrentPathNode != null)
                    {
                        animal.CurrentPathNode.IsWalkable = true;
                        animal.CurrentPathNode.UpdateVisual();
                    }
                    animal._nodes.ForEach(node => { node.IsWalkable = true; node.UpdateVisual(); });
                    animal._nodes.Clear();
                });
            }

            if (_currentPathNode != null)
            {
                _currentPathNode.IsWalkable = true;
                _currentPathNode.UpdateVisual();
            }

            _nodes.ForEach(node => { node.IsWalkable = true; node.UpdateVisual(); });
            _nodes.Clear();
        }

        public void SetCurrentNode(GridCell node)
        {
            _currentPathNode = node;
        }

        public void FillNodes(List<GridCell> nodes)
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

        public void SetNewNode(GridCell gridCell)
        {
            ClearNodes();

            Place(gridCell.WorldPosition, Utilities.GetMovementOffset(gridCell, _sizeType, _direction));
            List<GridCell> neighbours = _gridManager.GetNeighborCells(gridCell.GridPosition, _sizeType.ToUnitSize(), _direction.ToDirection());
            neighbours.ForEach(neighbour => { neighbour.IsWalkable = false; neighbour.UpdateVisual(); });

            _currentPathNode = gridCell;
            _currentPathNode.IsWalkable = false;
            _currentPathNode.UpdateVisual();
            _nodes = neighbours;
        }

        public bool IsCloseToTarget(Vector3 target)
        {
            if (_currentPathNode == null || CurrentTarget == null) return false;

            // Собрать все клетки, которые занимает атакующий юнит
            List<GridCell> attackerCells = new List<GridCell> { _currentPathNode };
            attackerCells.AddRange(_nodes);

            // Получить клетки, которые занимает цель
            ITransformable targetTransformable = CurrentTarget.Transformable;
            List<GridCell> targetCells = new List<GridCell>();

            // Добавляем главную клетку цели
            if (targetTransformable.CurrentPathNode != null)
            {
                targetCells.Add(targetTransformable.CurrentPathNode);
            }

            // Если цель - AnimalMovement, используем её Nodes
            // Для других целей предполагаем, что они занимают только CurrentPathNode
            if (targetTransformable is AnimalMovement targetAnimal)
            {
                targetCells.AddRange(targetAnimal.Nodes);
            }

            // Если не удалось получить клетки цели, проверяем по позиции
            if (targetCells.Count == 0)
            {
                Vector2Int targetGridPos = new Vector2Int(
                    Mathf.RoundToInt(target.x),
                    Mathf.RoundToInt(target.z)
                );
                GridCell targetMainCell = _gridManager.GetCell(targetGridPos);
                if (targetMainCell == null) return false;
                targetCells.Add(targetMainCell);
            }

            // Проверить, есть ли хотя бы одна пара соседних клеток (включая диагонали)
            foreach (GridCell attackerCell in attackerCells)
            {
                foreach (GridCell targetCell in targetCells)
                {
                    int dx = Mathf.Abs(attackerCell.X - targetCell.X);
                    int dy = Mathf.Abs(attackerCell.Y - targetCell.Y);

                    // Соседство по Чебышёву: max(dx, dy) <= 1, исключая совпадение
                    if (dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private List<GridCell> FindPath(Vector2Int[] points)
        {
            ClearNodes();

            UnitSize unitSize = _sizeType.ToUnitSize();
            Direction direction = _direction.ToDirection();
            Vector2Int startPos = new Vector2Int(_currentPathNode.X, _currentPathNode.Y);

            for (int i = 0; i < points.Length; i++)
            {
                PathResult result = _pathfinder.FindPath(startPos, points[i], unitSize, direction);

                if (result.Success && result.Path.Count > 0)
                {
                    // Convert Vector2Int path to GridCell path
                    List<GridCell> path = result.Path
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
            Vector2Int[] possibleMoves =
            {
                new Vector2Int(_currentPathNode.X + 1, _currentPathNode.Y),
                new Vector2Int(_currentPathNode.X - 1, _currentPathNode.Y),
                new Vector2Int(_currentPathNode.X, _currentPathNode.Y - 1),
            };

            List<GridCell> path = FindPath(possibleMoves);
            
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

            ClearNodes();

            _currentPathNode = path.LastOrDefault();
            if (_currentPathNode != null)
            {
                _currentPathNode.IsWalkable = false;
                _currentPathNode.UpdateVisual();
                List<GridCell> neighbours = _gridManager.GetNeighborCells(_currentPathNode.GridPosition, _sizeType.ToUnitSize(), _direction.ToDirection());
                neighbours.ForEach(neighbour => { neighbour.IsWalkable = false; neighbour.UpdateVisual(); });
                FillNodes(neighbours);
            }

            await tween.AsyncWaitForCompletion();
        }

        //надо находить врага и просто брать среди их тайлов ближайший ко мне

        public async Task Move(Vector3 target, Func<Task> reachedTargetCallback = null)
        {
            Vector2Int[] points = GetPossibleMoves(target);

            List<GridCell> path = FindPath(points);

            Debug.Log($"[PathFindDebug] is path is null - {path}, count - {path?.Count ?? 0}");
            if (path == null || path.Count == 0) return;

            if (path.Count > _tilesPerMove + 1)
                path = path.Take(_tilesPerMove + 1).ToList();

            Vector3[] pathPositions = GetPathPositions(path);
            _debugPathPoints = _debugDrawPath ? pathPositions : null;

            // Free previous node and neighbours before moving so the tile color reverts to green
            ClearNodes();

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
                List<GridCell> neighbours = _gridManager.GetNeighborCells(_currentPathNode.GridPosition, _sizeType.ToUnitSize(), _direction.ToDirection());
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

            int zOffset = target.z > _currentPathNode.Y ? z - sizeEffect : z + sizeEffect;

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

        private Vector3[] GetPathPositions(List<GridCell> path)
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