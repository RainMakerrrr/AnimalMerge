using System.Collections.Generic;
using System.Linq;
using Code.GridPathfinding;
using UnityEngine;
using Zenject;

namespace Code.Animals.Movement
{
    /// <summary>
    /// Вычисляет возможные позиции для атаки и проверяет близость к цели
    /// Изолирует сложную геометрическую логику позиционирования (~200 строк)
    /// </summary>
    public class TargetPositionCalculator : ITargetDetector
    {
        private readonly IGridManager _gridManager;

        public TargetPositionCalculator([Inject(Id = GridIdentifier.GameGrid)]IGridManager gridManager)
        {
            _gridManager = gridManager;
        }

        public Vector2Int[] GetPossibleAttackPositions(
            IGridCell currentNode,
            ITarget target,
            UnitSize unitSize,
            Direction direction)
        {
            // 1. Получить все клетки цели
            var targetTransformable = target?.Transformable;
            if (targetTransformable == null || currentNode == null)
            {
                return System.Array.Empty<Vector2Int>();
            }

            var targetCells = GetAllTargetCells(targetTransformable);
            if (targetCells.Count == 0)
            {
                return System.Array.Empty<Vector2Int>();
            }

            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Target cells: {string.Join(", ", targetCells.Select(c => $"({c.X},{c.Y})"))}");

            // 2. Построить зону атаки (все соседи клеток цели)
            var attackZone = new HashSet<Vector2Int>();
            foreach (var targetCell in targetCells)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        attackZone.Add(new Vector2Int(targetCell.X + dx, targetCell.Y + dy));
                    }
                }
            }

            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Attack zone size: {attackZone.Count}");
            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Attack zone cells: {string.Join(", ", attackZone.Select(az => $"({az.x},{az.y})"))}");

            // 3. Для каждой клетки зоны атаки найти возможные anchor points
            var candidateAnchors = new HashSet<Vector2Int>();

            foreach (var attackCell in attackZone)
            {
                var anchors = GetAnchorPointsForCell(attackCell, unitSize, direction);
                foreach (var anchor in anchors)
                {
                    candidateAnchors.Add(anchor);
                }
            }

            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Candidate anchors: {string.Join(", ", candidateAnchors.Select(a => $"({a.x},{a.y})"))}");

            // 4. Get current unit's occupied cells to exclude from walkability checks
            // This allows the unit to pathfind through positions that overlap with its current location
            var currentOccupiedCells = _gridManager.GetOccupiedCells(new Vector2Int(currentNode.X, currentNode.Y), unitSize, direction);
            var excludePositions = new HashSet<Vector2Int>();
            foreach (var cell in currentOccupiedCells)
            {
                excludePositions.Add(new Vector2Int(cell.X, cell.Y));
            }

            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Current unit occupied cells (excluded from checks): {string.Join(", ", excludePositions.Select(p => $"({p.x},{p.y})"))}");

            // 5. Фильтровать: проверить какие anchor points валидны
            var validPositions = new List<Vector2Int>();
            int rejectedByPlacement = 0;
            int rejectedByOverlap = 0;
            int rejectedByAdjacency = 0;

            foreach (var anchor in candidateAnchors)
            {
                // First check if unit can be placed, excluding its own current cells from walkability check
                if (!_gridManager.CanPlaceUnit(anchor, unitSize, direction, excludePositions))
                {
                    rejectedByPlacement++;
                    Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Rejected anchor: ({anchor.x},{anchor.y}) - CanPlaceUnit returned false");
                    continue; // Can't place (out of bounds, or occupied by other units)
                }

                // Get all cells this unit would occupy at this anchor position
                var occupiedByAnchor = _gridManager.GetOccupiedCells(anchor, unitSize, direction);

                // Check if this anchor would cause overlap with target cells
                bool overlapsWithTarget = false;
                foreach (var occupiedCell in occupiedByAnchor)
                {
                    if (targetCells.Any(tc => tc.X == occupiedCell.X && tc.Y == occupiedCell.Y))
                    {
                        overlapsWithTarget = true;
                        break;
                    }
                }

                if (overlapsWithTarget)
                {
                    rejectedByOverlap++;
                    Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Rejected anchor: ({anchor.x},{anchor.y}) - overlaps with target");
                    continue; // Skip if overlapping with enemy
                }

                // CRITICAL FIX: Verify that at least one occupied cell is adjacent to at least one target cell
                // This prevents invalid positions like (5,5) when attacking enemy at (4,7)
                bool hasAdjacentCell = false;
                foreach (var occupiedCell in occupiedByAnchor)
                {
                    foreach (var targetCell in targetCells)
                    {
                        // Check adjacency using Chebyshev distance (includes diagonals)
                        int dx = Mathf.Abs(occupiedCell.X - targetCell.X);
                        int dy = Mathf.Abs(occupiedCell.Y - targetCell.Y);

                        // Adjacent if Chebyshev distance ≤ 1 (excluding exact overlap which was already checked)
                        if (Mathf.Max(dx, dy) <= 1 && !(dx == 0 && dy == 0))
                        {
                            hasAdjacentCell = true;
                            break;
                        }
                    }
                    if (hasAdjacentCell) break;
                }

                // Only add if adjacent to enemy
                if (hasAdjacentCell)
                {
                    validPositions.Add(anchor);
                    Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Valid anchor: ({anchor.x},{anchor.y}) - passed all checks");
                }
                else
                {
                    rejectedByAdjacency++;
                    Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Rejected anchor: ({anchor.x},{anchor.y}) - no adjacent cells to enemy");
                }
            }

            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Validation summary: {candidateAnchors.Count} total, {rejectedByPlacement} rejected by placement, {rejectedByOverlap} rejected by overlap, {rejectedByAdjacency} rejected by adjacency, {validPositions.Count} valid");


            // 5. Сортировка по приоритету: ближе к врагу, потом ближе к текущей позиции
            var currentPos = new Vector2Int(currentNode.X, currentNode.Y);

            validPositions.Sort((a, b) =>
            {
                // Primary: sort by minimum distance from occupied cells to enemy (closer = better)
                // For Large/Medium units, we need to check distance from ALL occupied cells, not just anchor
                var occupiedByA = _gridManager.GetOccupiedCells(a, unitSize, direction);
                var occupiedByB = _gridManager.GetOccupiedCells(b, unitSize, direction);

                var minDistToTargetA = occupiedByA.Min(oa =>
                    targetCells.Min(tc => Mathf.Abs(oa.X - tc.X) + Mathf.Abs(oa.Y - tc.Y)));
                var minDistToTargetB = occupiedByB.Min(ob =>
                    targetCells.Min(tc => Mathf.Abs(ob.X - tc.X) + Mathf.Abs(ob.Y - tc.Y)));

                var targetDistComparison = minDistToTargetA.CompareTo(minDistToTargetB);
                if (targetDistComparison != 0) return targetDistComparison;

                // Tie-breaker 1: sort by distance from anchor to current position (closer = better)
                var distToCurrentA = Mathf.Abs(a.x - currentPos.x) + Mathf.Abs(a.y - currentPos.y);
                var distToCurrentB = Mathf.Abs(b.x - currentPos.x) + Mathf.Abs(b.y - currentPos.y);

                var currentDistComparison = distToCurrentA.CompareTo(distToCurrentB);
                if (currentDistComparison != 0) return currentDistComparison;

                // Tie-breaker 2: prefer positions with same X as current (straight path)
                var deltaXA = Mathf.Abs(a.x - currentPos.x);
                var deltaXB = Mathf.Abs(b.x - currentPos.x);

                return deltaXA.CompareTo(deltaXB);
            });

            Debug.Log($"[PathfindingDebug][GetPossibleAttackPositions] Valid positions (sorted): {string.Join(", ", validPositions.Select(v => $"({v.x},{v.y})"))}");

            return validPositions.ToArray();
        }

        public bool IsCloseToTarget(
            IGridCell currentNode,
            List<IGridCell> occupiedNodes,
            ITarget target)
        {
            if (currentNode == null || target == null) return false;

            // Собрать все клетки, которые занимает атакующий юнит
            var attackerCells = new List<IGridCell> { currentNode };
            if (occupiedNodes != null)
            {
                attackerCells.AddRange(occupiedNodes);
            }

            // Получить клетки, которые занимает цель
            var targetTransformable = target.Transformable;
            var targetCells = GetAllTargetCells(targetTransformable);

            // Если не удалось получить клетки цели, вернуть false
            if (targetCells.Count == 0)
            {
                return false;
            }

            // Проверить, есть ли хотя бы одна пара соседних клеток (включая диагонали)
            foreach (var attackerCell in attackerCells)
            {
                foreach (var targetCell in targetCells)
                {
                    var dx = Mathf.Abs(attackerCell.X - targetCell.X);
                    var dy = Mathf.Abs(attackerCell.Y - targetCell.Y);

                    // Соседство по Чебышёву: max(dx, dy) <= 1, исключая совпадение
                    if (dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Получить все клетки, занимаемые целью
        /// </summary>
        public List<IGridCell> GetAllTargetCells(ITransformable targetTransformable)
        {
            if (targetTransformable == null)
            {
                return new List<IGridCell>();
            }

            return targetTransformable.GetOccupiedCells();
        }

        /// <summary>
        /// Вычисляет все возможные anchor points, при которых юнит займет указанную клетку
        /// </summary>
        private List<Vector2Int> GetAnchorPointsForCell(Vector2Int targetCell, UnitSize unitSize, Direction unitDirection)
        {
            var anchors = new List<Vector2Int>();

            // 1×1 юнит
            if (unitSize == UnitSize.Small)
            {
                anchors.Add(targetCell);
                //Debug.Log($"[PathfindingDebug][GetAnchorPointsForCell] Small unit, targetCell: ({targetCell.x},{targetCell.y}), anchor: ({targetCell.x},{targetCell.y})");
                return anchors;
            }

            // 1×2 юнит (прямоугольный)
            if (unitSize == UnitSize.Medium)
            {
                if (unitDirection == Direction.North)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x, anchor.y+1)
                    anchors.Add(targetCell); // targetCell как anchor
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y - 1)); // targetCell как вторая клетка
                }
                else if (unitDirection == Direction.South)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x, anchor.y-1)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y + 1));
                }
                else if (unitDirection == Direction.East)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x+1, anchor.y)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y));
                }
                else if (unitDirection == Direction.West)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x-1, anchor.y)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x + 1, targetCell.y));
                }
                //Debug.Log($"[PathfindingDebug][GetAnchorPointsForCell] Medium unit ({unitDirection}), targetCell: ({targetCell.x},{targetCell.y}), anchors: {string.Join(", ", anchors.Select(a => $"({a.x},{a.y})"))}");
                return anchors;
            }

            // 2×2 юнит (квадратный)
            if (unitSize == UnitSize.Large)
            {
                if (unitDirection == Direction.North || unitDirection == Direction.East)
                {
                    // For North/East: unit occupies (anchor+0,anchor+0), (anchor+1,anchor+0),
                    //                               (anchor+0,anchor+1), (anchor+1,anchor+1)
                    // To occupy targetCell, anchor could be at:
                    anchors.Add(targetCell);                                       // targetCell is cell (0,0)
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y));   // targetCell is cell (1,0)
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y - 1));   // targetCell is cell (0,1)
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y - 1)); // targetCell is cell (1,1)
                }
                else if (unitDirection == Direction.South)
                {
                    // For South: unit occupies (anchor+0,anchor+0), (anchor+1,anchor+0),
                    //                          (anchor+0,anchor-1), (anchor+1,anchor-1)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y));
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y + 1));
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y + 1));
                }
                else if (unitDirection == Direction.West)
                {
                    // For West: unit occupies (anchor+0,anchor+0), (anchor-1,anchor+0),
                    //                         (anchor+0,anchor+1), (anchor-1,anchor+1)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x + 1, targetCell.y));
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y - 1));
                    anchors.Add(new Vector2Int(targetCell.x + 1, targetCell.y - 1));
                }
                //Debug.Log($"[PathfindingDebug][GetAnchorPointsForCell] Large unit ({unitDirection}), targetCell: ({targetCell.x},{targetCell.y}), anchors: {string.Join(", ", anchors.Select(a => $"({a.x},{a.y})"))}");
                return anchors;
            }

            return anchors;
        }
    }
}
