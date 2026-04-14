using Code.Abilities;
using Code.Animals.Merge.MergeSkills;
using ModestTree;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class HedgehogFacade : PlayerAnimalFacade
    {
        public override void InitBehaviours()
        {
            Ability = new CounterAttack(_health, Animator, AttackInstance, isOwner: true, _randomProvider);
            MergeSkill = new HedgehogMergeSkill();

            //MergeSkills.Add(MergeSkill);

            Debug.Log($"ME - {gameObject.name}, my merge skills - {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);

            // Register in facade's AbilityManager (used by both AnimalAttack and AnimalHealth)
            AddAbility(Ability);
        }
    }
}