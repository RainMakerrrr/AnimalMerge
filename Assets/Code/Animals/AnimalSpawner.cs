using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code.Animals
{
    public class AnimalSpawner : MonoBehaviour
    {
        [SerializeField] private Grid _mergeGrid;
        [SerializeField] private Grid _gameGrid;

        private IAnimalFactory _factory;

        private readonly AnimalType[] _animalTypes = new[] {AnimalType.Cheetah, AnimalType.Fox, AnimalType.Hedgehog};

        private readonly List<AnimalFacade> _animals = new List<AnimalFacade>();
        public IReadOnlyList<AnimalFacade> Animals => _animals.Where(animal => animal.gameObject.activeInHierarchy).ToList();

        [Inject]
        private void Construct(IAnimalFactory factory)
        {
            _factory = factory;
        }

        private void Start()
        {
            _factory.Load();
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

                if (_mergeGrid.HasNodeFor(animalType))
                {
                    AnimalFacade animal = _factory.Create(animalType);
                    _animals.Add(animal);
                    //animal.transform.position = _spawnPoint;

                    _mergeGrid.PlaceOnGrid(animal.GetComponent<AnimalMovement>());

                    _counter++;
                }
            }
        }
    }
}