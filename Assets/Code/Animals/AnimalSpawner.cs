using System;
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

        private readonly AnimalType[] _animalTypes = new[] {AnimalType.Elephant, AnimalType.Cheetah};
        
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
                    //animal.transform.position = _spawnPoint;
                    
                    _mergeGrid.PlaceOnGrid(animal.GetComponent<AnimalMovement>());
                }
            }
        }
    }
}