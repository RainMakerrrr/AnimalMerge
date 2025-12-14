using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Чистый C# класс для pathfinding без MonoBehaviour
    /// Можно создавать несколько инстансов с разными настройками
    /// Легко тестировать и использовать без Unity Scene
    /// </summary>
    public class PathfindingSystemPure
    {
        // Настройки
        public int MaxIterations { get; set; }
        public bool UseDiagonalMovement { get; set; }
        public int DiagonalCost { get; set; }
        public int StraightCost { get; set; }

        private readonly Grid _grid;

        // Для хранения последнего найденного пути (для debug)
        public List<GridNode> LastFoundPath { get; private set; }

        /// <summary>
        /// Конструктор с настройками по умолчанию
        /// </summary>
        public PathfindingSystemPure()
        {
            MaxIterations = 1000;
            UseDiagonalMovement = true;
            DiagonalCost = 14;
            StraightCost = 10;
        }
        
        public PathfindingSystemPure(Grid grid)
        {
            MaxIterations = 1000;
            UseDiagonalMovement = true;
            DiagonalCost = 14;
            StraightCost = 10;
            _grid = grid;
        }

        /// <summary>
        /// Конструктор с кастомными настройками
        /// </summary>
        public PathfindingSystemPure(PathfindingConfig config)
        {
            MaxIterations = config.MaxIterations;
            UseDiagonalMovement = config.UseDiagonalMovement;
            DiagonalCost = config.DiagonalCost;
            StraightCost = config.StraightCost;
        }

        /// <summary>
        /// Найти путь от стартовой позиции до целевой для юнита
        /// </summary>
        public List<GridNode> FindPath(Unit unit, Vector2Int start, Vector2Int goal)
        {
            if (_grid == null)
            {
                Debug.LogError("GridManager is null");
                return null;
            }

            GridNode startNode = _grid.GetNode(start);
            GridNode goalNode = _grid.GetNode(goal);

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
        public List<GridNode> FindPath(Unit unit, GridNode startNode, GridNode goalNode)
        {
            if (unit == null || startNode == null || goalNode == null || _grid == null)
                return null;

            // Сброс данных A*
            _grid.ResetPathfindingData();

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
                    LastFoundPath = path;
                    Debug.Log($"Путь найден! Длина: {path.Count}, Итераций: {iterations}");
                    return path;
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // Проверяем всех соседей
                foreach (GridNode neighbor in _grid.GetNeighbors(currentNode))
                {
                    if (closedList.Contains(neighbor))
                        continue;

                    // Проверяем, может ли юнит занять эту позицию
                    if (!CanUnitFitAt(unit, neighbor, goalNode, _grid))
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
            LastFoundPath = null;
            return null;
        }

        /// <summary>
        /// Проверить, может ли юнит занять позицию с учетом его размера
        /// </summary>
        private bool CanUnitFitAt(Unit unit, GridNode node, GridNode goalNode, GridManager gridManager)
        {
            Vector2Int position = node.GridPosition;
            UnitSize size = unit.Size;

            // Проверяем все клетки, которые займет юнит
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    GridNode checkNode = gridManager.GetNode(position.x + x, position.y + y);

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
        private bool IsDiagonalMove(GridNode from, GridNode to)
        {
            return from.GridX != to.GridX && from.GridY != to.GridY;
        }

        /// <summary>
        /// Получить узел с наименьшей стоимостью из списка
        /// </summary>
        private GridNode GetLowestFCostNode(List<GridNode> nodeList)
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
        private int GetHeuristic(GridNode from, GridNode to)
        {
            int dx = Mathf.Abs(from.GridX - to.GridX);
            int dy = Mathf.Abs(from.GridY - to.GridY);

            return StraightCost * (dx + dy);
        }

        /// <summary>
        /// Восстановить путь от цели к старта
        /// </summary>
        private List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
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
        public Vector2Int FindNearestWalkablePosition(Unit unit, Vector2Int target, GridManager gridManager)
        {
            if (gridManager == null)
                return target;

            if (gridManager.IsAreaClear(target.x, target.y, unit.Size.Width, unit.Size.Height, unit))
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

                        if (gridManager.IsAreaClear(checkPos.x, checkPos.y,
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
        public int GetPathCost(List<GridNode> path)
        {
            if (path == null || path.Count == 0)
                return 0;

            return path[path.Count - 1].GCost;
        }
    }
}

