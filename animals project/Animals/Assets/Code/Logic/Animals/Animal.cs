using System.Collections.Generic;
using Code.Logic.Boards;
using UnityEngine;

namespace Code.Logic.Animals
{
    public abstract class Animal : MonoBehaviour
    {
        [SerializeField] private int _tilesCount;
        
        public int TilesCount => _tilesCount;
        
        public Tile[] Tiles { get; set; }
        
        public abstract void PlaceOnTile(IEnumerable<Tile> tiles);
    }
}