using Code.Logic.Boards;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using UnityEngine;

namespace Code.Infrastructure.Factories.Tiles
{
    public class TileFactory : ITileFactory
    {
        private readonly IAssetProvider _assetProvider;
        private Tile _tilePrefab;

        public TileFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public void Load() => _tilePrefab = _assetProvider.Load<Tile>(AssetPath.Tile);

        public Tile Create() => Object.Instantiate(_tilePrefab);

        public Tile Create(Vector3 position, Transform parent) =>
            Object.Instantiate(_tilePrefab, position, Quaternion.identity, parent);
    }
}