using System;
using UnityEngine;

namespace Code.Pathfinding
{
    [Serializable]
    public class SubTile
    {
        public bool blocked;
        public GridTile tile_s;
        public int num;
    }
}