using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        public bool CanUse(IAttacker attacker) => true; // MultipleCharacters always works

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

        public async UniTask Apply()
        {
            GridCell currentCell = _movement.CurrentPathNode;
            if (currentCell == null)
            {
                await UniTask.CompletedTask;
                return;
            }

            List<Vector2Int[]> possibleNodesPositions = GetPossibleNodesPositions(currentCell);

            List<GridCell> freeCells = FindFreeCells(possibleNodesPositions);
            if (freeCells == null || freeCells.Count < _additionalCharactersCount)
            {
                await UniTask.CompletedTask;
                return;
            }

            CreateAdditionalCharacters(freeCells);
            await UniTask.CompletedTask;
        }

        private void CreateAdditionalCharacters(IReadOnlyList<GridCell> freeCells)
        {
            Debug.Log("Create new chars");

            for (int i = 0; i < freeCells.Count; i++)
            {
                var animal = _animalFactory.Create(_animalType);
                if (animal == null)
                {
                    Debug.LogError($"[MultipleCharacters] Factory produced no instance for {_animalType}");
                    return;
                }

                var animalMovement = animal.Movement;

                animalMovement.SetCurrentNode(freeCells[i]);
                animalMovement.Place(freeCells[i].WorldPosition);

                freeCells[i].IsWalkable = false;

                _additionalCharacters[i] = animal;
            }

            var allAnimals =
                new List<AnimalMovement>(_additionalCharacters.Select(animal => animal.Movement))
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
                    var cell = _gridManager.GetCell(position.x, position.y) as GridCell;

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