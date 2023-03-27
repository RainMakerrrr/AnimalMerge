using Code.Animals;
using Code.Animals.Health;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;

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
        private readonly IAnimalFactory _animalFactory;
        private readonly AnimalType _animalType;

        public bool CanUse => true;


        public MultipleCharacters(IAnimalFactory animalFactory, AnimalType animalType)
        {
            _animalFactory = animalFactory;
            _animalType = animalType;
        }

        public void Apply()
        {
            _animalFactory.Create(_animalType);
        }
    }
}