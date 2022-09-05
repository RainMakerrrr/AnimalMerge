using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using UnityEngine;

namespace Code.Logic.Animals
{
    public class Fox : Animal
    {
        public override void PlaceOnTile(IEnumerable<Tile> tiles)
        {
            Tile lowerTile = tiles.OrderBy(tile => tile.Position.y).FirstOrDefault();
            if (lowerTile == null) return;
            
            transform.position = new Vector3(lowerTile.Position.x, 0f, lowerTile.Position.y);
        }
    }
}