using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals
{
    public class AnimalSpawner : MonoBehaviour
    {
        [SerializeField] private GridManager _mergeGrid;
        [SerializeField] private GridManager _gameGrid;
        [SerializeField] private AnimalType[] _animalTypes = new[] {AnimalType.Cheetah, AnimalType.Fox, AnimalType.Hedgehog};

        private IAnimalFactory _factory;
        
        private readonly List<AnimalFacade> _animals = new List<AnimalFacade>();
        public IReadOnlyList<AnimalFacade> Animals => _animals.Where(animal => animal != null && animal.gameObject != null && animal.gameObject.activeInHierarchy).ToList();

        [Inject]
        private void Construct(IAnimalFactory factory)
        {
            _factory = factory;
        }

        private void SpawnInitialAnimals()
        {
            Debug.Log($"[AnimalSpawner] Spawning {_animalTypes.Length} initial animals");

            foreach (var animalType in _animalTypes)
            {
                SpawnAnimal(animalType);
            }

            Debug.Log($"[AnimalSpawner] Spawned {_animals.Count} animals total");
        }

        private void SpawnAnimal(AnimalType animalType)
        {
            if (!_mergeGrid.HasCellFor(animalType))
            {
                Debug.LogWarning($"[AnimalSpawner] No free cell for {animalType}, skipping");
                return;
            }

            var animal = _factory.Create(animalType);
            _animals.Add(animal);

            // Subscribe to removal event to clean up list when animal is merged/destroyed
            animal.OnRemoved += OnAnimalRemoved;

            _mergeGrid.PlaceOnGrid(animal.GetComponent<AnimalMovement>());

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
                Debug.Log($"[AnimalSpawner] Spawned {additionalChickens.Count} additional chickens");
            }
        }

        public void SpawnAnimals()
        {
            _factory.Load();
            _factory.SetMergeGrid(_mergeGrid);
            SpawnInitialAnimals();
        }

        /// <summary>
        /// Spawns one random animal from available types
        /// </summary>
        public void SpawnRandomAnimal()
        {
            if (_animalTypes.Length == 0)
            {
                Debug.LogWarning("[AnimalSpawner] No animal types configured");
                return;
            }

            var randomIndex = Random.Range(0, _animalTypes.Length);
            var randomType = _animalTypes[randomIndex];

            Debug.Log($"[AnimalSpawner] Spawning random animal: {randomType}");
            SpawnAnimal(randomType);
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