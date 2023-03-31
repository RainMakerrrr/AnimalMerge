using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Grid = Code.Pathfinding.Grid;

namespace Code.Infrastructure.Factories.Abilities
{
    public class AbilityFactory
    {
        private readonly Grid _grid;
        private readonly IAnimalFactory _animalFactory;

        public AbilityFactory(Grid grid, IAnimalFactory animalFactory)
        {
            _grid = grid;
            _animalFactory = animalFactory;
        }

       // public Dodge CreateDodge(ITransformable transformable, Collider collider, int probability) => new Dodge(transformable, collider, probability);

        //public CounterAttack CreateCounterAttack(AnimalHealth health, int probability) => new CounterAttack(health, probability);

        public MultipleCharacters CreateMultipleCharactersAbility(AnimalMovement movement, AnimalType animalType,
            int count) =>
            new MultipleCharacters(movement, _grid, _animalFactory, animalType, count);
    }
}