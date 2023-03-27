using System;
using System.Collections.Generic;
using Code.Pathfinding;
using UnityEngine;
using Grid = Code.Pathfinding.Grid;

namespace Code
{
    public class TestEnemiesSpawner : MonoBehaviour
    {
        [SerializeField] private AnimalMovement[] _animals;
        [SerializeField] private Grid _gameGrid;

        private List<AnimalMovement> _animalsInstances = new List<AnimalMovement>();

        public IReadOnlyList<AnimalMovement> AnimalInstances => _animalsInstances;

        private void Start()
        {
            PathNode gridObject = _gameGrid.GetGridObject(3, 9);
            _animals[0].Place(gridObject.WorldPosition);
            
            _animals[0].SetCurrentNode(gridObject);

            List<PathNode> neighbours = gridObject.GetNeighbours(_animals[0].ObjectSizeType, _animals[0].Direction);
            if (neighbours.Count == 0)
            {
                gridObject.IsWalkable = false;
            }
            else
            {
                neighbours.ForEach(neighbour => neighbour.IsWalkable = false);

                _animals[0].FillNodes(neighbours);
            }
            
            _animalsInstances.Add(_animals[0]);
        }
    }
}