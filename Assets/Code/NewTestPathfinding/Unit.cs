using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Базовый класс юнита с поддержкой разных размеров
    /// </summary>
    [RequireComponent(typeof(UnitMovementController))]
    public class Unit : MonoBehaviour
    {
        [Header("Unit Properties")]
        [SerializeField] private UnitSize unitSize = UnitSize.Size1X1;
        [SerializeField] private Team team = Team.Player;
        [SerializeField] private float movementSpeed = 3f;
        [SerializeField] private int attackRange = 1;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private int damage = 10;
        [SerializeField] private int maxHealth = 100;

        [Header("References")]
        private UnitMovementController movementController;

        // Состояние
        private int currentHealth;
        private float lastAttackTime;
        private Vector2Int currentGridPosition;

        // Свойства
        public UnitSize Size => unitSize;
        public Team Team => team;
        public float MovementSpeed => movementSpeed;
        public int AttackRange => attackRange;
        public int Damage => damage;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;
        
        public Vector2Int CurrentGridPosition
        {
            get => currentGridPosition;
            set => currentGridPosition = value;
        }

        public List<GridNode> CurrentPath { get; set; }
        public Unit CurrentTarget { get; set; }

        private void Awake()
        {
            movementController = GetComponent<UnitMovementController>();
            currentHealth = maxHealth;
        }

        private void Start()
        {
            // Инициализация позиции на сетке
            if (GridManager.Instance != null)
            {
                currentGridPosition = GridManager.Instance.GetGridPosition(transform.position);
                GridManager.Instance.OccupyArea(this, currentGridPosition);
            }
        }

        /// <summary>
        /// Получить все клетки, которые занимает юнит
        /// </summary>
        public List<Vector2Int> GetOccupiedCells()
        {
            return unitSize.GetAllOccupiedCells(currentGridPosition);
        }

        /// <summary>
        /// Получить список GridNode, которые занимает юнит
        /// </summary>
        public List<GridNode> GetOccupiedNodes()
        {
            var nodes = new List<GridNode>();
            var cells = GetOccupiedCells();

            foreach (var cell in cells)
            {
                var node = GridManager.Instance?.GetNode(cell);
                if (node != null)
                    nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// Проверить, может ли юнит атаковать цель
        /// </summary>
        public bool CanAttackTarget(Unit target)
        {
            if (target == null || target.IsDead)
                return false;

            if (target.Team == this.Team)
                return false;

            if (Time.time - lastAttackTime < attackCooldown)
                return false;

            return IsInAttackRange(target);
        }

        /// <summary>
        /// Проверить, находится ли цель в радиусе атаки
        /// Использует Chebyshev distance (диагональ разрешена)
        /// </summary>
        public bool IsInAttackRange(Unit target)
        {
            if (target == null)
                return false;

            var myCells = GetOccupiedCells();
            var targetCells = target.GetOccupiedCells();

            // Проверяем расстояние между всеми парами клеток
            foreach (var myCell in myCells)
            {
                foreach (var targetCell in targetCells)
                {
                    int distance = GetChebyshevDistance(myCell, targetCell);
                    if (distance <= attackRange)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Chebyshev distance (для диагонального движения)
        /// </summary>
        private int GetChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        /// <summary>
        /// Атаковать цель
        /// </summary>
        public void Attack(Unit target)
        {
            if (!CanAttackTarget(target))
                return;

            lastAttackTime = Time.time;
            target.TakeDamage(damage);

            Debug.Log($"{gameObject.name} атакует {target.gameObject.name} на {damage} урона!");

            // Вызов события атаки для анимации/эффектов
            OnAttackPerformed(target);
        }

        /// <summary>
        /// Получить урон
        /// </summary>
        public void TakeDamage(int damageAmount)
        {
            if (IsDead)
                return;

            currentHealth -= damageAmount;
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"{gameObject.name} получил {damageAmount} урона. HP: {currentHealth}/{maxHealth}");

            if (IsDead)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// Обработка смерти
        /// </summary>
        private void OnDeath()
        {
            Debug.Log($"{gameObject.name} погиб!");

            // Освобождаем занятые клетки
            if (GridManager.Instance != null)
            {
                GridManager.Instance.FreeArea(this);
            }

            // Уведомляем контроллер движения
            if (movementController != null)
            {
                movementController.OnUnitDied();
            }

            // Можно добавить анимацию смерти, эффекты и т.д.
            // Пока просто удаляем
            Destroy(gameObject, 0.5f);
        }

        /// <summary>
        /// Переместить юнит на новую позицию в сетке
        /// </summary>
        public void MoveToGridPosition(Vector2Int newPosition)
        {
            if (GridManager.Instance == null)
                return;

            // Освобождаем старую область
            GridManager.Instance.FreeArea(this);

            // Обновляем позицию
            currentGridPosition = newPosition;

            // Занимаем новую область
            GridManager.Instance.OccupyArea(this, newPosition);

            // Обновляем мировую позицию
            Vector3 worldPos = GridManager.Instance.GetWorldPosition(newPosition.x, newPosition.y);
            
            // Учитываем центр юнита
            Vector2 center = unitSize.GetCenter(newPosition);
            worldPos.x = center.x * GridManager.Instance.CellSize;
            worldPos.z = center.y * GridManager.Instance.CellSize;
            
            transform.position = worldPos;
        }

        /// <summary>
        /// Событие атаки (для переопределения в наследниках)
        /// </summary>
        protected virtual void OnAttackPerformed(Unit target)
        {
            // Здесь можно добавить анимацию, звуки, эффекты
        }

        private void OnDestroy()
        {
            // Освобождаем клетки при уничтожении
            if (GridManager.Instance != null)
            {
                GridManager.Instance.FreeArea(this);
            }
        }

        private void OnDrawGizmos()
        {
            if (GridManager.Instance == null)
                return;

            // Визуализация занимаемых клеток
            var cells = GetOccupiedCells();
            Gizmos.color = team == Team.Player ? Color.blue : Color.red;
            
            foreach (var cell in cells)
            {
                Vector3 worldPos = GridManager.Instance.GetWorldPosition(cell.x, cell.y);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
            }

            // Визуализация радиуса атаки
            Gizmos.color = Color.yellow;
            if (CurrentTarget != null)
            {
                Gizmos.DrawLine(transform.position, CurrentTarget.transform.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Показать радиус атаки
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange * 1.5f);
        }
    }
}

