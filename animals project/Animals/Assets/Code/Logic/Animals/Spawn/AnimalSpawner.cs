using System;
using System.Collections.Generic;
using System.Linq;
using Code.Infrastructure.Factories.Animals;
using Code.Logic.Boards;
using Code.Logic.MovementModel;
using Pathfinding;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Code.Logic.Animals.Spawn
{
    public class AnimalSpawner : MonoBehaviour
    {
        [SerializeField] private Animal _testPrefab;
        [SerializeField] private MergeBoard _board;
        [SerializeField] private BlockManager _blockManager;

        private IAnimalFactory _animalFactory;

        [Inject]
        private void Construct(IAnimalFactory animalFactory)
        {
            _animalFactory = animalFactory;
        }

        private void Start()
        {
            _animalFactory.Load();
        }

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
                Animal animal = _animalFactory.Create(AnimalType.Elephant);
                
                animal.GetComponent<GridTransformable>().Construct(_blockManager);
                animal.GetComponent<GridTransformable>().InitCurrentGraphNode();
                animal.GetComponent<SingleNodeBlocker>().Construct(_blockManager);
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