using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Singleton для управления сеткой
    /// Отвечает за создание, доступ к клеткам и управление занятостью
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 10;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 gridOrigin = Vector2.zero;

        [Header("Node Prefab")]
        [SerializeField] private GameObject nodePrefab;

        [Header("Debug")]
        [SerializeField] private bool autoGenerateOnStart = true;
        [SerializeField] private bool showGridGizmos = true;

        private GridNode[,] grid;
        private Dictionary<Vector2Int, GridNode> gridDictionary;

        public int Width => gridWidth;
        public int Height => gridHeight;
        public float CellSize => cellSize;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                GenerateGrid();
            }
        }

        /// <summary>
        /// Генерация сетки
        /// </summary>
        public void GenerateGrid()
        {
            ClearGrid();

            grid = new GridNode[gridWidth, gridHeight];
            gridDictionary = new Dictionary<Vector2Int, GridNode>();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CreateNode(x, y);
                }
            }

            Debug.Log($"Grid generated: {gridWidth}x{gridHeight} = {gridWidth * gridHeight} nodes");
        }

        /// <summary>
        /// Создать одну клетку
        /// </summary>
        private void CreateNode(int x, int y)
        {
            Vector3 worldPosition = GetWorldPosition(x, y);
            
            GameObject nodeObject;
            if (nodePrefab != null)
            {
                nodeObject = Instantiate(nodePrefab, worldPosition, Quaternion.identity, transform);
            }
            else
            {
                // Создаем базовый объект с визуализацией
                nodeObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                nodeObject.transform.position = worldPosition;
                nodeObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                nodeObject.transform.parent = transform;
            }

            nodeObject.name = $"Node_{x}_{y}";

            GridNode node = nodeObject.GetComponent<GridNode>();
            if (node == null)
                node = nodeObject.AddComponent<GridNode>();

            node.GridX = x;
            node.GridY = y;

            grid[x, y] = node;
            gridDictionary[new Vector2Int(x, y)] = node;
        }

        /// <summary>
        /// Очистить сетку
        /// </summary>
        public void ClearGrid()
        {
            if (grid != null)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        if (grid[x, y] != null)
                            Destroy(grid[x, y].gameObject);
                    }
                }
            }

            grid = null;
            gridDictionary?.Clear();
        }

        /// <summary>
        /// Получить клетку по координатам сетки
        /// </summary>
        public GridNode GetNode(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return null;

            return grid[x, y];
        }

        public GridNode GetNode(Vector2Int gridPos)
        {
            return GetNode(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Получить соседей клетки (8 направлений)
        /// </summary>
        public List<GridNode> GetNeighbors(GridNode node)
        {
            var neighbors = new List<GridNode>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.GridX + x;
                    int checkY = node.GridY + y;

                    if (IsValidPosition(checkX, checkY))
                    {
                        neighbors.Add(grid[checkX, checkY]);
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Проверить, валидна ли позиция в сетке
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return IsValidPosition(pos.x, pos.y);
        }

        /// <summary>
        /// Получить все клетки в области (для юнитов разных размеров)
        /// </summary>
        public List<GridNode> GetNodesInArea(int x, int y, int width, int height)
        {
            var nodes = new List<GridNode>();

            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var node = GetNode(x + dx, y + dy);
                    if (node != null)
                        nodes.Add(node);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Проверить, свободна ли область для юнита
        /// </summary>
        public bool IsAreaClear(int x, int y, int width, int height, Unit ignoreUnit = null)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var node = GetNode(x + dx, y + dy);
                    
                    if (node == null)
                        return false;

                    if (!node.IsAvailable(ignoreUnit))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Занять область для юнита
        /// </summary>
        public void OccupyArea(Unit unit, Vector2Int bottomLeft)
        {
            var cells = unit.Size.GetAllOccupiedCells(bottomLeft);
            
            foreach (var cell in cells)
            {
                var node = GetNode(cell);
                if (node != null)
                    node.Occupy(unit);
            }
        }

        /// <summary>
        /// Освободить область юнита
        /// </summary>
        public void FreeArea(Unit unit)
        {
            var cells = unit.GetOccupiedCells();
            
            foreach (var cell in cells)
            {
                var node = GetNode(cell);
                if (node != null && node.OccupyingUnit == unit)
                    node.Free();
            }
        }

        /// <summary>
        /// Конвертировать координаты сетки в мировые
        /// </summary>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(
                gridOrigin.x + x * cellSize + cellSize * 0.5f,
                0f,
                gridOrigin.y + y * cellSize + cellSize * 0.5f
            );
        }

        /// <summary>
        /// Конвертировать мировые координаты в координаты сетки
        /// </summary>
        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((worldPosition.z - gridOrigin.y) / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Сброс всех данных A* в сетке
        /// </summary>
        public void ResetPathfindingData()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y].ResetPathfindingData();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGridGizmos) return;

            Gizmos.color = Color.gray;
            
            // Рисуем границы сетки
            Vector3 bottomLeft = new Vector3(gridOrigin.x, 0, gridOrigin.y);
            Vector3 bottomRight = new Vector3(gridOrigin.x + gridWidth * cellSize, 0, gridOrigin.y);
            Vector3 topLeft = new Vector3(gridOrigin.x, 0, gridOrigin.y + gridHeight * cellSize);
            Vector3 topRight = new Vector3(gridOrigin.x + gridWidth * cellSize, 0, gridOrigin.y + gridHeight * cellSize);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

