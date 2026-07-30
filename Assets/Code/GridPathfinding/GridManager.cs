using System.Collections.Generic;
using System.Linq;
using Code;
using Code.Animals;
using Code.Animals.Movement;
using Code.GridPathfinding.Config;
using Code.Pathfinding;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// MonoBehaviour managing the grid with Zenject DI support
    /// Handles grid initialization, coordinate conversion, and occupancy tracking
    /// Spawns GridCell prefabs for each cell
    /// </summary>
    // Runs ahead of the default order so the grid exists before GameBootstrapper.Awake()
    // drives the whole bootstrap — enemy spawning included — in one synchronous pass.
    // Still after Zenject's SceneContext, which sits far lower.
    [DefaultExecutionOrder(-100)]
    public class GridManager : MonoBehaviour, IGridManager
    {
        [Header("Grid Configuration")]
        [SerializeField] private GridConfig _config;

        [Header("Visual Cells")]
        [SerializeField] private GridCell _cellPrefab;
        [SerializeField] private Transform _cellsParent;

        [Header("Debug Visualization")]
        [SerializeField] private bool _showDebugGizmos = false;
        [SerializeField] private Color _walkableColor = Color.green;
        [SerializeField] private Color _blockedColor = Color.red;
        [SerializeField] private float _gizmoHeight = 0.1f;

        private GridCell[,] _cells;
        private int _runtimeWidth;
        private int _runtimeHeight;
        private float _runtimeCellSize;
        private bool _dimensionsInitialized;

        // Set by Rebuild(): the caller-supplied dimensions must win over the assigned config
        // until the next Awake, otherwise the edit-mode branch of EnsureDimensions() would
        // silently restore the asset values while the grid is being built.
        private bool _hasRuntimeOverride;

        public int Width
        {
            get
            {
                EnsureDimensions();
                return _runtimeWidth;
            }
        }

        public int Height
        {
            get
            {
                EnsureDimensions();
                return _runtimeHeight;
            }
        }

        public float CellSize
        {
            get
            {
                EnsureDimensions();
                return _runtimeCellSize;
            }
        }

        private void Awake()
        {
            if (_config == null)
                Debug.LogError($"[GridManager] GridConfig is not assigned on '{name}', falling back to default grid size", this);

            _hasRuntimeOverride = false;
            ApplyConfig();
            EnsureGridInitialized();
        }

        private void EnsureGridInitialized()
        {
            if (_cells != null)
                return;

            if (!Application.isPlaying)
                return;

            InitializeGrid();
        }

        /// <summary>
        /// Seeds the runtime dimensions from the config before they are read.
        /// </summary>
        private void EnsureDimensions()
        {
#if UNITY_EDITOR
            // OnDrawGizmos runs in edit mode long before Awake, and the config asset can be
            // tweaked while the scene is open, so outside play mode the values are re-read on
            // every access to keep the drawn grid in sync with the asset.
            // Dimensions supplied through Rebuild() are the one exception: they replace the
            // asset for this session, so re-reading the config would both report and build the
            // wrong size (InitializeGrid consumes these very properties).
            if (!Application.isPlaying && !_hasRuntimeOverride)
            {
                ApplyConfig();
                return;
            }
#endif

            if (!_dimensionsInitialized)
                ApplyConfig();
        }

        private void ApplyConfig()
        {
            _runtimeWidth = _config != null ? _config.Width : GridConfig.DefaultWidth;
            _runtimeHeight = _config != null ? _config.Height : GridConfig.DefaultHeight;
            _runtimeCellSize = _config != null ? _config.CellSize : GridConfig.DefaultCellSize;
            _dimensionsInitialized = true;
        }

        /// <summary>
        /// Initializes the grid with cells and spawns visual representations
        /// </summary>
        private void InitializeGrid()
        {
            _cells = new GridCell[Width, Height];

            // Create parent for cells if not assigned
            if (_cellsParent == null)
            {
                var parentObj = new GameObject("GridCells");
                parentObj.transform.SetParent(transform);
                parentObj.transform.localPosition = Vector3.zero;
                _cellsParent = parentObj.transform;
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Create cell if prefab is assigned
                    if (_cellPrefab != null)
                    {
                        Vector3 worldPos = transform.position + new Vector3(
                            x * CellSize,
                            0,
                            y * CellSize);
                        GridCell cell = Instantiate(_cellPrefab, worldPos, Quaternion.identity, _cellsParent);
                        cell.SetSize(CellSize);
                        cell.Initialize(x, y, this, true);
                        _cells[x, y] = cell;
                    }
                }
            }

            Debug.Log($"[GridPathfinding] Grid initialized: {Width}x{Height}, CellSize: {CellSize}");
        }

        /// <summary>
        /// Rebuilds the grid with new dimensions at runtime.
        /// WARNING: destroys every existing cell, so any external reference to an IGridCell
        /// becomes invalid — AnimalMovement.CurrentPathNode, the nodes passed to FillNodes(),
        /// and MoveRangeHighlighter's cached cells all dangle afterwards.
        /// Only safe to call while no units are placed on the grid; otherwise the caller
        /// is responsible for re-placing them.
        /// The new dimensions override the assigned GridConfig for this session only —
        /// they are never written back to the asset, and the next Awake drops the override.
        /// </summary>
        public void Rebuild(int width, int height, float cellSize)
        {
            ClearCells();

            _runtimeWidth = width;
            _runtimeHeight = height;
            _runtimeCellSize = cellSize;
            _dimensionsInitialized = true;
            _hasRuntimeOverride = true;

            InitializeGrid();
        }

        /// <summary>
        /// Destroys all spawned cell instances and drops the grid array
        /// </summary>
        private void ClearCells()
        {
            if (_cells == null) return;

            foreach (var cell in _cells)
            {
                if (cell == null) continue;

                // Destroy() is deferred to the end of the frame, so the old colliders would stay
                // live for one more frame and could be picked up by AnimalMovement.TryPlace's
                // raycast. Deactivating first takes them out of the physics scene immediately.
                cell.gameObject.SetActive(false);
                Destroy(cell.gameObject);
            }

            _cells = null;
        }

        public IGridCell GetCell(int x, int y)
        {
            if (!IsInBounds(x, y))
                return null;

            EnsureGridInitialized();

            // Still null in edit mode, where cells are deliberately never spawned — callers
            // already treat null as "no cell here", so return it instead of throwing.
            return _cells?[x, y];
        }

        public IGridCell GetCell(Vector2Int position)
        {
            return GetCell(position.x, position.y);
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsInBounds(Vector2Int position)
        {
            return IsInBounds(position.x, position.y);
        }

        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - transform.position;
            int x = Mathf.FloorToInt(localPosition.x / CellSize);
            int y = Mathf.FloorToInt(localPosition.z / CellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return transform.position + new Vector3(
                x * CellSize,
                0,
                y * CellSize
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
                x * CellSize + CellSize * 0.5f,  // Add half cell to X
                0,
                y * CellSize + CellSize * 0.5f   // Add half cell to Z
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
            }

            return true;
        }

        /// <summary>
        /// Checks if a unit can be placed at position, excluding specific cells from walkability check.
        /// This is useful for pathfinding when a unit needs to ignore its own current occupied cells.
        /// </summary>
        public bool CanPlaceUnit(Vector2Int position, UnitSize size, Direction direction, HashSet<Vector2Int> excludePositions)
        {
            var cells = GetOccupiedCellsInternal(position, size, direction);

            foreach (var cell in cells)
            {
                if (cell == null)
                    return false;

                // Skip walkability check for excluded positions (e.g., unit's current cells)
                var cellPos = new Vector2Int(cell.X, cell.Y);
                if (excludePositions != null && excludePositions.Contains(cellPos))
                    continue;

                // Check walkability for non-excluded cells
                if (!cell.IsWalkable)
                    return false;
            }

            return true;
        }

        public List<IGridCell> GetOccupiedCells(Vector2Int position, UnitSize size, Direction direction)
        {
            return GetOccupiedCellsInternal(position, size, direction).Cast<IGridCell>().ToList();
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
        public List<IGridCell> GetNeighborCells(Vector2Int position, UnitSize size, Direction direction)
        {
            // Get all cells (internally adjusts position to fit bounds)
            var allCells = GetOccupiedCellsInternal(position, size, direction);

            // Remove the cell at the ORIGINAL position as the "base" cell
            // This ensures that when TestEnemiesSpawner marks gridCell (original) and neighbors,
            // all actual occupied cells get marked correctly
            var baseCellAtOriginalPosition = GetCell(position) as GridCell;
            if (baseCellAtOriginalPosition != null && allCells.Contains(baseCellAtOriginalPosition))
            {
                allCells.Remove(baseCellAtOriginalPosition);
            }

            return allCells.Cast<IGridCell>().ToList();
        }

        /// <summary>
        /// Adjusts position to ensure all cells of a unit fit within grid bounds.
        /// If a unit would extend beyond grid boundaries, shifts the position to keep it in bounds.
        /// Now accounts for negative direction movement (South, West).
        /// </summary>
        private Vector2Int AdjustPositionToFitBounds(Vector2Int position, UnitSize size, Direction direction)
        {
            int width = size.Width;
            int height = size.Height;

            int adjustedX = position.x;
            int adjustedY = position.y;

            // Get direction vector to determine positive/negative movement
            Vector3 directionVector = DirectionToVector(direction);
            int xDirection = GetCellOffsetDirection(directionVector.x);
            int zDirection = GetCellOffsetDirection(directionVector.z);

            // For 1x2 units, determine actual dimensions based on direction
            int xExtent = width;
            int yExtent = height;

            if (size.IsRectangular())
            {
                if (direction == Direction.East || direction == Direction.West)
                {
                    // Horizontal orientation: swap dimensions
                    xExtent = height;
                    yExtent = width;
                }
            }

            // Adjust X coordinate based on direction
            if (xDirection > 0)
            {
                // Positive direction: check if extends beyond right boundary
                if (position.x + xExtent > Width)
                {
                    adjustedX = Width - xExtent;
                }
            }
            else
            {
                // Negative direction: check if extends beyond left boundary
                if (position.x - (xExtent - 1) < 0)
                {
                    adjustedX = xExtent - 1;
                }
            }

            // Adjust Y coordinate based on direction
            if (zDirection > 0)
            {
                // Positive direction: check if extends beyond top boundary
                if (position.y + yExtent > Height)
                {
                    adjustedY = Height - yExtent;
                }
            }
            else
            {
                // Negative direction: check if extends beyond bottom boundary
                if (position.y - (yExtent - 1) < 0)
                {
                    adjustedY = yExtent - 1;
                }
            }

            // Final clamp to ensure position is within valid range
            adjustedX = Mathf.Clamp(adjustedX, 0, Width - 1);
            adjustedY = Mathf.Clamp(adjustedY, 0, Height - 1);

            return new Vector2Int(adjustedX, adjustedY);
        }

        /// <summary>
        /// Calculates which cells a unit occupies based on size and direction.
        /// Automatically adjusts position if unit would extend beyond grid bounds.
        /// The anchor point is always the "back-left" cell relative to the unit's movement direction.
        /// Examples for 2x2 unit:
        /// - Direction North (0,0,1): anchor at (3,0), occupies: (3,0), (4,0), (3,1), (4,1)
        /// - Direction South (0,0,-1): anchor at (3,9), occupies: (3,9), (4,9), (3,8), (4,8)
        /// </summary>
        private List<GridCell> GetOccupiedCellsInternal(Vector2Int position, UnitSize size, Direction direction)
        {
            // Adjust position to ensure all cells fit within bounds (now supports negative directions)
            position = AdjustPositionToFitBounds(position, size, direction);

            var cells = new List<GridCell>();
            int width = size.Width;
            int height = size.Height;

            // For 1x2 units, direction affects which dimension is which
            // North/South: unit is vertical (height along Y axis)
            // East/West: unit is horizontal (height along X axis)
            if (size.IsRectangular())
            {
                if (direction == Direction.North || direction == Direction.South)
                {
                    // Vertical orientation: unit extends along Z axis
                    // North: +1 direction, South: -1 direction
                    int zDirection = direction == Direction.North ? 1 : -1;

                    for (int dy = 0; dy < height; dy++)
                    {
                        var cell = GetCell(position.x, position.y + dy * zDirection) as GridCell;
                        if (cell != null)
                            cells.Add(cell);
                    }
                }
                else // East or West
                {
                    // Horizontal orientation: unit extends along X axis
                    // East: +1 direction, West: -1 direction
                    int xDirection = direction == Direction.East ? 1 : -1;

                    for (int dx = 0; dx < height; dx++)  // Using height as horizontal extent
                    {
                        var cell = GetCell(position.x + dx * xDirection, position.y) as GridCell;
                        if (cell != null)
                            cells.Add(cell);
                    }
                }
            }
            else
            {
                // For square units (1x1, 2x2), calculate occupied cells based on movement direction
                // The anchor point is the "back-left" corner, and cells extend in the direction of movement
                Vector3 directionVector = DirectionToVector(direction);
                int xDirection = GetCellOffsetDirection(directionVector.x);
                int zDirection = GetCellOffsetDirection(directionVector.z);

                for (int dx = 0; dx < width; dx++)
                {
                    for (int dy = 0; dy < height; dy++)
                    {
                        int actualX = position.x + dx * xDirection;
                        int actualY = position.y + dy * zDirection;
                        var cell = GetCell(actualX, actualY) as GridCell;
                        if (cell != null)
                            cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// Converts Direction enum to Vector3 direction
        /// </summary>
        private Vector3 DirectionToVector(Direction direction)
        {
            return direction switch
            {
                Direction.North => new Vector3(0, 0, 1),   // Forward
                Direction.South => new Vector3(0, 0, -1),  // Backward
                Direction.East => new Vector3(1, 0, 0),    // Right
                Direction.West => new Vector3(-1, 0, 0),   // Left
                _ => new Vector3(0, 0, 1)                  // Default: North
            };
        }

        /// <summary>
        /// Determines cell offset direction based on direction component.
        /// Returns +1 for positive or zero direction, -1 for negative direction.
        /// </summary>
        private int GetCellOffsetDirection(float directionComponent)
        {
            return directionComponent < 0 ? -1 : 1;
        }

        public void SetOccupied(Vector2Int position, UnitSize size, Direction direction, object unit)
        {
            var cells = GetOccupiedCellsInternal(position, size, direction);

            foreach (var cell in cells)
            {
                if (cell != null)
                {
                    cell.IsWalkable = false;
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
                    cell.IsWalkable = true;
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
                (cell as GridCell)?.UpdateVisual();
            }
        }

        /// <summary>
        /// Updates all cell visuals
        /// </summary>
        public void RefreshAllVisuals()
        {
            if (_cells == null) return;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
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
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = _cells[x, y];
                    if (cell == null) continue;

                    Vector3 worldPos = GridToWorld(x, y);

                    // Choose color based on cell state
                    Color cellColor = cell.IsWalkable ? _walkableColor : _blockedColor;

                    cellColor.a = 0.3f;
                    Gizmos.color = cellColor;

                    // Draw cell cube
                    Gizmos.DrawCube(worldPos + Vector3.up * _gizmoHeight, new Vector3(CellSize * 0.9f, 0.01f, CellSize * 0.9f));

                    // Draw cell border
                    Gizmos.color = Color.white * 0.5f;
                    DrawCellBorder(worldPos, CellSize);
                }
            }
        }

        private void DrawPlaceholderGrid()
        {
            Gizmos.color = Color.white * 0.3f;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Vector3 worldPos = transform.position + new Vector3(
                        x * CellSize + CellSize * 0.5f,
                        _gizmoHeight,
                        y * CellSize + CellSize * 0.5f
                    );

                    DrawCellBorder(worldPos, CellSize);
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
        /// Checks if there is space to actually place an animal of the given type
        /// Verifies that contiguous cells in the correct shape are available
        /// </summary>
        public bool HasCellFor(AnimalType animalType)
        {
            // Determine unit size based on animal type
            UnitSize unitSize = animalType switch
            {
                AnimalType.Elephant => new UnitSize(2, 2),  // 2x2 (Big) - 4 cells
                AnimalType.Chicken => new UnitSize(1, 1),   // 1x1 (Small) - Each chicken is 1x1 (4 chickens = ChickenGroup)
                AnimalType.Cheetah => new UnitSize(1, 2),   // 1x2 (Medium) - 2 adjacent cells
                AnimalType.Deer => new UnitSize(1, 2),      // 1x2 (Medium) - 2 adjacent cells
                AnimalType.Fox => new UnitSize(1, 2),       // 1x2 (Medium) - 2 adjacent cells
                AnimalType.Hedgehog => new UnitSize(1, 2),  // 1x2 (Medium) - 2 adjacent cells
                _ => new UnitSize(1, 1)  // Default fallback
            };

            // Try to find a valid placement position in the bottom two rows
            // For rectangular units (1×2 or 2×1), we need to check both vertical and horizontal orientations
            Direction[] directionsToCheck = unitSize.IsRectangular()
                ? new[] { Direction.North, Direction.East }  // Try vertical and horizontal
                : new[] { Direction.North };                  // Square units don't depend on direction

            for (int y = 0; y <= 1; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);

                    // Check if we can place the unit at this position with any valid direction
                    foreach (Direction direction in directionsToCheck)
                    {
                        if (CanPlaceUnit(position, unitSize, direction))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Places an animal on the first available valid position in the grid
        /// </summary>
        public void PlaceOnGrid(AnimalMovement animal)
        {
            if (animal == null)
            {
                Debug.LogWarning("[GridManager] Cannot place null animal");
                return;
            }

            var unitSize = animal.UnitSize;
            var direction = animal.Direction;

            // Find the first valid placement position in bottom two rows
            GridCell targetCell = null;
            for (int y = 0; y <= 1 && targetCell == null; y++)
            {
                for (int x = 0; x < Width && targetCell == null; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (CanPlaceUnit(position, unitSize, direction))
                    {
                        targetCell = GetCell(position) as GridCell;
                    }
                }
            }

            if (targetCell == null)
            {
                Debug.LogWarning("[GridManager] No valid placement position found");
                return;
            }

            // Place the animal at the target position
            animal.Place(targetCell.WorldPosition);
            animal.SetCurrentNode(targetCell);

            // Get all occupied cells (includes the base cell and neighbors)
            var allOccupiedCells = GetOccupiedCells(targetCell.GridPosition, unitSize, direction).Cast<GridCell>().ToList();

            // Mark all cells as occupied (this sets IsWalkable = false)
            SetOccupied(targetCell.GridPosition, unitSize, direction, animal);

            // Get neighbor cells (excluding the base targetCell) for the animal's internal tracking
            var neighbourCells = GetNeighborCells(targetCell.GridPosition, unitSize, direction).Cast<GridCell>().ToList();
            if (neighbourCells.Count > 0)
            {
                animal.FillNodes(neighbourCells);
            }
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
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    GridCell cell = _cells[x, y];
                    if (cell != null && cell.IsWalkable && cell.CanPlace &&
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
