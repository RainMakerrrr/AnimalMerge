using Code.Abilities;
using Code.Animals.Merge.MergeSkills;
using Code.Data.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals.Facades
{
    public class FoxFacade : PlayerAnimalFacade
    {
        private AnimalDatabase _database;

        [Inject]
        private void ConstructDatabase(AnimalDatabase database) => _database = database;

        public override void InitBehaviours()
        {
            var ownerDodgeChance = FoxStats.DefaultOwnerDodgeChance;
            var inheritedDodgeChance = FoxStats.DefaultInheritedDodgeChance;

            if (_database.GetStats(Type) is FoxStats stats)
            {
                ownerDodgeChance = stats.OwnerDodgeChance;
                inheritedDodgeChance = stats.InheritedDodgeChance;
            }
            else
            {
                Debug.LogWarning($"[FoxFacade] Stats for {Type} are not {nameof(FoxStats)}; using default dodge chances.");
            }

            Ability = new Dodge(_movement, Colliders, ownerDodgeChance, _randomProvider);
            MergeSkill = new FoxMergeSkill(inheritedDodgeChance);

            //MergeSkills.Add(MergeSkill);
            // Register in facade's AbilityManager (used by both AnimalAttack and AnimalHealth)
            AddAbility(Ability);

            Debug.Log($"ME - {gameObject.name}, my merge skills - {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);
        }
    }
}