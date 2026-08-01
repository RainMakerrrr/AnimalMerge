using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals
{
    public class AnimalSpawner : MonoBehaviour, IAnimalSpawner
    {
        [SerializeField] private GridManager _mergeGrid;
        [SerializeField] private GridManager _gameGrid;
        [SerializeField] private AnimalType[] _animalTypes = new[] {AnimalType.Cheetah, AnimalType.Fox, AnimalType.Hedgehog};

        private IAnimalFactory _factory;
        private bool _factoryReady;

        private readonly List<AnimalFacade> _animals = new List<AnimalFacade>();
        public IReadOnlyList<AnimalFacade> Animals => _animals.Where(animal => animal != null && animal.gameObject != null && animal.gameObject.activeInHierarchy).ToList();

        public bool TryPickRandomType(out AnimalType type)
        {
            if (_animalTypes.Length == 0)
            {
                Debug.LogWarning("[AnimalSpawner] No animal types configured - cannot pick a random one");
                type = default;
                return false;
            }

            type = _animalTypes[Random.Range(0, _animalTypes.Length)];
            return true;
        }

        [Inject]
        private void Construct(IAnimalFactory factory)
        {
            _factory = factory;
        }

        public bool HasFreeCellFor(AnimalType type) => _mergeGrid.HasCellFor(type);

        public IReadOnlyList<AnimalFacade> Spawn(AnimalType animalType)
        {
            EnsureFactoryReady();

            var spawned = new List<AnimalFacade>();

            if (!_mergeGrid.HasCellFor(animalType))
            {
                Debug.LogWarning($"[AnimalSpawner] No free cell for {animalType}, skipping");
                return spawned;
            }

            var animal = _factory.Create(animalType);
            _animals.Add(animal);
            spawned.Add(animal);

            // Subscribe to removal event to clean up list when animal is merged/destroyed
            animal.OnRemoved += OnAnimalRemoved;

            _mergeGrid.PlaceOnGrid(animal.Movement);

            Debug.Log($"[AnimalSpawner] Spawned {animalType} at {animal.Movement.CurrentPathNode.GridPosition}");

            // Handle special case for Chicken (spawns additional units)
            if (animalType == AnimalType.Chicken)
            {
                var additionalChickens = _factory.SpawnAdditionalChickens(animal as ChickenFacade);
                foreach (var chicken in additionalChickens)
                {
                    chicken.OnRemoved += OnAnimalRemoved;
                }
                _animals.AddRange(additionalChickens);
                spawned.AddRange(additionalChickens);
                Debug.Log($"[AnimalSpawner] Spawned {additionalChickens.Count} additional chickens");
            }

            return spawned;
        }

        private void EnsureFactoryReady()
        {
            if (_factoryReady)
                return;

            _factory.Load();
            _factory.SetMergeGrid(_mergeGrid);
            _factoryReady = true;
        }

        private void OnAnimalRemoved(AnimalFacade animal)
        {
            if (_animals.Remove(animal))
            {
                Debug.Log($"[AnimalSpawner] Removed {animal.name} from spawner list. Remaining: {_animals.Count}");
            }
            else
            {
                Debug.LogWarning($"[AnimalSpawner] Tried to remove {animal.name} but it wasn't in the list");
            }

            // Unsubscribe to prevent memory leaks
            animal.OnRemoved -= OnAnimalRemoved;
        }

        private void OnDestroy()
        {
            // Unsubscribe from all animals to prevent memory leaks
            foreach (var animal in _animals)
            {
                if (animal != null)
                {
                    animal.OnRemoved -= OnAnimalRemoved;
                }
            }
        }
    }
}
