using System;
using System.Collections.Generic;
using Code.Animals.Movement;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;
using Random = UnityEngine.Random;

namespace Code.Animals
{
    public class AnimalSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 _spawnPoint;
        [SerializeField] private AnimalMover _mover;
        [SerializeField] private Grid _mergeGrid;
        [SerializeField] private Grid _gameGrid;

        private IAnimalFactory _factory;

        private readonly AnimalType[] _animalTypes = new[] {AnimalType.Chicken, AnimalType.Elephant, AnimalType.Fox};

        private readonly List<Animal> _animals = new List<Animal>();
        public List<Animal> _enemies = new List<Animal>();

        public IReadOnlyList<Animal> Animals => _animals;

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
                    Animal animal = _factory.Create(animalType);
                    _animals.Add(animal);
                    //animal.transform.position = _spawnPoint;

                    _mergeGrid.PlaceOnGrid(animal.GetComponent<AnimalMovement>());

                    _counter++;
                }
            }
        }
    }
}