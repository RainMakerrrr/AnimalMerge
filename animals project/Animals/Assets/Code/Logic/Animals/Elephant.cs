using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using UnityEngine;

namespace Code.Logic.Animals
{
    public class Elephant : Animal
    {
        public override void PlaceOnTile(IEnumerable<Tile> tiles)
        {
            List<Tile> lowerTiles = tiles.OrderBy(tile => tile.Position.y).ToList();

            float center = (float) (lowerTiles[0].Position.x + lowerTiles[1].Position.x) / 2;

            transform.position = new Vector3(center, 0f, lowerTiles[0].Position.y);
        }
    }
}