using System.Collections.Generic;
using System.Linq;
using Code;
using Code.Animals;
using Code.Animals.Movement;
using Code.Pathfinding;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// MonoBehaviour managing the grid with Zenject DI support
    /// Handles grid initialization, coordinate conversion, and occupancy tracking
    /// Spawns GridCell prefabs for each cell
    /// </summary>
    public class GridManager : MonoBehaviour, IGridManager
    {
        [Header("Grid Configuration")]
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 10;
        [SerializeField] private float _cellSize = 1f;

        [Header("Visual Cells")]
        [SerializeField] private GridCell _cellPrefab;
        [SerializeField] private Transform _cellsParent;

        [Header("Debug Visualization")]
        [SerializeField] private bool _showDebugGizmos = false;
        [SerializeField] private Color _walkableColor = Color.green;
        [SerializeField] private Color _blockedColor = Color.red;
        [SerializeField] private Color _occupiedColor = Color.yellow;
        [SerializeField] private float _gizmoHeight = 0.1f;

        private GridCell[,] _cells;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;

        private void Awake()
        {
            InitializeGrid();
        }

        /// <summary>
        /// Initializes the grid with cells and spawns visual representations
        /// </summary>
        private void InitializeGrid()
        {
            _cells = new GridCell[_width, _height];

            // Create parent for cells if not assigned
            if (_cellsParent == null)
            {
                var parentObj = new GameObject("GridCells");
                parentObj.transform.SetParent(transform);
                parentObj.transform.localPosition = Vector3.zero;
                _cellsParent = parentObj.transform;
            }

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    // Create cell if prefab is assigned
                    if (_cellPrefab != null)
                    {
                        Vector3 worldPos = transform.position + new Vector3(
                            x * _cellSize,
                            0,
                            y * _cellSize);
                        GridCell cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, _cellsParent);
                        cell.Initialize(x, y, this, true);
                        _cells[x, y] = cell;
                    }
                }
            }

            Debug.Log($"[GridPathfinding] Grid initialized: {_width}x{_height}, CellSize: {_cellSize}");
        }

        public GridCell GetCell(int x, int y)
        {
            if (!IsInBounds(x, y))
                return null;

            return _cells[x, y];
        }

        public GridCell GetCell(Vector2Int position)
        {
            return GetCell(position.x, position.y);
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        public bool IsInBounds(Vector2Int position)
        {
            return IsInBounds(position.x, position.y);
        }

        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - transform.position;
            int x = Mathf.FloorToInt(localPosition.x / _cellSize);
            int y = Mathf.FloorToInt(localPosition.z / _cellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return transform.position + new Vector3(
                x * _cellSize,
                0,
                y * _cellSize
            );
        }

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return GridToWorld(gridPosition.x, gridPosition.y);
        }

        /// <summary>
        /// Converts grid coordinates to world position at cell CENTER
        /// </summary>
        public Vector3 GridToWorldCenter(int x, int y)
        {
            return transform.position + new Vector3(
                x * _cellSize + _cellSize * 0.5f,  // Add half cell to X
                0,
                y * _cellSize + _cellSize * 0.5f   // Add half cell to Z
            );
        }

        /// <summary>
        /// Converts grid coordinates to world position at cell CENTER
        /// </summary>
        public Vector3 GridToWorldCenter(Vector2Int gridPosition)
        {
            return GridToWorldCenter(gridPosition.x, gridPosition.y);
        }

        /// <summary>
        /// Gets the world position for a unit of given size and direction at grid position.
        /// Returns the center position of the unit by calculating the center of all occupied cells.
        /// Grid position is the bottom-left cell of the unit's footprint.
        /// </summary>
        public Vector3 GetUnitWorldPosition(Vector2Int gridPosition, UnitSize size, Direction direction)
        {
            // Get all cells this unit occupies
            var occupiedCells = GetOccupiedCellsInternal(gridPosition, size, direction);

            if (occupiedCells.Count == 0)
            {
                // Fallback to single cell center if no cells found
                return GridToWorldCenter(gridPosition);
            }

            // Calculate the average position of all cell centers
            Vector3 sum = Vector3.zero;
            foreach (var cell in occupiedCells)
            {
                sum += GridToWorldCenter(cell.X, cell.Y);
            }

            return sum / occupiedCells.Count;
        }

        public bool CanPlaceUnit(Vector2Int position, UnitSize size, Direction direction, bool ignoreOccupied = false)
        {
            var cells = GetOccupiedCellsInternal(position, size, direction);

            foreach (var cell in cells)
            {
                if (cell == null || !cell.IsWalkable)
                    return false;

                if (!ignoreOccupied && cell.IsOccupied)
                    return false;
            }

            return true;
        }

        public List<GridCell> GetOccupiedCells(Vector2Int position, UnitSize size, Direction direction)
        {
            return GetOccupiedCellsInternal(position, size, direction);
        }

        /// <summary>
        /// Gets the neighbor cells (additional cells excluding the base position cell)
        /// that a unit occupies based on its size and direction.
        /// This matches the old PathNode.GetNeighbours behavior.
        /// Automatically adjusts position if unit would extend beyond grid bounds.
        /// NOTE: Removes the cell at the ORIGINAL position, not the adjusted position,
        /// so that calling code can mark the original position as occupied and all returned
        /// cells as neighbors.
        /// </summary>
        public List<GridCell> GetNeighborCells(Vector2Int position, UnitSize size, Direction direction)
        {
            // Get all cells (internally adjusts position to fit bounds)
            var allCells = GetOccupiedCellsInternal(position, size, direction);

            // Remove the cell at the ORIGINAL position as the "base" cell
            // This ensures that when TestEnemiesSpawner marks gridCell (original) and neighbors,
            // all actual occupied cells get marked correctly
            var baseCellAtOriginalPosition = GetCell(position);
            if (baseCellAtOriginalPosition != null && allCells.Contains(baseCellAtOriginalPosition))
            {
                allCells.Remove(baseCellAtOriginalPosition);
            }

            return allCells;
        }

        /// <summary>
        /// Adjusts position to ensure all cells of a unit fit within grid bounds.
        /// If a unit would extend beyond grid boundaries, shifts the position to keep it in bounds.
        /// </summary>
        private Vector2Int AdjustPositionToFitBounds(Vector2Int position, UnitSize size, Direction direction)
        {
            int width = size.Width;
            int height = size.Height;

            int adjustedX = position.x;
            int adjustedY = position.y;

            // For 2x1 units, determine actual dimensions based on direction
            int xExtent = width;
            int yExtent = height;

            if ((width == 1 && height == 2) || (width == 2 && height == 1))
            {
                if (direction == Direction.East || direction == Direction.West)
                {
                    // Horizontal orientation: swap dimensions
                    xExtent = height;
                    yExtent = width;
                }
            }

            // Clamp position so unit fits within bounds
            // If position.x + xExtent > _width, adjust position.x
            if (position.x + xExtent > _width)
            {
                adjustedX = _width - xExtent;
            }

            // If position.y + yExtent > _height, adjust position.y
            if (position.y + yExtent > _height)
            {
                adjustedY = _height - yExtent;
            }

            // Ensure position is not negative
            adjustedX = Mathf.Max(0, adjustedX);
            adjustedY = Mathf.Max(0, adjustedY);

            return new Vector2Int(adjustedX, adjustedY);
        }

        /// <summary>
        /// Calculates which cells a unit occupies based on size and direction.
        /// Automatically adjusts position if unit would extend beyond grid bounds.
        /// </summary>
        private List<GridCell> GetOccupiedCellsInternal(Vector2Int position, UnitSize size, Direction direction)
        {
            // Adjust position to ensure all cells fit within bounds
            position = AdjustPositionToFitBounds(position, size, direction);

            var cells = new List<GridCell>();
            int width = size.Width;
            int height = size.Height;

            // For 2x1 units, direction affects which dimension is which
            // North/South: unit is vertical (height along Y axis)
            // East/West: unit is horizontal (height along X axis)
            if ((width == 1 && height == 2) || (width == 2 && height == 1))
            {
                if (direction == Direction.North || direction == Direction.South)
                {
                    // Vertical orientation: (x,y) and (x,y+1)
                    for (int dy = 0; dy < height; dy++)
                    {
                        var cell = GetCell(position.x, position.y + dy);
                        if (cell != null)
                            cells.Add(cell);
                    }
                }
                else // East or West
                {
                    // Horizontal orientation: (x,y) and (x+1,y)
                    for (int dx = 0; dx < height; dx++)  // Using height as horizontal extent
                    {
                        var cell = GetCell(position.x + dx, position.y);
                        if (cell != null)
                            cells.Add(cell);
                    }
                }
            }
            else
            {
                // For square units (1x1, 2x2), direction doesn't matter
                for (int dx = 0; dx < width; dx++)
                {
                    for (int dy = 0; dy < height; dy++)
                    {
                        var cell = GetCell(position.x + dx, position.y + dy);
                        if (cell != null)
                            cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        public void SetOccupied(Vector2Int position, UnitSize size, Direction direction, object unit)
        {
            var cells = GetOccupiedCellsInternal(position, size, direction);

            foreach (var cell in cells)
            {
                if (cell != null)
                {
                    cell.IsOccupied = true;
                    cell.OccupyingUnit = unit;
                    cell.UpdateVisual();
                }
            }
        }

        public void ClearOccupied(Vector2Int position, UnitSize size, Direction direction)
        {
            var cells = GetOccupiedCellsInternal(position, size, direction);

            foreach (var cell in cells)
            {
                if (cell != null)
                {
                    cell.IsOccupied = false;
                    cell.OccupyingUnit = null;
                    cell.UpdateVisual();
                }
            }
        }

        public void SetWalkable(int x, int y, bool walkable)
        {
            var cell = GetCell(x, y);
            if (cell != null)
            {
                cell.IsWalkable = walkable;
                cell.UpdateVisual();
            }
        }

        /// <summary>
        /// Updates all cell visuals
        /// </summary>
        public void RefreshAllVisuals()
        {
            if (_cells == null) return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _cells[x, y]?.UpdateVisual();
                }
            }
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos)
                return;

            if (_cells == null)
            {
                // Draw placeholder grid in editor before play mode
                DrawPlaceholderGrid();
                return;
            }

            // Draw actual grid with cell states
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var cell = _cells[x, y];
                    if (cell == null) continue;

                    Vector3 worldPos = GridToWorld(x, y);

                    // Choose color based on cell state
                    Color cellColor;
                    if (!cell.IsWalkable)
                        cellColor = _blockedColor;
                    else if (cell.IsOccupied)
                        cellColor = _occupiedColor;
                    else
                        cellColor = _walkableColor;

                    cellColor.a = 0.3f;
                    Gizmos.color = cellColor;

                    // Draw cell cube
                    Gizmos.DrawCube(worldPos + Vector3.up * _gizmoHeight, new Vector3(_cellSize * 0.9f, 0.01f, _cellSize * 0.9f));

                    // Draw cell border
                    Gizmos.color = Color.white * 0.5f;
                    DrawCellBorder(worldPos, _cellSize);
                }
            }
        }

        private void DrawPlaceholderGrid()
        {
            Gizmos.color = Color.white * 0.3f;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 worldPos = transform.position + new Vector3(
                        x * _cellSize + _cellSize * 0.5f,
                        _gizmoHeight,
                        y * _cellSize + _cellSize * 0.5f
                    );

                    DrawCellBorder(worldPos, _cellSize);
                }
            }
        }

        private void DrawCellBorder(Vector3 center, float size)
        {
            float halfSize = size * 0.5f;
            Vector3 offset = Vector3.up * _gizmoHeight;

            Vector3 bottomLeft = center + new Vector3(-halfSize, 0, -halfSize) + offset;
            Vector3 bottomRight = center + new Vector3(halfSize, 0, -halfSize) + offset;
            Vector3 topRight = center + new Vector3(halfSize, 0, halfSize) + offset;
            Vector3 topLeft = center + new Vector3(-halfSize, 0, halfSize) + offset;

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }

        #endregion

        #region Animal Placement Helpers

        /// <summary>
        /// Checks if there are enough free cells to place an animal of the given type
        /// </summary>
        public bool HasCellFor(AnimalType animalType)
        {
            List<GridCell> freeCells = GetSortedFreeCells();

            // Determine required cell count based on animal type
            int requiredCells = animalType switch
            {
                AnimalType.Elephant => 2,  // 2x1 or larger
                _ => 1  // All others need at least 1 cell
            };

            return freeCells.Count >= requiredCells;
        }

        /// <summary>
        /// Places an animal on the first available cell in the grid
        /// </summary>
        public void PlaceOnGrid(AnimalMovement animal)
        {
            if (animal == null)
            {
                Debug.LogWarning("[GridManager] Cannot place null animal");
                return;
            }

            List<GridCell> freeCells = GetSortedFreeCells();
            GridCell targetCell = freeCells.FirstOrDefault();

            if (targetCell == null)
            {
                Debug.LogWarning("[GridManager] No free cells available for placement");
                return;
            }

            UnitSize unitSize = animal.ObjectSizeType.ToUnitSize();
            Direction direction = animal.Direction.ToDirection();

            // Get neighbor cells (excluding the base targetCell)
            List<GridCell> neighbourCells = GetNeighborCells(
                targetCell.GridPosition,
                unitSize,
                direction
            );

            // Place the animal
            animal.Place(targetCell.WorldPosition);
            //todo fix
            animal.SetCurrentNode(targetCell);

            // Mark base cell as not walkable
            targetCell.IsWalkable = false;
            targetCell.UpdateVisual();

            // Mark neighbor cells as not walkable (if any)
            if (neighbourCells.Count > 0)
            {
                neighbourCells.ForEach(cell =>
                {
                    cell.IsWalkable = false;
                    cell.UpdateVisual();
                });

                //todo fix
                animal.FillNodes(neighbourCells);
            }

            SetOccupied(targetCell.GridPosition, unitSize, direction, animal);
        }

        /// <summary>
        /// Gets all free cells sorted by position (bottom-left to top-right, row by row)
        /// Only returns cells in the bottom two rows (y = 0 or y = 1) that are walkable and can be placed on
        /// </summary>
        private List<GridCell> GetSortedFreeCells()
        {
            List<GridCell> freeCells = new List<GridCell>();

            if (_cells == null) return freeCells;

            // Collect all cells from the grid
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    GridCell cell = _cells[x, y];
                    if (cell != null && cell.IsWalkable && !cell.IsOccupied && cell.CanPlace &&
                        (y == 0 || y == 1))  // Only bottom two rows
                    {
                        freeCells.Add(cell);
                    }
                }
            }

            // Sort by Y first (ascending), then by X (ascending)
            // This gives us bottom-left to top-right ordering
            return freeCells
                .OrderBy(cell => cell.Y)
                .ThenBy(cell => cell.X)
                .ToList();
        }

        #endregion
    }
}
