using System.Collections.Generic;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Movement
{
    /// <summary>
    /// Управляет занятыми клетками юнита на сетке
    /// Выделяет ответственность за управление состоянием занятости клеток из AnimalMovement
    /// </summary>
    public class UnitOccupancy : MonoBehaviour, IUnitOccupancy
    {
        [SerializeField] private GridCell _currentCell;
        [SerializeField] private List<GridCell> _occupiedCells = new List<GridCell>();

        private readonly List<AnimalMovement> _additionalUnits = new List<AnimalMovement>();

        // Temporary storage for restore on failed placement
        private GridCell _savedCurrentCell;
        private List<GridCell> _savedOccupiedCells;

        public GridCell CurrentCell => _currentCell;

        public IReadOnlyList<GridCell> OccupiedCells => _occupiedCells;

        /// <summary>
        /// Добавить дополнительные юниты, чья занятость также должна управляться
        /// (для abilities типа MultipleCharacters)
        /// </summary>
        public void AddAdditionalUnits(IEnumerable<AnimalMovement> units)
        {
            _additionalUnits.AddRange(units);
        }

        public void SetCurrentCell(GridCell cell)
        {
            _currentCell = cell;
        }

        public void SetOccupiedCells(List<GridCell> cells)
        {
            _occupiedCells = cells ?? new List<GridCell>();
        }

        public void ClearOccupancy()
        {
            // Очистить дополнительных юнитов (если есть)
            if (_additionalUnits.Count > 0)
            {
                foreach (AnimalMovement animal in _additionalUnits)
                {
                    if (animal.CurrentPathNode != null)
                    {
                        animal.CurrentPathNode.IsWalkable = true;
                        animal.CurrentPathNode.UpdateVisual();
                    }

                    foreach (GridCell node in animal.Nodes)
                    {
                        node.IsWalkable = true;
                        node.UpdateVisual();
                    }

                    animal.Nodes.Clear();
                }
            }

            // Очистить главную клетку
            if (_currentCell != null)
            {
                _currentCell.IsWalkable = true;
                _currentCell.UpdateVisual();
            }

            // Очистить занятые клетки
            foreach (GridCell node in _occupiedCells)
            {
                node.IsWalkable = true;
                node.UpdateVisual();
            }

            _occupiedCells.Clear();
        }

        public void MarkCellsAsOccupied(GridCell currentCell, List<GridCell> neighbors)
        {
            if (currentCell == null) return;

            _currentCell = currentCell;
            _currentCell.IsWalkable = false;
            _currentCell.UpdateVisual();

            if (neighbors != null)
            {
                foreach (var neighbor in neighbors)
                {
                    neighbor.IsWalkable = false;
                    neighbor.UpdateVisual();
                }

                _occupiedCells = neighbors;
            }
        }

        /// <summary>
        /// Saves the current grid state before attempting placement.
        /// Call this before ClearOccupancy() when starting drag.
        /// </summary>
        public void SaveState()
        {
            _savedCurrentCell = _currentCell;
            _savedOccupiedCells = _occupiedCells != null
                ? new List<GridCell>(_occupiedCells)
                : null;
        }

        /// <summary>
        /// Restores the saved grid state when placement fails.
        /// Re-marks the cells as occupied by the unit.
        /// </summary>
        public void RestoreState()
        {
            if (_savedCurrentCell != null)
            {
                MarkCellsAsOccupied(_savedCurrentCell, _savedOccupiedCells);
            }
        }

        /// <summary>
        /// Clears the saved state after successful placement.
        /// </summary>
        public void ClearSavedState()
        {
            _savedCurrentCell = null;
            _savedOccupiedCells = null;
        }
    }
}
