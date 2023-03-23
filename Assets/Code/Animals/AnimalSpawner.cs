using System;
using System.Collections.Generic;
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
        
        private IAnimalFactory _factory;

        private readonly AnimalType[] _animalTypes = new[] {AnimalType.Cheetah, AnimalType.Fox, AnimalType.Elephant};

        private readonly List<Animal> _animals = new List<Animal>();

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AnimalType animalType = _animalTypes[Random.Range(0, _animalTypes.Length)];

                if (_mergeGrid.HasNodeFor(animalType))
                {
                    Animal animal = _factory.Create(animalType);
                    _animals.Add(animal);
                    //animal.transform.position = _spawnPoint;
                    
                    _mergeGrid.PlaceOnGrid(animal.GetComponent<AnimalMovement>());
                }
            }
        }
    }
}