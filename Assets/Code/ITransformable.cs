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

        /// <summary>
        /// Executes dodge movement to avoid incoming damage.
        /// Returns true if dodge was successful (valid position found), false if no valid dodge position exists.
        /// </summary>
        Task<bool> Shift();

        /// <summary>
        /// Retreats from target position by specified distance.
        /// Used by Velociraptor's retreat ability.
        /// </summary>
        Task RetreatFrom(Vector2Int targetPosition, int maxDistance);
    }
}