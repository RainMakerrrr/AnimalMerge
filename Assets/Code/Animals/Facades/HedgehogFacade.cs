using Code.Abilities;
using Code.Animals.Merge.MergeSkills;
using Code.Data.Animals;
using ModestTree;
using UnityEngine;
using Zenject;

namespace Code.Animals.Facades
{
    public class HedgehogFacade : PlayerAnimalFacade
    {
        private AnimalDatabase _database;

        [Inject]
        private void ConstructDatabase(AnimalDatabase database) => _database = database;

        public override void InitBehaviours()
        {
            var ownerCounterChance = HedgehogStats.DefaultOwnerCounterChance;
            var inheritedCounterChance = HedgehogStats.DefaultInheritedCounterChance;

            if (_database.GetStats(Type) is HedgehogStats stats)
            {
                ownerCounterChance = stats.OwnerCounterChance;
                inheritedCounterChance = stats.InheritedCounterChance;
            }
            else
            {
                Debug.LogWarning($"[HedgehogFacade] Stats for {Type} are not {nameof(HedgehogStats)}; using default counter-attack chances.");
            }

            Ability = new CounterAttack(_health, Animator, AttackInstance, ownerCounterChance, _randomProvider);
            MergeSkill = new HedgehogMergeSkill(inheritedCounterChance);

            //MergeSkills.Add(MergeSkill);

            Debug.Log($"ME - {gameObject.name}, my merge skills - {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);

            // Register in facade's AbilityManager (used by both AnimalAttack and AnimalHealth)
            AddAbility(Ability);
        }
    }
}