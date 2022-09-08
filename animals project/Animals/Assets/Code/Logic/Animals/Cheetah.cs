using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using UnityEngine;

namespace Code.Logic.Animals
{
    public class Cheetah : Animal
    {
        public override void PlaceOnTile(IEnumerable<Tile> tiles)
        {
            Tile lowerTile = tiles.OrderBy(tile => tile.Position.y).FirstOrDefault();
            if (lowerTile == null) return;
            
            transform.position = new Vector3(lowerTile.Position.x, 0f, lowerTile.Position.y);
        }
        
        [ContextMenu("Move")]
        private void Move()
        {
            Tile upperTile = GetUpperTile();
            
            int i = 0;
            Tile current = upperTile;
            Tile next = current.Next;
            
            current.ReleaseAnimal();

            while (i != TilesPerStep)
            {
                next = current.Next;
                current = next;
                
                i++;
            }

            if (next.CanAssignAnimal(TilesCount, out List<Tile> neighbours))
            {
                
                PlaceOnTile(neighbours);
                Tiles = neighbours.ToArray();

                foreach (Tile neighbour in neighbours)
                {
                    neighbour.AssignAnimal(this);
                }
            }
        }

        private Tile GetUpperTile()
        {
            return Tiles.OrderByDescending(tile => tile.Position.y).FirstOrDefault();
        }
    }
}