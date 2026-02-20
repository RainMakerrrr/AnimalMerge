using Code.Abilities;
using Code.Animals.Facades;
using UnityEngine;

namespace Code.Animals.Merge.MergeSkills
{
    public interface IMergeSkill
    {
        AnimalType AnimalType { get; }
        void Merge(AnimalFacade animal);
        void Merge(AnimalFacade animal, AnimalFacade other);
    }

    public class ElephantMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Elephant;
        private readonly float _multiplier;

        public ElephantMergeSkill(float multiplier)
        {
            _multiplier = multiplier;
        }

        public void Merge(AnimalFacade animal)
        {
            animal.UpgradeHealth(_multiplier);
        }

        public void Merge(AnimalFacade animal, AnimalFacade other)
        {
            animal.UpgradeHealth(_multiplier);

            foreach (IMergeSkill mergeSkill in other.MergeSkills)
            {
                mergeSkill.Merge(animal);
            }
        }
    }

    public class CheetahMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Cheetah;

        private readonly int _multiplier;


        public CheetahMergeSkill(int multiplier)
        {
            _multiplier = multiplier;
        }

        public void Merge(AnimalFacade animal)
        {
            animal.UpgradeSpeed(_multiplier);
        }

        public void Merge(AnimalFacade animal, AnimalFacade other)
        {
            animal.UpgradeSpeed(_multiplier);

            foreach (IMergeSkill mergeSkill in other.MergeSkills)
            {
                mergeSkill.Merge(animal);
            }
        }
    }

    public class DeerMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Deer;

        private readonly float _multiplier;

        public DeerMergeSkill(float multiplier)
        {
            _multiplier = multiplier;
        }

        public void Merge(AnimalFacade animal)
        {
            animal.UpgradeDamage(_multiplier);
        }

        public void Merge(AnimalFacade animal, AnimalFacade other)
        {
            animal.UpgradeDamage(_multiplier);

            foreach (IMergeSkill mergeSkill in other.MergeSkills)
            {
                mergeSkill.Merge(animal);
            }
        }
    }

    public class HedgehogMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Hedgehog;

        public void Merge(AnimalFacade animal)
        {
            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);

            animal.MergeSkills.ForEach(Debug.Log);

            animal.AddAbility(new CounterAttack(animal.Health, animal.Animator, animal.AttackInstance, isOwner: false, animal.RandomProvider));
        }

        public void Merge(AnimalFacade animal, AnimalFacade other)
        {
            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);

            animal.AddAbility(new CounterAttack(animal.Health, animal.Animator, animal.AttackInstance, isOwner: false, animal.RandomProvider));

            foreach (IMergeSkill mergeSkill in other.MergeSkills)
            {
                mergeSkill.Merge(animal);
            }
        }
    }

    public class FoxMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Fox;

        public void Merge(AnimalFacade animal)
        {
            animal.AddAbility(new Dodge(animal.Movement, animal.Colliders, isOwner: false, animal.RandomProvider));

            Debug.Log(animal.gameObject.name);
            animal.MergeSkills.ForEach(Debug.Log);

            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);
        }

        public void Merge(AnimalFacade animal, AnimalFacade other)
        {
            if (animal == null)
                return;

            animal.AddAbility(new Dodge(animal.Movement, animal.Colliders, isOwner: false, animal.RandomProvider));

            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);

            if (other == null)
                return;

            foreach (IMergeSkill mergeSkill in other.MergeSkills)
            {
                mergeSkill.Merge(animal);
            }
        }
    }
}