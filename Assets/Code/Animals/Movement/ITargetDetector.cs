using System.Collections.Generic;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Movement
{
    /// <summary>
    /// Интерфейс для определения позиций атаки и проверки близости к цели
    /// </summary>
    public interface ITargetDetector
    {
        /// <summary>
        /// Получить все возможные позиции для атаки цели
        /// </summary>
        /// <param name="currentNode">Текущая клетка юнита</param>
        /// <param name="target">Цель для атаки</param>
        /// <param name="unitSize">Размер атакующего юнита</param>
        /// <param name="direction">Направление атакующего юнита</param>
        /// <returns>Массив возможных позиций anchor points для атаки</returns>
        Vector2Int[] GetPossibleAttackPositions(
            GridCell currentNode,
            ITarget target,
            UnitSize unitSize,
            Direction direction);

        /// <summary>
        /// Проверить, находится ли юнит в клетке, соседней с целью (готов к атаке)
        /// </summary>
        /// <param name="currentNode">Текущая клетка юнита</param>
        /// <param name="occupiedNodes">Список клеток, занимаемых юнитом</param>
        /// <param name="target">Цель для проверки</param>
        /// <returns>True если юнит находится рядом с целью и может атаковать</returns>
        bool IsCloseToTarget(
            GridCell currentNode,
            List<GridCell> occupiedNodes,
            ITarget target);
    }
}
