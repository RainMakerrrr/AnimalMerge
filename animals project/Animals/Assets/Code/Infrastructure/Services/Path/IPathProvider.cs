using Code.Logic.MovementModel;
using Pathfinding;
using UnityEngine;

namespace Code.Infrastructure.Services.Path
{
    public interface IPathProvider
    {
        void GeneratePossibleMoves(GridTransformable unit);
        ABPath GeneratePath(GridTransformable unit, GraphNode node);
        ABPath GeneratePath(GridTransformable unit, Vector3 nodePosition);
    }
}