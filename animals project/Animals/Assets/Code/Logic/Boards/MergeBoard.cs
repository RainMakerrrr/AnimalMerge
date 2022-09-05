using System.Collections.Generic;
using UnityEngine;

namespace Code.Logic.Boards
{
    public class MergeBoard : MonoBehaviour
    {
        [SerializeField] private Tile _tilePrefab;
        [SerializeField] private Vector2Int _boardSize;

        private readonly List<Tile> _tiles = new List<Tile>();
        public IReadOnlyCollection<Tile> Tiles => _tiles;


        private void Start()
        {
            SpawnTiles();
        }

        private void SpawnTiles()
        {
            for (int i = 0; i < _boardSize.x; i++)
            {
                for (int j = 0; j < _boardSize.y; j++)
                {
                    Tile tile = Instantiate(_tilePrefab, new Vector3(i, 0, j), Quaternion.identity, transform);
                    tile.Position = new Vector2Int(i, j);
                    tile.name = $"Tile {i}, {j}";
                    
                    _tiles.Add(tile);
                }
            }
            
            _tiles.ForEach(tile => tile.TryFindNeighbours());
        }
    }
}