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
        [SerializeField] private Vector2Int[] _positions;

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
            for (var i = 0; i < _animals.Length; i++)
            {
                var animal = _animals[i];
                var position = _positions[i];
                GridCell gridCell = _gridManager.GetCell(position.x, position.y);
                animal.Movement.Place(gridCell.WorldPosition);

                animal.Movement.SetCurrentNode(gridCell);

                UnitSize unitSize = animal.Movement.ObjectSizeType.ToUnitSize();
                Direction direction = animal.Movement.Direction.ToDirection();
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

                    animal.Movement.FillNodes(neighbours);
                }

                _animalsInstances.Add(animal);
            }
        }
    }
}