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
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Collider))]
    public class GridCell : MonoBehaviour, IRaycastable
    {
        [Header("Colors")]
        [SerializeField] private Color _walkableColor = new Color(0.7f, 0.7f, 0.7f, 1f);  // Neutral gray
        [SerializeField] private Color _blockedColor = new Color(0.8f, 0.2f, 0.2f, 1f);   // Red
        [SerializeField] private Color _highlightColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green (for hover/selection)

        private MeshRenderer _meshRenderer;
        private Material _material;
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

        // Cell state
        public bool IsWalkable { get; set; }

        // World position (center of the cell)
        public Vector3 WorldPosition => transform.position;

        // Check if unit can be placed here
        public bool CanPlace => IsWalkable && (Y == 0 || Y == 1);

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            // Create instance of material to avoid modifying shared material
            if (_meshRenderer.sharedMaterial != null)
            {
                _material = new Material(_meshRenderer.sharedMaterial);
                _meshRenderer.material = _material;
            }
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
            if (_material == null) return;

            Color targetColor = IsWalkable ? _walkableColor : _blockedColor;

            SetColor(targetColor);
        }

        /// <summary>
        /// Highlights the cell (e.g., for hover effects)
        /// </summary>
        public void Highlight(bool highlighted)
        {
            if (_material == null) return;

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
        /// Sets the cell color
        /// </summary>
        public void SetColor(Color color)
        {
            if (_material == null) return;

            // Try common shader color property names
            if (_material.HasProperty("_Color"))
            {
                _material.SetColor("_Color", color);
            }
            else if (_material.HasProperty("_BaseColor"))
            {
                _material.SetColor("_BaseColor", color);
            }
            else
            {
                _material.color = color;
            }
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
        public bool Accept(AnimalFacade animal)
        {
            if (animal == null || _gridManager == null)
                return false;

            AnimalMovement movement = animal.Movement;
            if (movement == null)
                return false;

            UnitSize unitSize = movement.ObjectSizeType.ToUnitSize();
            Direction direction = movement.Direction.ToDirection();

            // Try this cell and neighbors
            Vector2Int[] possiblePositions =
            {
                new Vector2Int(X, Y),
                new Vector2Int(X, Y - 1),
                new Vector2Int(X - 1, Y)
            };

            foreach (Vector2Int pos in possiblePositions)
            {
                if (_gridManager.CanPlaceUnit(pos, unitSize, direction))
                {
                    GridCell targetCell = _gridManager.GetCell(pos);
                    if (targetCell != null && targetCell.IsWalkable &&
                        (pos.y == 0 || pos.y == 1)) // Can only place in bottom rows
                    {
                        movement.Place(targetCell.WorldPosition);

                        //todo fix
                        movement.SetCurrentNode(targetCell);

                        List<GridCell> occupiedCells = _gridManager.GetOccupiedCells(pos, unitSize, direction);
                        _gridManager.SetOccupied(pos, unitSize, direction, movement);

                        if (occupiedCells.Count > 0)
                        {
                            //todo fix
                            movement.FillNodes(occupiedCells);
                        }

                        return true;
                    }
                }
            }

            return false;
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

        private void OnDestroy()
        {
            // Clean up instanced material
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw cell bounds when selected in editor
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 0.1f, 1f));
        }
    }
}
