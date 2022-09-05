using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Logic.Boards
{
    public class GameBoard : MonoBehaviour
    {
        [SerializeField] private Tile _gameTilePrefab;
        [SerializeField] private Vector2Int _size;

        private readonly List<Tile> _tiles = new List<Tile>();

        private void Start()
        {
            SpawnTiles();
        }
        
        private void SpawnTiles()
        {
            for (int i = 0; i < _size.x; i++)
            {
                for (int j = 0; j < _size.y; j++)
                {
                    Vector3 spawnPosition = new Vector3(i + transform.position.x, 0f, j + transform.position.z);

                    Tile tile = Instantiate(_gameTilePrefab, spawnPosition, Quaternion.identity, transform);
                    tile.Position = new Vector2Int((int) spawnPosition.x, (int) spawnPosition.z);

                    _tiles.Add(tile);
                }
            }
        }
    }
}