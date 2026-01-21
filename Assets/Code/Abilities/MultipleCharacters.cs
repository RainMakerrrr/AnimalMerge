using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using ModestTree;
using UnityEngine;

namespace Code.Abilities
{
    public class MultipleCharacters : IAbility
    {
        private readonly AnimalMovement _movement;
        private readonly IGridManager _gridManager;
        private readonly IAnimalFactory _animalFactory;
        private readonly AnimalType _animalType;
        private readonly int _additionalCharactersCount;

        private readonly AnimalFacade[] _additionalCharacters;

        public bool IsBlockingDamage => false;
        public int Priority => 2;
        public bool CanUse => true;

        public MultipleCharacters(AnimalMovement movement, IGridManager gridManager, IAnimalFactory animalFactory,
            AnimalType animalType, int additionalCharactersCount)
        {
            _animalFactory = animalFactory;
            _animalType = animalType;
            _additionalCharactersCount = additionalCharactersCount;
            _movement = movement;
            _gridManager = gridManager;
            _additionalCharacters = new AnimalFacade[_additionalCharactersCount];
        }

        public void Apply()
        {
            GridCell currentCell = _movement.CurrentPathNode;
            if (currentCell == null) return;

            List<Vector2Int[]> possibleNodesPositions = GetPossibleNodesPositions(currentCell);

            List<GridCell> freeCells = FindFreeCells(possibleNodesPositions);
            if (freeCells == null || freeCells.Count < _additionalCharactersCount) return;

            CreateAdditionalCharacters(freeCells);
        }

        private void CreateAdditionalCharacters(IReadOnlyList<GridCell> freeCells)
        {
            Debug.Log("Create new chars");

            for (int i = 0; i < freeCells.Count; i++)
            {
                var animal = _animalFactory.Create(_animalType);
                AnimalMovement animalMovement = animal.GetComponent<AnimalMovement>();

                animalMovement.SetCurrentNode(freeCells[i]);
                animalMovement.Place(freeCells[i].WorldPosition);

                freeCells[i].IsWalkable = false;

                _additionalCharacters[i] = animal;
            }

            List<AnimalMovement> allAnimals =
                new List<AnimalMovement>(_additionalCharacters.Select(animal => animal.GetComponent<AnimalMovement>()))
                    {_movement};

            foreach (AnimalMovement animal in allAnimals)
            {
                animal.AddAdditionalAnimalsRange(allAnimals.Except(animal));
            }
        }

        private List<GridCell> FindFreeCells(List<Vector2Int[]> possibleNodesPositions)
        {
            List<GridCell> cells = new List<GridCell>();

            foreach (Vector2Int[] possibleNodesPosition in possibleNodesPositions)
            {
                foreach (Vector2Int position in possibleNodesPosition)
                {
                    GridCell cell = _gridManager.GetCell(position.x, position.y);

                    if (cell != null && cell.IsWalkable && cell.CanPlace)
                    {
                        cells.Add(cell);
                    }
                }

                if (cells.Count == _additionalCharactersCount)
                {
                    return cells;
                }

                cells.Clear();
            }

            return null;
        }

        private List<Vector2Int[]> GetPossibleNodesPositions(GridCell cell)
        {
            List<Vector2Int[]> result = new List<Vector2Int[]>
            {
                new[]
                {
                    new Vector2Int(cell.X, cell.Y + 1),
                    new Vector2Int(cell.X + 1, cell.Y),
                    new Vector2Int(cell.X + 1, cell.Y + 1)
                },
                new[]
                {
                    new Vector2Int(cell.X, cell.Y + 1),
                    new Vector2Int(cell.X - 1, cell.Y),
                    new Vector2Int(cell.X - 1, cell.Y + 1)
                },
                new[]
                {
                    new Vector2Int(cell.X, cell.Y + 1),
                    new Vector2Int(cell.X - 1, cell.Y),
                    new Vector2Int(cell.X - 1, cell.Y + 1)
                },
                new[]
                {
                    new Vector2Int(cell.X, cell.Y - 1),
                    new Vector2Int(cell.X + 1, cell.Y),
                    new Vector2Int(cell.X + 1, cell.Y - 1)
                },
                new[]
                {
                    new Vector2Int(cell.X, cell.Y - 1),
                    new Vector2Int(cell.X - 1, cell.Y),
                    new Vector2Int(cell.X - 1, cell.Y - 1)
                }
            };


            return result;
        }
    }
}