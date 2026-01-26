using System.Collections.Generic;
using System.Linq;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Movement
{
    /// <summary>
    /// Вычисляет возможные позиции для атаки и проверяет близость к цели
    /// Изолирует сложную геометрическую логику позиционирования (~200 строк)
    /// </summary>
    public class TargetPositionCalculator : ITargetDetector
    {
        private readonly IGridManager _gridManager;

        public TargetPositionCalculator(IGridManager gridManager)
        {
            _gridManager = gridManager;
        }

        public Vector2Int[] GetPossibleAttackPositions(
            GridCell currentNode,
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

            // 4. Фильтровать: проверить какие anchor points валидны
            var validPositions = new List<Vector2Int>();
            foreach (var anchor in candidateAnchors)
            {
                if (_gridManager.CanPlaceUnit(anchor, unitSize, direction, ignoreOccupied: true))
                {
                    validPositions.Add(anchor);
                }
            }

            // 5. Сортировка по приоритету (ближе по X, потом по Y - предпочтение верхним позициям)
            var currentPos = new Vector2Int(currentNode.X, currentNode.Y);
            validPositions.Sort((a, b) =>
            {
                var deltaXa = Mathf.Abs(a.x - currentPos.x);
                var deltaXb = Mathf.Abs(b.x - currentPos.x);

                var xComparison = deltaXa.CompareTo(deltaXb);
                if (xComparison != 0) return xComparison;

                var deltaYa = a.y - currentPos.y;
                var deltaYb = b.y - currentPos.y;
                return deltaYb.CompareTo(deltaYa);
            });

            return validPositions.ToArray();
        }

        public bool IsCloseToTarget(
            GridCell currentNode,
            List<GridCell> occupiedNodes,
            ITarget target)
        {
            if (currentNode == null || target == null) return false;

            // Собрать все клетки, которые занимает атакующий юнит
            var attackerCells = new List<GridCell> { currentNode };
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
        private List<GridCell> GetAllTargetCells(ITransformable targetTransformable)
        {
            var targetCells = new List<GridCell>();

            if (targetTransformable.CurrentPathNode != null)
            {
                targetCells.Add(targetTransformable.CurrentPathNode);
            }

            if (targetTransformable is AnimalMovement targetAnimal)
            {
                targetCells.AddRange(targetAnimal.Nodes);
            }

            return targetCells;
        }

        /// <summary>
        /// Вычисляет все возможные anchor points, при которых юнит займет указанную клетку
        /// </summary>
        private List<Vector2Int> GetAnchorPointsForCell(Vector2Int targetCell, UnitSize unitSize, Direction unitDirection)
        {
            var anchors = new List<Vector2Int>();
            var width = unitSize.Width;
            var height = unitSize.Height;

            // 1×1 юнит
            if (width == 1 && height == 1)
            {
                anchors.Add(targetCell);
                return anchors;
            }

            // 1×2 юнит (прямоугольный)
            if (width == 1 && height == 2)
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
                return anchors;
            }

            // 2×2 юнит (квадратный)
            if (width == 2 && height == 2)
            {
                if (unitDirection == Direction.North || unitDirection == Direction.East)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x+1, anchor.y), (anchor.x, anchor.y+1), (anchor.x+1, anchor.y+1)
                    anchors.Add(targetCell);                                       // (0,0)
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y));   // (1,0)
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y - 1));   // (0,1)
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y - 1)); // (1,1)
                }
                else if (unitDirection == Direction.South)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x+1, anchor.y), (anchor.x, anchor.y-1), (anchor.x+1, anchor.y-1)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y));
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y + 1));
                    anchors.Add(new Vector2Int(targetCell.x - 1, targetCell.y + 1));
                }
                else if (unitDirection == Direction.West)
                {
                    // Занимает: (anchor.x, anchor.y), (anchor.x-1, anchor.y), (anchor.x, anchor.y+1), (anchor.x-1, anchor.y+1)
                    anchors.Add(targetCell);
                    anchors.Add(new Vector2Int(targetCell.x + 1, targetCell.y));
                    anchors.Add(new Vector2Int(targetCell.x, targetCell.y - 1));
                    anchors.Add(new Vector2Int(targetCell.x + 1, targetCell.y - 1));
                }
                return anchors;
            }

            return anchors;
        }
    }
}
