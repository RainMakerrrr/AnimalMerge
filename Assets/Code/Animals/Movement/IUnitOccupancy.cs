using System.Collections.Generic;
using Code.GridPathfinding;

namespace Code.Animals.Movement
{
    /// <summary>
    /// Интерфейс для управления занятыми клетками юнита на сетке
    /// </summary>
    public interface IUnitOccupancy
    {
        /// <summary>
        /// Текущая главная клетка юнита (anchor point)
        /// </summary>
        GridCell CurrentCell { get; }

        /// <summary>
        /// Список дополнительных клеток, занимаемых юнитом (кроме главной)
        /// </summary>
        IReadOnlyList<GridCell> OccupiedCells { get; }

        /// <summary>
        /// Установить текущую главную клетку юнита
        /// </summary>
        void SetCurrentCell(GridCell cell);

        /// <summary>
        /// Установить список занимаемых клеток
        /// </summary>
        void SetOccupiedCells(List<GridCell> cells);

        /// <summary>
        /// Освободить все занимаемые клетки (сделать их walkable)
        /// </summary>
        void ClearOccupancy();

        /// <summary>
        /// Пометить клетки как занятые юнитом (сделать их non-walkable)
        /// </summary>
        /// <param name="currentCell">Главная клетка юнита</param>
        /// <param name="neighbors">Соседние клетки, занимаемые юнитом</param>
        void MarkCellsAsOccupied(GridCell currentCell, List<GridCell> neighbors);

        /// <summary>
        /// Добавить дополнительные юниты для управления занятостью
        /// (используется для abilities типа MultipleCharacters)
        /// </summary>
        void AddAdditionalUnits(System.Collections.Generic.IEnumerable<AnimalMovement> units);
    }
}
