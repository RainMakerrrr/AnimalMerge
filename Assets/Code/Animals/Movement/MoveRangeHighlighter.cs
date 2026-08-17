using System.Collections.Generic;
using System.Linq;
using Code.GridPathfinding;
using UnityEngine;
using Zenject;

namespace Code.Animals.Movement
{
    public class MoveRangeHighlighter : IMoveRangeHighlighter
    {
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 1),   // Up-Right
            new Vector2Int(1, -1),  // Down-Right
            new Vector2Int(-1, 1),  // Up-Left
            new Vector2Int(-1, -1)  // Down-Left
        };

        private readonly IGridManager _gridManager;
        private readonly List<GridCell> _highlightedCells = new List<GridCell>();

        [Inject]
        public MoveRangeHighlighter(IGridManager gridManager)
        {
            _gridManager = gridManager;
        }

        public void Show(AnimalMovement unit)
        {
            if (unit == null || unit.CurrentPathNode == null) return;

            Hide();

            var color = unit.MoveRangeColor;
            var cells = GetReachableCells(unit.CurrentPathNode, unit.UnitSize, unit.Direction, unit.TilesPerMove);

            foreach (var cell in cells)
            {
                cell.SetColor(color);
                _highlightedCells.Add(cell);
            }
        }

        public void Hide()
        {
            foreach (var cell in _highlightedCells)
            {
                cell.UpdateVisual();
            }

            _highlightedCells.Clear();
        }

        private List<GridCell> GetReachableCells(GridCell origin, UnitSize size, Direction direction, int maxSteps)
        {
            var ownFootprint = new HashSet<Vector2Int>();
            foreach (var cell in _gridManager.GetOccupiedCells(origin.GridPosition, size, direction))
            {
                ownFootprint.Add(new Vector2Int(cell.X, cell.Y));
            }

            var visited = new HashSet<Vector2Int> { origin.GridPosition };
            var result = new HashSet<GridCell>();
            var queue = new Queue<(Vector2Int pos, int depth)>();
            queue.Enqueue((origin.GridPosition, 0));

            while (queue.Count > 0)
            {
                var (pos, depth) = queue.Dequeue();

                if (depth >= maxSteps) continue;

                foreach (var dir in Directions)
                {
                    var candidate = pos + dir;

                    if (visited.Contains(candidate)) continue;

                    if (_gridManager.CanPlaceUnit(candidate, size, direction, ownFootprint) &&
                        (_gridManager.GetCell(candidate) as GridCell) != null)
                    {
                        visited.Add(candidate);
                        queue.Enqueue((candidate, depth + 1));

                        var occupied = _gridManager.GetOccupiedCells(candidate, size, direction)
                            .Where(c => c != null)
                            .Cast<GridCell>();

                        foreach (var cell in occupied)
                        {
                            result.Add(cell);
                        }
                    }
                }
            }

            result.RemoveWhere(cell => ownFootprint.Contains(cell.GridPosition));

            return result.ToList();
        }
    }
}
