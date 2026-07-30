using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Movement;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Unified grid cell combining model and view in a single MonoBehaviour
    /// Handles A* pathfinding properties, visual representation, and raycasting
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GridCell : MonoBehaviour, IRaycastable, IGridCell
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Colors")]
        // Alpha drives the fill opacity of the GridCellBorder shader; the border color itself
        // is a shared material property and is deliberately not affected by these.
        [SerializeField] private Color _walkableColor = new Color(0.7f, 0.7f, 0.7f, 0.15f);  // Neutral gray, barely visible
        [SerializeField] private Color _blockedColor = new Color(0.8f, 0.2f, 0.2f, 0.35f);   // Red
        [SerializeField] private Color _highlightColor = new Color(0.2f, 0.8f, 0.2f, 0.35f); // Green (for hover/selection)

        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private IGridManager _gridManager;

        // Grid position
        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector2Int GridPosition => new Vector2Int(X, Y);

        // A* costs
        public float GCost { get; set; }  // Distance from start node
        public float HCost { get; set; }  // Heuristic distance to target
        public float FCost => GCost + HCost;  // Total cost

        // Pathfinding data
        public GridCell Parent { get; set; }

        // Explicit interface implementation for IGridCell.Parent
        IGridCell IGridCell.Parent
        {
            get => Parent;
            set => Parent = value as GridCell;
        }

        // Cell state
        public bool IsWalkable { get; set; }

        // World position (center of the cell)
        public Vector3 WorldPosition => transform.position;

        // Check if unit can be placed here
        public bool CanPlace => IsWalkable && (Y == 0 || Y == 1);

        private void Awake()
        {
            // The renderer lives on the "Visual" child (a Quad rotated to lie flat), because
            // GridManager instantiates cells with Quaternion.identity and would discard any
            // rotation baked into the prefab root.
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Scales the cell to match the grid's cell size. The border shader keeps line
        /// thickness in world units, so lines stay the same width at any cell size.
        /// </summary>
        public void SetSize(float cellSize)
        {
            transform.localScale = new Vector3(cellSize, 1f, cellSize);
        }

        /// <summary>
        /// Initialize the cell with grid data
        /// </summary>
        public void Initialize(int x, int y, IGridManager gridManager, bool isWalkable = true)
        {
            X = x;
            Y = y;
            _gridManager = gridManager;
            IsWalkable = isWalkable;

            gameObject.name = $"Cell_{x}_{y}";
            Reset();
            UpdateVisual();
        }

        /// <summary>
        /// Resets A* pathfinding data for reuse
        /// </summary>
        public void Reset()
        {
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }

        /// <summary>
        /// Updates the visual representation based on cell state
        /// </summary>
        public void UpdateVisual()
        {
            if (_meshRenderer == null) return;

            Color targetColor = IsWalkable ? _walkableColor : _blockedColor;

            SetColor(targetColor);
        }

        /// <summary>
        /// Highlights the cell (e.g., for hover effects)
        /// </summary>
        public void Highlight(bool highlighted)
        {
            if (_meshRenderer == null) return;

            if (highlighted && IsWalkable)
            {
                SetColor(_highlightColor);
            }
            else
            {
                UpdateVisual();
            }
        }

        /// <summary>
        /// Sets the cell's fill color. Alpha controls fill opacity; the cell border keeps
        /// the constant color defined on the shared material.
        /// Uses a MaterialPropertyBlock so all cells share one material and GPU-instance.
        /// </summary>
        public void SetColor(Color color)
        {
            if (_meshRenderer == null) return;

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(ColorId, color);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Gets neighbors in cardinal directions
        /// </summary>
        public GridCell[] GetNeighbors()
        {
            // This would need reference to parent GridManager to get neighbor views
            // For now, return empty array - implement if needed
            return new GridCell[0];
        }

        /// <summary>
        /// Implementation of IRaycastable - checks if animal can be placed here
        /// </summary>
        public bool Accept(PlayerAnimalFacade animal)
        {
            if (!IsInDeploymentZone())
                return false;

            var movement = animal.Movement;
            var unitSize = movement.UnitSize;
            var direction = movement.Direction;

            var possiblePositions = GetPossiblePlacementPositions();

            foreach (var pos in possiblePositions)
            {
                if (IsPositionValidForPlacement(pos, unitSize, direction) &&
                    TryPlaceAnimalAtPosition(pos, unitSize, direction, movement))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this cell is in the deployment zone (Y <= 1)
        /// </summary>
        private bool IsInDeploymentZone()
        {
            return Y <= 1;
        }

        /// <summary>
        /// Gets possible placement positions: this cell and its neighbors
        /// </summary>
        private Vector2Int[] GetPossiblePlacementPositions()
        {
            return new[]
            {
                new Vector2Int(X, Y),
                new Vector2Int(X, Y - 1),
                new Vector2Int(X - 1, Y)
            };
        }

        /// <summary>
        /// Checks if a position is valid for unit placement:
        /// - Anchor must be in deployment zone (Y <= 1)
        /// - Unit must fit within grid bounds
        /// - All occupied cells must be in deployment zone
        /// </summary>
        private bool IsPositionValidForPlacement(Vector2Int position, UnitSize unitSize, Direction direction)
        {
            // Anchor point must be in deployment zone
            if (position.y > 1)
                return false;

            // Calculate theoretical bounds
            var (minX, maxX, minY, maxY) = Utilities.CalculateUnitBounds(position, unitSize, direction);

            // Check if unit would extend outside grid bounds
            if (minX < 0 || maxX >= _gridManager.Width || minY < 0 || maxY >= _gridManager.Height)
                return false;

            // All cells must be in deployment zone
            if (maxY > 1)
                return false;

            return true;
        }

        /// <summary>
        /// Attempts to place the animal at the specified position.
        /// Returns true if successful, false otherwise.
        /// </summary>
        private bool TryPlaceAnimalAtPosition(Vector2Int position, UnitSize unitSize, Direction direction, AnimalMovement movement)
        {
            if (!_gridManager.CanPlaceUnit(position, unitSize, direction))
                return false;

            var targetCell = _gridManager.GetCell(position) as GridCell;
            if (targetCell == null || !targetCell.IsWalkable)
                return false;

            var occupiedCells = _gridManager.GetOccupiedCells(position, unitSize, direction)
                .Where(cell => cell != null)
                .Cast<GridCell>()
                .ToList();

            PlaceAnimal(position, targetCell, occupiedCells, unitSize, direction, movement);
            return true;
        }

        /// <summary>
        /// Places the animal at the target position and marks cells as occupied
        /// </summary>
        private void PlaceAnimal(Vector2Int position, GridCell targetCell, List<GridCell> occupiedCells,
            UnitSize unitSize, Direction direction, AnimalMovement movement)
        {
            movement.Place(targetCell.WorldPosition);
            movement.SetCurrentNode(targetCell);
            _gridManager.SetOccupied(position, unitSize, direction, movement);

            if (occupiedCells.Count > 0)
            {
                movement.FillNodes(occupiedCells);
            }
        }

        public override string ToString()
        {
            return $"Cell({X},{Y}) Walkable:{IsWalkable}";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is GridCell other && other.X == X && other.Y == Y;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw cell bounds when selected in editor
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0.1f, 1f));
        }
    }
}
