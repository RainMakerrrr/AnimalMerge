using System.Collections.Generic;
using Code.Animals.Facades;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals
{
    public class AnimalSpawner : MonoBehaviour, IAnimalSpawner
    {
        [SerializeField] private GridManager _gameGrid;

        private IAnimalFactory _factory;
        private bool _factoryReady;

        [Inject]
        private void Construct(IAnimalFactory factory)
        {
            _factory = factory;
        }

        public bool HasFreeCellFor(AnimalType type) => _gameGrid.HasCellFor(type);

        public IReadOnlyList<AnimalFacade> Spawn(AnimalType animalType)
        {
            EnsureFactoryReady();

            if (animalType == AnimalType.Chicken)
                return SpawnChickenFlock();

            var spawned = new List<AnimalFacade>();

            if (!_gameGrid.HasCellFor(animalType))
            {
                Debug.LogWarning($"[AnimalSpawner] No free cell for {animalType}, skipping");
                return spawned;
            }

            var animal = _factory.Create(animalType);
            if (animal == null)
            {
                Debug.LogError($"[AnimalSpawner] Factory produced no instance for {animalType}");
                return spawned;
            }

            spawned.Add(animal);

            _gameGrid.PlaceOnGrid(animal.Movement);

            Debug.Log($"[AnimalSpawner] Spawned {animalType} at {animal.Movement.CurrentPathNode.GridPosition}");

            return spawned;
        }

        private IReadOnlyList<AnimalFacade> SpawnChickenFlock()
        {
            if (!_gameGrid.TryFindFreePlacement(ChickenFlock.Footprint, Direction.North, out var anchor))
            {
                Debug.LogWarning($"[AnimalSpawner] No free {ChickenFlock.Footprint} block for {AnimalType.Chicken}, skipping");
                return new List<AnimalFacade>();
            }

            var cells = _gameGrid.GetOccupiedCells(anchor, ChickenFlock.Footprint, Direction.North);

            return _factory.CreateChickenFlock(cells);
        }

        private void EnsureFactoryReady()
        {
            if (_factoryReady)
                return;

            _factory.Load();
            _factoryReady = true;
        }
    }
}
