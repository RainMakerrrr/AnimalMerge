using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Logic.Animals.Spawn
{
    public class AnimalSpawner : MonoBehaviour
    {
        [SerializeField] private Animal _testPrefab;
        [SerializeField] private MergeBoard _board;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnAnimal();
            }
        }

        private void SpawnAnimal()
        {
            Tile[] emptyTiles = _board.Tiles.Where(tile => tile.IsEmpty).ToArray();
            if (emptyTiles.Length == 0) return;
            
            Tile randomTile = emptyTiles[Random.Range(0, emptyTiles.Length)];
            
            if (randomTile.CanAssignAnimal(_testPrefab.TilesCount, out List<Tile> tiles))
            {
                Animal animal = Instantiate(_testPrefab);
                animal.PlaceOnTile(tiles);
                animal.Tiles = tiles.ToArray();
                
                foreach (Tile tile in tiles)
                {
                    tile.AssignAnimal(animal);
                }
            }
        }
    }
}