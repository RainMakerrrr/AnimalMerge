using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// MonoBehaviour компонент для каждой клетки сетки
    /// Содержит данные для A* алгоритма и информацию о занятости
    /// </summary>
    public class GridNode : MonoBehaviour
    {
        [Header("Grid Position")]
        public int GridX;
        public int GridY;

        [Header("Walkability")]
        public bool IsWalkable = true;
        public bool IsOccupied = false;
        public Unit OccupyingUnit = null;

        [Header("Reservation System")]
        public Unit ReservedBy = null;
        public float ReservationTime = 0f;

        [Header("A* Data")]
        public int GCost; // Стоимость от старта
        public int HCost; // Эвристическая стоимость до цели
        public GridNode Parent; // Родитель для восстановления пути

        [Header("Visualization")]
        [SerializeField] private Color walkableColor = Color.white;
        [SerializeField] private Color unwalkableColor = Color.red;
        [SerializeField] private Color occupiedColor = Color.yellow;
        [SerializeField] private Color reservedColor = Color.cyan;
        [SerializeField] private bool showDebugGizmos = true;

        public int FCost => GCost + HCost;

        public Vector2Int GridPosition => new Vector2Int(GridX, GridY);

        private void Start()
        {
            // Визуализация клетки
            UpdateVisual();
        }

        /// <summary>
        /// Сброс A* данных перед новым поиском пути
        /// </summary>
        public void ResetPathfindingData()
        {
            GCost = int.MaxValue;
            HCost = 0;
            Parent = null;
        }

        /// <summary>
        /// Занять клетку юнитом
        /// </summary>
        public void Occupy(Unit unit)
        {
            IsOccupied = true;
            OccupyingUnit = unit;
            UpdateVisual();
        }

        /// <summary>
        /// Освободить клетку
        /// </summary>
        public void Free()
        {
            IsOccupied = false;
            OccupyingUnit = null;
            UpdateVisual();
        }

        /// <summary>
        /// Зарезервировать клетку для юнита
        /// </summary>
        public bool TryReserve(Unit unit)
        {
            if (ReservedBy != null && ReservedBy != unit)
                return false;

            ReservedBy = unit;
            ReservationTime = Time.time;
            UpdateVisual();
            return true;
        }

        /// <summary>
        /// Снять резервацию
        /// </summary>
        public void ReleaseReservation(Unit unit)
        {
            if (ReservedBy == unit)
            {
                ReservedBy = null;
                ReservationTime = 0f;
                UpdateVisual();
            }
        }

        /// <summary>
        /// Проверить, доступна ли клетка для движения
        /// </summary>
        public bool IsAvailable(Unit requester = null)
        {
            if (!IsWalkable) return false;
            if (IsOccupied && OccupyingUnit != requester) return false;
            if (ReservedBy != null && ReservedBy != requester) return false;
            return true;
        }

        /// <summary>
        /// Обновить визуальное представление
        /// </summary>
        private void UpdateVisual()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (!IsWalkable)
                    spriteRenderer.color = unwalkableColor;
                else if (IsOccupied)
                    spriteRenderer.color = occupiedColor;
                else if (ReservedBy != null)
                    spriteRenderer.color = reservedColor;
                else
                    spriteRenderer.color = walkableColor;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = !IsWalkable ? unwalkableColor : 
                          IsOccupied ? occupiedColor : 
                          ReservedBy != null ? reservedColor : 
                          walkableColor;
            
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        }

        private void OnDrawGizmosSelected()
        {
            // Показать информацию о клетке
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"({GridX},{GridY})\nF:{FCost} G:{GCost} H:{HCost}"
            );
#endif
        }
    }
}

