using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Статический класс для A* pathfinding без MonoBehaviour
    /// Легковесная версия, не требует GameObject в сцене
    /// </summary>
    public static class PathfindingSystemStatic
    {
        // Настройки (можно вынести в отдельный класс конфигурации)
        public static int MaxIterations { get; set; } = 1000;
        public static bool UseDiagonalMovement { get; set; } = true;
        public static int DiagonalCost { get; set; } = 14;
        public static int StraightCost { get; set; } = 10;

        /// <summary>
        /// Найти путь от стартовой позиции до целевой для юнита
        /// </summary>
        public static List<GridNode> FindPath(Unit unit, Vector2Int start, Vector2Int goal)
        {
            if (GridManager.Instance == null)
            {
                Debug.LogError("GridManager не найден!");
                return null;
            }

            GridNode startNode = GridManager.Instance.GetNode(start);
            GridNode goalNode = GridManager.Instance.GetNode(goal);

            if (startNode == null || goalNode == null)
            {
                Debug.LogWarning("Стартовая или целевая позиция не валидна!");
                return null;
            }

            return FindPath(unit, startNode, goalNode);
        }

        /// <summary>
        /// Найти путь от стартового узла до целевого
        /// </summary>
        public static List<GridNode> FindPath(Unit unit, GridNode startNode, GridNode goalNode)
        {
            if (unit == null || startNode == null || goalNode == null)
                return null;

            // Сброс данных A*
            GridManager.Instance.ResetPathfindingData();

            // Open и Closed списки
            List<GridNode> openList = new List<GridNode>();
            HashSet<GridNode> closedList = new HashSet<GridNode>();

            // Инициализация стартовой ноды
            startNode.GCost = 0;
            startNode.HCost = GetHeuristic(startNode, goalNode);
            openList.Add(startNode);

            int iterations = 0;

            while (openList.Count > 0 && iterations < MaxIterations)
            {
                iterations++;

                // Получаем ноду с наименьшей FCost
                GridNode currentNode = GetLowestFCostNode(openList);

                // Достигли цели
                if (currentNode == goalNode)
                {
                    var path = RetracePath(startNode, goalNode);
                    Debug.Log($"Путь найден! Длина: {path.Count}, Итераций: {iterations}");
                    return path;
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // Проверяем всех соседей
                foreach (GridNode neighbor in GridManager.Instance.GetNeighbors(currentNode))
                {
                    if (closedList.Contains(neighbor))
                        continue;

                    // Проверяем, может ли юнит занять эту позицию
                    if (!CanUnitFitAt(unit, neighbor, goalNode))
                        continue;

                    // Диагональное движение
                    bool isDiagonal = IsDiagonalMove(currentNode, neighbor);
                    if (isDiagonal && !UseDiagonalMovement)
                        continue;

                    // Расчет стоимости
                    int moveCost = isDiagonal ? DiagonalCost : StraightCost;
                    int newGCost = currentNode.GCost + moveCost;

                    // Если нашли более короткий путь
                    if (newGCost < neighbor.GCost)
                    {
                        neighbor.GCost = newGCost;
                        neighbor.HCost = GetHeuristic(neighbor, goalNode);
                        neighbor.Parent = currentNode;

                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            // Путь не найден
            Debug.LogWarning($"Путь не найден! Итераций: {iterations}");
            return null;
        }

        /// <summary>
        /// Проверить, может ли юнит занять позицию с учетом его размера
        /// </summary>
        private static bool CanUnitFitAt(Unit unit, GridNode node, GridNode goalNode)
        {
            Vector2Int position = node.GridPosition;
            UnitSize size = unit.Size;

            // Проверяем все клетки, которые займет юнит
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    GridNode checkNode = GridManager.Instance.GetNode(position.x + x, position.y + y);

                    if (checkNode == null)
                        return false;

                    if (checkNode == goalNode)
                        continue;

                    if (!checkNode.IsAvailable(unit))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Проверить, является ли движение диагональным
        /// </summary>
        private static bool IsDiagonalMove(GridNode from, GridNode to)
        {
            return from.GridX != to.GridX && from.GridY != to.GridY;
        }

        /// <summary>
        /// Получить узел с наименьшей стоимостью из списка
        /// </summary>
        private static GridNode GetLowestFCostNode(List<GridNode> nodeList)
        {
            GridNode lowest = nodeList[0];

            for (int i = 1; i < nodeList.Count; i++)
            {
                if (nodeList[i].FCost < lowest.FCost ||
                    (nodeList[i].FCost == lowest.FCost && nodeList[i].HCost < lowest.HCost))
                {
                    lowest = nodeList[i];
                }
            }

            return lowest;
        }

        /// <summary>
        /// Эвристическая функция (Manhattan distance)
        /// </summary>
        private static int GetHeuristic(GridNode from, GridNode to)
        {
            int dx = Mathf.Abs(from.GridX - to.GridX);
            int dy = Mathf.Abs(from.GridY - to.GridY);

            return StraightCost * (dx + dy);
        }

        /// <summary>
        /// Восстановить путь от цели к старту
        /// </summary>
        private static List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
        {
            List<GridNode> path = new List<GridNode>();
            GridNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;

                if (currentNode == null)
                {
                    Debug.LogError("Путь прерван - нет родителя!");
                    break;
                }
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Найти ближайшую доступную позицию к цели
        /// </summary>
        public static Vector2Int FindNearestWalkablePosition(Unit unit, Vector2Int target)
        {
            if (GridManager.Instance == null)
                return target;

            if (GridManager.Instance.IsAreaClear(target.x, target.y, unit.Size.Width, unit.Size.Height, unit))
                return target;

            int maxRadius = 10;
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                            continue;

                        Vector2Int checkPos = new Vector2Int(target.x + x, target.y + y);

                        if (GridManager.Instance.IsAreaClear(checkPos.x, checkPos.y,
                            unit.Size.Width, unit.Size.Height, unit))
                        {
                            return checkPos;
                        }
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// Получить стоимость пути
        /// </summary>
        public static int GetPathCost(List<GridNode> path)
        {
            if (path == null || path.Count == 0)
                return 0;

            return path[path.Count - 1].GCost;
        }
    }

    /// <summary>
    /// Класс конфигурации для PathfindingSystem (опционально)
    /// Позволяет иметь разные настройки для разных ситуаций
    /// </summary>
    public class PathfindingConfig
    {
        public int MaxIterations { get; set; } = 1000;
        public bool UseDiagonalMovement { get; set; } = true;
        public int DiagonalCost { get; set; } = 14;
        public int StraightCost { get; set; } = 10;

        public static PathfindingConfig Default => new PathfindingConfig();
        
        public static PathfindingConfig Fast => new PathfindingConfig
        {
            MaxIterations = 500,
            UseDiagonalMovement = true
        };

        public static PathfindingConfig Precise => new PathfindingConfig
        {
            MaxIterations = 2000,
            UseDiagonalMovement = true
        };
    }
}

