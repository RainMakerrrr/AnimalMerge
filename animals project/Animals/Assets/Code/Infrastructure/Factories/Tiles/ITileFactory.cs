using Code.Logic.Boards;
using UnityEngine;

namespace Code.Infrastructure.Factories.Tiles
{
    public interface ITileFactory
    {
        void Load();
        Tile Create(TileType tileType);
        Tile Create(Vector3 position, Transform parent);
    }
}