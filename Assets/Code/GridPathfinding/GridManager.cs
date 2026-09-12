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

        private readonly List<Vector2Int> _footprintPositions = new List<Vector2Int>();

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

        private Vector2Int AdjustPositionToFitBounds(Vector2Int position, UnitSize size, Direction direction)
        {
            return UnitFootprint.AdjustAnchor(position, size, direction, Width, Height);
        }

        private List<GridCell> GetOccupiedCellsInternal(Vector2Int position, UnitSize size, Direction direction)
        {
            var cells = new List<GridCell>();

            UnitFootprint.Cells(position, size, direction, Width, Height, _footprintPositions);

            for (var index = 0; index < _footprintPositions.Count; index++)
            {
                var cell = GetCell(_footprintPositions[index]) as GridCell;

                if (cell != null)
                    cells.Add(cell);
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
            var unitSize = GetFootprint(animalType);

            Direction[] directionsToCheck = unitSize.IsRectangular()
                ? new[] { Direction.North, Direction.East }
                : new[] { Direction.North };

            foreach (var direction in directionsToCheck)
            {
                if (TryFindFreePlacement(unitSize, direction, out _))
                    return true;
            }

            return false;
        }

        public bool TryFindFreePlacement(UnitSize size, Direction direction, out Vector2Int anchor)
        {
            for (int y = 0; y < DeploymentZone.Depth; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var candidate = AdjustPositionToFitBounds(new Vector2Int(x, y), size, direction);

                    if (!CanPlaceUnit(candidate, size, direction))
                        continue;

                    if (!FitsInDeploymentZone(candidate, size, direction))
                        continue;

                    anchor = candidate;
                    return true;
                }
            }

            anchor = default;
            return false;
        }

        private bool FitsInDeploymentZone(Vector2Int anchor, UnitSize size, Direction direction)
        {
            var footprintCells = GetOccupiedCells(anchor, size, direction);

            return footprintCells.Count == size.Width * size.Height
                   && DeploymentZone.ContainsAll(footprintCells);
        }

        private static UnitSize GetFootprint(AnimalType animalType) => AnimalFootprints.For(animalType);

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

            if (!TryFindFreePlacement(unitSize, direction, out var anchor))
            {
                Debug.LogWarning("[GridManager] No valid placement position found");
                return;
            }

            var targetCell = GetCell(anchor) as GridCell;

            if (targetCell == null)
            {
                Debug.LogWarning("[GridManager] No valid placement position found");
                return;
            }

            animal.Place(targetCell.WorldPosition);
            animal.SetCurrentNode(targetCell);

            SetOccupied(targetCell.GridPosition, unitSize, direction, animal);

            // Get neighbor cells (excluding the base targetCell) for the animal's internal tracking
            var neighbourCells = GetNeighborCells(targetCell.GridPosition, unitSize, direction).Cast<GridCell>().ToList();
            if (neighbourCells.Count > 0)
            {
                animal.FillNodes(neighbourCells);
            }
        }

        #endregion
    }
}
