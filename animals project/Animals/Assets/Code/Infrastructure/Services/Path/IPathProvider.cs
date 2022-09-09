using Code.Logic.MovementModel;
using Pathfinding;

namespace Code.Infrastructure.Services.Path
{
    public interface IPathProvider
    {
        void GeneratePossibleMoves(GridTransformable unit);
        ABPath GeneratePath(GridTransformable unit, GraphNode node);
    }
}