using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using UnityEngine;

namespace Code.Logic.MovementModel
{
    public class ElephantTransformable : GridTransformable
    {
        protected override Vector3 GetTargetPosition()
        {
            List<Tile> lowerTiles = CurrentTiles.OrderBy(tile => tile.Position.y).ToList();

            float center = (float) (lowerTiles[0].Position.x + lowerTiles[1].Position.x) / 2;
            
           return new Vector3(center, 0.01f, lowerTiles.FirstOrDefault()!.transform.position.z);
        }
    }
}