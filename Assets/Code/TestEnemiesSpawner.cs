using System.Collections.Generic;
using Code.Animals.Facades;
using Code.GridPathfinding;
using Code.Pathfinding;
using UnityEngine;
using Zenject;

namespace Code
{
    public class TestEnemiesSpawner : MonoBehaviour
    {
        [SerializeField] private AnimalFacade[] _animals;

        private IGridManager _gridManager;
        private readonly List<AnimalFacade> _animalsInstances = new List<AnimalFacade>();

        public IReadOnlyList<AnimalFacade> AnimalInstances => _animalsInstances;

        [Inject]
        private void Construct(IGridManager gridManager)
        {
            _gridManager = gridManager;
        }

        private void Start()
        {
            GridCell gridCell = _gridManager.GetCell(3, 9);
            _animals[0].Movement.Place(gridCell.WorldPosition);

            _animals[0].Movement.SetCurrentNode(gridCell);

            UnitSize unitSize = _animals[0].Movement.ObjectSizeType.ToUnitSize();
            Direction direction = _animals[0].Movement.Direction.ToDirection();
            List<GridCell> neighbours = _gridManager.GetNeighborCells(gridCell.GridPosition, unitSize, direction);

            if (neighbours.Count == 0)
            {
                gridCell.IsWalkable = false;
                gridCell.UpdateVisual();
            }
            else
            {
                neighbours.ForEach(neighbour =>
                {
                    neighbour.IsWalkable = false;
                    neighbour.UpdateVisual();
                });

                gridCell.IsWalkable = false;
                gridCell.UpdateVisual();
                
                _animals[0].Movement.FillNodes(neighbours);
            }

            _animalsInstances.Add(_animals[0]);
        }
    }
}