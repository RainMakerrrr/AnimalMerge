using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Контроллер движения юнита с State Machine
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class UnitMovementController : MonoBehaviour
    {
        public enum MovementState
        {
            Idle,
            SearchingTarget,
            MovingToTarget,
            Attacking,
            Waiting
        }

        [Header("Settings")]
        [SerializeField] private float pathRecalculationInterval = 1f;
        [SerializeField] private float targetSearchInterval = 0.5f;
        [SerializeField] private float arrivedThreshold = 0.1f;
        [SerializeField] private float blockedWaitTime = 2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private Unit unit;
        private MovementState currentState = MovementState.Idle;
        
        private List<GridNode> currentPath;
        private int currentPathIndex;
        private Vector3 currentMoveTarget;
        
        private float lastPathRecalculation;
        private float lastTargetSearch;
        private float blockedStartTime;
        
        private Unit targetEnemy;

        public MovementState CurrentState => currentState;

        private void Awake()
        {
            unit = GetComponent<Unit>();
        }

        private void Start()
        {
            ChangeState(MovementState.SearchingTarget);
        }

        private void Update()
        {
            if (unit.IsDead)
                return;

            switch (currentState)
            {
                case MovementState.Idle:
                    UpdateIdle();
                    break;
                case MovementState.SearchingTarget:
                    UpdateSearchingTarget();
                    break;
                case MovementState.MovingToTarget:
                    UpdateMovingToTarget();
                    break;
                case MovementState.Attacking:
                    UpdateAttacking();
                    break;
                case MovementState.Waiting:
                    UpdateWaiting();
                    break;
            }
        }

        #region State Updates

        private void UpdateIdle()
        {
            // В idle просто ищем цель
            if (Time.time - lastTargetSearch > targetSearchInterval)
            {
                ChangeState(MovementState.SearchingTarget);
            }
        }

        private void UpdateSearchingTarget()
        {
            lastTargetSearch = Time.time;

            // Поиск ближайшего врага
            targetEnemy = TargetFindingSystem.Instance?.FindNearestEnemy(unit);

            if (targetEnemy == null)
            {
                ChangeState(MovementState.Idle);
                return;
            }

            // Проверяем, в радиусе атаки ли цель
            if (unit.IsInAttackRange(targetEnemy))
            {
                ChangeState(MovementState.Attacking);
                return;
            }

            // Строим путь к цели
            BuildPathToTarget();
        }

        private void UpdateMovingToTarget()
        {
            // Проверяем валидность цели
            if (targetEnemy == null || targetEnemy.IsDead)
            {
                StopMovement();
                ChangeState(MovementState.SearchingTarget);
                return;
            }

            // Проверяем, достигли ли радиуса атаки
            if (unit.IsInAttackRange(targetEnemy))
            {
                StopMovement();
                ChangeState(MovementState.Attacking);
                return;
            }

            // Пересчет пути, если цель далеко
            if (Time.time - lastPathRecalculation > pathRecalculationInterval)
            {
                BuildPathToTarget();
                return;
            }

            // Движение по пути
            if (currentPath != null && currentPath.Count > 0)
            {
                MoveAlongPath();
            }
            else
            {
                // Путь закончился или недоступен
                ChangeState(MovementState.SearchingTarget);
            }
        }

        private void UpdateAttacking()
        {
            // Проверка валидности цели
            if (targetEnemy == null || targetEnemy.IsDead)
            {
                ChangeState(MovementState.SearchingTarget);
                return;
            }

            // Проверка дистанции
            if (!unit.IsInAttackRange(targetEnemy))
            {
                // Цель вышла из радиуса - преследуем
                ChangeState(MovementState.MovingToTarget);
                return;
            }

            // Атакуем
            if (unit.CanAttackTarget(targetEnemy))
            {
                unit.Attack(targetEnemy);
                
                // Поворачиваемся к цели
                LookAtTarget(targetEnemy.transform.position);
            }
        }

        private void UpdateWaiting()
        {
            // Ожидание, когда путь заблокирован
            if (Time.time - blockedStartTime > blockedWaitTime)
            {
                // Время ожидания истекло - ищем новый путь
                ChangeState(MovementState.SearchingTarget);
            }
            else
            {
                // Проверяем, освободился ли путь
                if (IsNextNodeAvailable())
                {
                    ChangeState(MovementState.MovingToTarget);
                }
            }
        }

        #endregion

        #region Movement Logic

        private void BuildPathToTarget()
        {
            if (targetEnemy == null || PathfindingSystem.Instance == null)
                return;

            lastPathRecalculation = Time.time;

            // Получаем позицию для атаки
            Vector2Int targetPos = TargetFindingSystem.Instance.GetAttackPosition(unit, targetEnemy);

            // Строим путь
            currentPath = PathfindingSystem.Instance.FindPath(
                unit, 
                unit.CurrentGridPosition, 
                targetPos
            );

            if (currentPath != null && currentPath.Count > 0)
            {
                currentPathIndex = 0;
                ChangeState(MovementState.MovingToTarget);
            }
            else
            {
                // Путь не найден
                ChangeState(MovementState.Idle);
            }
        }

        private void MoveAlongPath()
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                StopMovement();
                return;
            }

            GridNode targetNode = currentPath[currentPathIndex];

            // Проверяем, доступна ли следующая клетка
            if (!IsNodeAvailable(targetNode))
            {
                // Клетка заблокирована - ждем
                blockedStartTime = Time.time;
                ChangeState(MovementState.Waiting);
                return;
            }

            // Резервируем следующую клетку
            ReserveNode(targetNode);

            // Целевая мировая позиция
            Vector3 targetWorldPos = GridManager.Instance.GetWorldPosition(
                targetNode.GridX, targetNode.GridY);

            // Двигаемся к цели
            Vector3 direction = (targetWorldPos - transform.position).normalized;
            float step = unit.MovementSpeed * Time.deltaTime;
            
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetWorldPos, 
                step
            );

            // Поворот к направлению движения
            if (direction != Vector3.zero)
            {
                LookAtDirection(direction);
            }

            // Проверяем, достигли ли клетки
            if (Vector3.Distance(transform.position, targetWorldPos) < arrivedThreshold)
            {
                // Обновляем позицию в сетке
                Vector2Int newGridPos = new Vector2Int(targetNode.GridX, targetNode.GridY);
                unit.MoveToGridPosition(newGridPos);

                // Снимаем резервацию
                ReleaseReservation(targetNode);

                // Переходим к следующей клетке
                currentPathIndex++;

                if (currentPathIndex >= currentPath.Count)
                {
                    // Путь завершен
                    StopMovement();
                    ChangeState(MovementState.SearchingTarget);
                }
            }
        }

        private bool IsNodeAvailable(GridNode node)
        {
            if (node == null)
                return false;

            // Для юнитов больше 1x1 проверяем все клетки
            var cells = unit.Size.GetAllOccupiedCells(node.GridPosition);
            
            foreach (var cell in cells)
            {
                var checkNode = GridManager.Instance.GetNode(cell);
                if (checkNode == null || !checkNode.IsAvailable(unit))
                    return false;
            }

            return true;
        }

        private bool IsNextNodeAvailable()
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
                return false;

            return IsNodeAvailable(currentPath[currentPathIndex]);
        }

        private void ReserveNode(GridNode node)
        {
            var cells = unit.Size.GetAllOccupiedCells(node.GridPosition);
            
            foreach (var cell in cells)
            {
                var checkNode = GridManager.Instance?.GetNode(cell);
                checkNode?.TryReserve(unit);
            }
        }

        private void ReleaseReservation(GridNode node)
        {
            var cells = unit.Size.GetAllOccupiedCells(node.GridPosition);
            
            foreach (var cell in cells)
            {
                var checkNode = GridManager.Instance?.GetNode(cell);
                checkNode?.ReleaseReservation(unit);
            }
        }

        private void StopMovement()
        {
            currentPath = null;
            currentPathIndex = 0;
        }

        #endregion

        #region Utility

        private void ChangeState(MovementState newState)
        {
            if (currentState == newState)
                return;

            if (showDebugInfo)
            {
                Debug.Log($"{gameObject.name}: {currentState} -> {newState}");
            }

            currentState = newState;
        }

        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Убираем вертикальную составляющую
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void LookAtDirection(Vector3 direction)
        {
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    Time.deltaTime * 10f
                );
            }
        }

        public void OnUnitDied()
        {
            StopMovement();
            currentState = MovementState.Idle;
            enabled = false;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || currentPath == null)
                return;

            if (GridManager.Instance == null)
                return;

            Gizmos.color = Color.cyan;

            for (int i = currentPathIndex; i < currentPath.Count; i++)
            {
                Vector3 worldPos = GridManager.Instance.GetWorldPosition(
                    currentPath[i].GridX, currentPath[i].GridY);
                
                Gizmos.DrawWireSphere(worldPos, 0.2f);

                if (i > currentPathIndex)
                {
                    Vector3 prevPos = GridManager.Instance.GetWorldPosition(
                        currentPath[i - 1].GridX, currentPath[i - 1].GridY);
                    Gizmos.DrawLine(prevPos, worldPos);
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo)
                return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2);
            
            if (screenPos.z > 0)
            {
                GUI.Label(
                    new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20),
                    $"State: {currentState}"
                );
            }
        }

        #endregion
    }
}

