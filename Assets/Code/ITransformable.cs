using System.Collections.Generic;
using System.Threading.Tasks;
using Code.GridPathfinding;
using Code.Pathfinding;
using UnityEngine;

namespace Code
{
    public interface ITransformable
    {
        Vector3 Position { get; }
        Vector2Int IntPosition { get; }
        UnitSize UnitSize { get; }
        int SizeEffect { get; }
        IGridCell CurrentPathNode { get; }

        /// <summary>
        /// Returns all grid cells occupied by this unit (CurrentPathNode + neighbor nodes)
        /// </summary>
        List<IGridCell> GetOccupiedCells();

        Task Shift();
    }
}