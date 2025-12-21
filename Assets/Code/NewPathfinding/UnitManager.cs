using System.Collections.Generic;
using UnityEngine;

namespace Code.NewPathfinding
{
    public class UnitManager : MonoBehaviour
    {
        public static UnitManager Instance { get; private set; }

        [SerializeField] private GridManager _gridManager;

        // Список всех активных юнитов на поле
        private readonly List<Unit> _activeUnits = new List<Unit>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Создает юнита на сетке
        /// </summary>
        public void SpawnUnit(Unit unitPrefab, int x, int y, bool isPlayerTeam)
        {
            // Проверяем, свободна ли клетка (с учетом размера юнита, который прописан в префабе)
            // Для этого нам нужно временно получить размер из префаба
            Vector2Int unitSize = unitPrefab.Size;

            if (!IsAreaWalkable(x, y, unitSize))
            {
                Debug.LogWarning($"Cannot spawn unit at {x},{y}: Area blocked.");
                return;
            }

            Vector3 spawnPosition = _gridManager.GetWorldPosition(x, y);

            // Создаем объект
            Unit newUnit = Instantiate(unitPrefab, spawnPosition, Quaternion.identity);

            // Инициализируем (передаем координаты сетки и команду)
            newUnit.Initialize(x, y, isPlayerTeam);

            _activeUnits.Add(newUnit);
        }

        /// <summary>
        /// Удаляет юнита из списка (вызывается при смерти)
        /// </summary>
        public void UnregisterUnit(Unit unit)
        {
            if (_activeUnits.Contains(unit))
            {
                _activeUnits.Remove(unit);
            }
        }

        /// <summary>
        /// Находит ближайшего врага для запрашивающего юнита
        /// </summary>
        public Unit GetNearestEnemy(Unit requestor)
        {
            Unit nearest = null;
            float minDistance = float.MaxValue;

            foreach (var unit in _activeUnits)
            {
                // Пропускаем самого себя и союзников
                if (unit == requestor || unit.IsPlayerTeam == requestor.IsPlayerTeam)
                    continue;

                float distance = Vector3.Distance(requestor.transform.position, unit.transform.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = unit;
                }
            }

            return nearest;
        }

        // Вспомогательный метод проверки перед спавном
        private bool IsAreaWalkable(int startX, int startY, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    GridNode node = _gridManager.GetNode(startX + x, startY + y);
                    if (node == null || !node.IsWalkable) return false;
                }
            }

            return true;
        }
    }
}