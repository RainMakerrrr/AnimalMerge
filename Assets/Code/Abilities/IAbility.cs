using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Infrastructure.Factories.Animals;
using Code.Pathfinding;
using ModestTree;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Grid = Code.Pathfinding.Grid;

namespace Code.Abilities
{
    public interface IAbility
    {
        bool CanUse { get; }
        void Apply();
    }

    public class CounterAttack : IAbility
    {
        private readonly AnimalHealth _health;

        public bool CanUse => Random.Range(0, 2) > 0;

        public CounterAttack(AnimalHealth health)
        {
            _health = health;
        }

        public void Apply()
        {
            _health.LastAttack.GetComponent<IDamageable>().TakeDamage(_health.GetComponent<AnimalAttack>());
        }
    }

    public class Dodge : IAbility
    {
        private readonly ITransformable _transformable;
        private int _counter;

        public bool CanUse
        {
            get
            {
                if (_counter == 0) return true;
                return Random.Range(0, 101) > 20;
            }
        }

        public Dodge(ITransformable transformable)
        {
            _transformable = transformable;
        }

        public void Apply()
        {
            _counter++;
            _transformable.Shift();
        }
    }

    public class MultipleCharacters : IAbility
    {
        private const int MaxAdditionalCharactersCount = 3;

        private readonly AnimalMovement _movement;
        private readonly Grid _gameGrid;
        private readonly IAnimalFactory _animalFactory;
        private readonly AnimalType _animalType;

        private readonly Animal[] _additionalCharacters = new Animal[MaxAdditionalCharactersCount];


        public bool CanUse => true;

        public MultipleCharacters(AnimalMovement movement, Grid gameGrid, IAnimalFactory animalFactory,
            AnimalType animalType)
        {
            _animalFactory = animalFactory;
            _animalType = animalType;
            _movement = movement;
            _gameGrid = gameGrid;
        }

        public void Apply()
        {
            PathNode pathNode = _movement.CurrentPathNode;
            if (pathNode == null) return;

            List<Vector2Int[]> possibleNodesPositions = GetPossibleNodesPositions(pathNode);

            List<PathNode> freeNodes = FindFreeNodes(possibleNodesPositions);
            if (freeNodes == null || freeNodes.Count < MaxAdditionalCharactersCount) return;

            CreateAdditionalCharacters(freeNodes);
        }

        private void CreateAdditionalCharacters(IReadOnlyList<PathNode> freeNodes)
        {
            Debug.Log("Create new chars");

            for (int i = 0; i < freeNodes.Count; i++)
            {
                Animal animal = _animalFactory.Create(_animalType);
                AnimalMovement animalMovement = animal.GetComponent<AnimalMovement>();

                animalMovement.SetCurrentNode(freeNodes[i]);
                animalMovement.Place(freeNodes[i].WorldPosition);

                freeNodes[i].IsWalkable = false;

                _additionalCharacters[i] = animal;
            }

            //_movement.AddAdditionalAnimalsRange(_additionalCharacters.Select(animal => animal.GetComponent<AnimalMovement>()));

            List<AnimalMovement> allAnimals =
                new List<AnimalMovement>(_additionalCharacters.Select(animal => animal.GetComponent<AnimalMovement>()))
                    {_movement};

            foreach (AnimalMovement animal in allAnimals)
            {
                animal.AddAdditionalAnimalsRange(allAnimals.Except(animal));
            }
        }

        private List<PathNode> FindFreeNodes(List<Vector2Int[]> possibleNodesPositions)
        {
            List<PathNode> nodes = new List<PathNode>();

            foreach (Vector2Int[] possibleNodesPosition in possibleNodesPositions)
            {
                foreach (Vector2Int position in possibleNodesPosition)
                {
                    PathNode node = _gameGrid.GetGridObject(position.x, position.y);

                    if (node != null && node.IsWalkable && node.CanPlace)
                    {
                        nodes.Add(node);
                    }
                }

                if (nodes.Count == MaxAdditionalCharactersCount)
                {
                    return nodes;
                }

                nodes.Clear();
            }

            return null;
        }

        private List<Vector2Int[]> GetPossibleNodesPositions(PathNode pathNode)
        {
            List<Vector2Int[]> result = new List<Vector2Int[]>
            {
                new[]
                {
                    new Vector2Int(pathNode.x, pathNode.y + 1),
                    new Vector2Int(pathNode.x + 1, pathNode.y),
                    new Vector2Int(pathNode.x + 1, pathNode.y + 1)
                },
                new[]
                {
                    new Vector2Int(pathNode.x, pathNode.y + 1),
                    new Vector2Int(pathNode.x - 1, pathNode.y),
                    new Vector2Int(pathNode.x - 1, pathNode.y + 1)
                },
                new[]
                {
                    new Vector2Int(pathNode.x, pathNode.y + 1),
                    new Vector2Int(pathNode.x - 1, pathNode.y),
                    new Vector2Int(pathNode.x - 1, pathNode.y + 1)
                },
                new[]
                {
                    new Vector2Int(pathNode.x, pathNode.y - 1),
                    new Vector2Int(pathNode.x + 1, pathNode.y),
                    new Vector2Int(pathNode.x + 1, pathNode.y - 1)
                },
                new[]
                {
                    new Vector2Int(pathNode.x, pathNode.y - 1),
                    new Vector2Int(pathNode.x - 1, pathNode.y),
                    new Vector2Int(pathNode.x - 1, pathNode.y - 1)
                }
            };


            return result;
        }
    }
}