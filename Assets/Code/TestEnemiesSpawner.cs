using System.Collections.Generic;
using System.Linq;
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
                var gridCell = _gridManager.GetCell(position.x, position.y) as GridCell;
                animal.Movement.Place(gridCell.WorldPosition);

                animal.Movement.SetCurrentNode(gridCell);

                var unitSize = animal.Movement.UnitSize;
                var direction = animal.Movement.Direction;
                var neighbours = _gridManager.GetNeighborCells(gridCell.GridPosition, unitSize, direction);

                if (neighbours.Count == 0)
                {
                    gridCell.IsWalkable = false;
                    gridCell.UpdateVisual();
                }
                else
                {
                    neighbours.ForEach(neighbour =>
                    {
                        var cell = neighbour as GridCell;
                        if (cell != null)
                        {
                            cell.IsWalkable = false;
                            cell.UpdateVisual();
                        }
                    });

                    gridCell.IsWalkable = false;
                    gridCell.UpdateVisual();

                    animal.Movement.FillNodes(neighbours.Cast<GridCell>().ToList());
                }

                _animalsInstances.Add(animal);
            }
        }
    }
}