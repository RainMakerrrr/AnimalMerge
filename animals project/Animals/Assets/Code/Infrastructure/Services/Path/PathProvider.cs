using Code.Logic.MovementModel;
using Pathfinding;
using UnityEngine;

namespace Code.Infrastructure.Services.Path
{
    public class PathProvider : IPathProvider
    {
        private const int MovementPointMultiplier = 1000;

        public void GeneratePossibleMoves(GridTransformable unit)
        {
            var path = ConstantPath.Construct(unit.Position, unit.MovementPoints * MovementPointMultiplier + 1);
            
            path.traversalProvider = unit.TraversalProvider;
            
            unit.Seeker.StartPath(path);
            path.BlockUntilCalculated();
            
        }

        public ABPath GeneratePath(GridTransformable unit, GraphNode node)
        {
            var path = ABPath.Construct(unit.transform.position, (Vector3) node.position);
            
            path.traversalProvider = unit.TraversalProvider;
            
            unit.Seeker.StartPath(path);

            return path;
        }
        
        public ABPath GeneratePath(GridTransformable unit, Vector3 nodePosition)
        {
            var path = ABPath.Construct(unit.transform.position, nodePosition);
            
            path.traversalProvider = unit.TraversalProvider;
            
            unit.Seeker.StartPath(path);

            return path;
        }
    }
}