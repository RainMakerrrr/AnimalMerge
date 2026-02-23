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

        private void Start()
        {
            _factory.Load();
            _factory.SetMergeGrid(_mergeGrid);
        }

        private int _counter;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_counter >= _animalTypes.Length)
                {
                    _counter = 0;
                }

                AnimalType animalType = _animalTypes[_counter];

                if (_mergeGrid.HasCellFor(animalType))
                {
                    AnimalFacade animal = _factory.Create(animalType);
                    _animals.Add(animal);
                    //animal.transform.position = _spawnPoint;

                    _mergeGrid.PlaceOnGrid(animal.GetComponent<AnimalMovement>());

                    _counter++;

                    if (animalType == AnimalType.Chicken)
                    {
                        var additionalChickens = _factory.SpawnAdditionalChickens(animal as ChickenFacade);
                        _animals.AddRange(additionalChickens);
                        Debug.Log($"[AnimalSpawner] Added {additionalChickens.Count} additional chickens to tracked animals list");
                    }
                }
            }
        }
    }
}