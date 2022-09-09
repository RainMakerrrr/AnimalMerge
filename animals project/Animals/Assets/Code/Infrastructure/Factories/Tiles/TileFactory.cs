using System;
using Code.Logic.Boards;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Infrastructure.Factories.Tiles
{
    public class TileFactory : ITileFactory
    {
        private readonly IAssetProvider _assetProvider;
        private Tile _smallTilePrefab;
        private Tile _mediumTilePrefab;
        private Tile _bigTilePrefab;

        public TileFactory(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public void Load()
        {
            _smallTilePrefab = _assetProvider.Load<Tile>(AssetPath.Tile);
            _mediumTilePrefab = _assetProvider.Load<Tile>(AssetPath.MediumTile);
            _bigTilePrefab = _assetProvider.Load<Tile>(AssetPath.BigTile);
        }
        
        public Tile Create(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Small:
                    return Object.Instantiate(_smallTilePrefab);
                case TileType.Medium:
                    return Object.Instantiate(_mediumTilePrefab);
                case TileType.Big:
                    return Object.Instantiate(_bigTilePrefab);
                default:
                    throw new ArgumentOutOfRangeException(nameof(tileType), tileType, null);
            }
        }
        
        
        public Tile Create(Vector3 position, Transform parent) =>
            Object.Instantiate(_smallTilePrefab, position, Quaternion.identity, parent);
    }
}