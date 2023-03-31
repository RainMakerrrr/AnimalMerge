using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Pathfinding;
using UnityEngine;
using Grid = Code.Pathfinding.Grid;

namespace Code
{
    public class TestEnemiesSpawner : MonoBehaviour
    {
        [SerializeField] private AnimalFacade[] _animals;
        [SerializeField] private Grid _gameGrid;

        private readonly List<AnimalFacade> _animalsInstances = new List<AnimalFacade>();

        public IReadOnlyList<AnimalFacade> AnimalInstances => _animalsInstances;

        private void Start()
        {
            PathNode gridObject = _gameGrid.GetGridObject(3, 9);
            _animals[0].Movement.Place(gridObject.WorldPosition);
            
            _animals[0].Movement.SetCurrentNode(gridObject);

            List<PathNode> neighbours = gridObject.GetNeighbours(_animals[0].Movement.ObjectSizeType, _animals[0].Movement.Direction);
            if (neighbours.Count == 0)
            {
                gridObject.IsWalkable = false;
            }
            else
            {
                neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

                _animals[0].Movement.FillNodes(neighbours);
            }
            
            _animalsInstances.Add(_animals[0]);
        }
    }
}