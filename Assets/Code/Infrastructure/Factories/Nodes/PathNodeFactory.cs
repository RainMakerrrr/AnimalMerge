using Code.Pathfinding;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code.Infrastructure.Factories.Nodes
{
    public class PathNodeFactory : IPathNodeFactory
    {
        private readonly IAssetProvider _assetProvider;
        private PathNode _pathNodePrefab;

        [Inject]
        public PathNodeFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public PathNode Create(int x, int y, Grid grid)
        {
            _pathNodePrefab ??= _assetProvider.Load<PathNode>(AssetPath.PathNode);

            PathNode pathNode = Object.Instantiate(_pathNodePrefab);
            pathNode.Construct(x, y, grid);

            return pathNode;
        }

        public PathNode Create(Vector3 position, Transform parent, int x, int y, Grid grid)
        {
            _pathNodePrefab ??= _assetProvider.Load<PathNode>(AssetPath.PathNode);

            Vector3 offset = parent.position;

            PathNode pathNode = Object.Instantiate(_pathNodePrefab, position, Quaternion.identity, parent);
            pathNode.Construct(x + Mathf.RoundToInt(offset.x), y + Mathf.RoundToInt(offset.z), grid);

            return pathNode;
        }
    }
}