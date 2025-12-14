using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Система поиска пути с использованием A* алгоритма
    /// Поддерживает юниты разных размеров (1x1, 2x1, 2x2)
    /// </summary>
    public class PathfindingSystem : MonoBehaviour
    {
        public static PathfindingSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxIterations = 1000;
        [SerializeField] private bool useDiagonalMovement = true;
        [SerializeField] private int diagonalCost = 14; // Примерно √2 * 10
        [SerializeField] private int straightCost = 10;

        [Header("Debug")]
        [SerializeField] private bool showDebugPath = true;
        [SerializeField] private Color pathColor = Color.green;

        private List<GridNode> debugPath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Найти путь от стартовой позиции до целевой для юнита
        /// </summary>
        public List<GridNode> FindPath(Unit unit, Vector2Int start, Vector2Int goal)
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
        public List<GridNode> FindPath(Unit unit, GridNode startNode, GridNode goalNode)
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

            while (openList.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // Получаем ноду с наименьшей FCost
                GridNode currentNode = GetLowestFCostNode(openList);

                // Достигли цели
                if (currentNode == goalNode)
                {
                    var path = RetracePath(startNode, goalNode);
                    debugPath = path;
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
                    if (isDiagonal && !useDiagonalMovement)
                        continue;

                    // Расчет стоимости
                    int moveCost = isDiagonal ? diagonalCost : straightCost;
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
            debugPath = null;
            return null;
        }

        /// <summary>
        /// Проверить, может ли юнит занять позицию с учетом его размера
        /// </summary>
        private bool CanUnitFitAt(Unit unit, GridNode node, GridNode goalNode)
        {
            Vector2Int position = node.GridPosition;
            UnitSize size = unit.Size;

            // Проверяем все клетки, которые займет юнит
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    GridNode checkNode = GridManager.Instance.GetNode(position.x + x, position.y + y);

                    // Клетка не существует
                    if (checkNode == null)
                        return false;

                    // Целевая клетка всегда доступна (даже если занята врагом)
                    if (checkNode == goalNode)
                        continue;

                    // Проверяем доступность клетки
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

            // Manhattan distance
            return straightCost * (dx + dy);

            // Для диагонального движения можно использовать:
            // int min = Mathf.Min(dx, dy);
            // int max = Mathf.Max(dx, dy);
            // return diagonalCost * min + straightCost * (max - min);
        }

        /// <summary>
        /// Восстановить путь от цели к старту
        /// </summary>
        private List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
        {
            List<GridNode> path = new List<GridNode>();
            GridNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;

                // Защита от бесконечного цикла
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
        public Vector2Int FindNearestWalkablePosition(Unit unit, Vector2Int target)
        {
            if (GridManager.Instance == null)
                return target;

            // Если целевая позиция доступна - возвращаем её
            if (GridManager.Instance.IsAreaClear(target.x, target.y, unit.Size.Width, unit.Size.Height, unit))
                return target;

            // Ищем ближайшую доступную позицию по спирали
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
        public int GetPathCost(List<GridNode> path)
        {
            if (path == null || path.Count == 0)
                return 0;

            return path[path.Count - 1].GCost;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugPath || debugPath == null || debugPath.Count == 0)
                return;

            if (GridManager.Instance == null)
                return;

            Gizmos.color = pathColor;

            for (int i = 0; i < debugPath.Count; i++)
            {
                Vector3 worldPos = GridManager.Instance.GetWorldPosition(
                    debugPath[i].GridX, debugPath[i].GridY);
                
                Gizmos.DrawSphere(worldPos, 0.3f);

                if (i > 0)
                {
                    Vector3 prevPos = GridManager.Instance.GetWorldPosition(
                        debugPath[i - 1].GridX, debugPath[i - 1].GridY);
                    Gizmos.DrawLine(prevPos, worldPos);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

