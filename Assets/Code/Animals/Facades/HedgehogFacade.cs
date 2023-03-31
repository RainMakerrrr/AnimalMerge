using Code.Abilities;
using Code.Animals.Merge.MergeSkills;
using ModestTree;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class HedgehogFacade : AnimalFacade
    {
        public override void InitBehaviours()
        {
            Ability = new CounterAttack(_health, Animator, -1, AttackInstance);
            MergeSkill = new HedgehogMergeSkill();
            
            //MergeSkills.Add(MergeSkill);

            Debug.Log($"ME - {gameObject.name}, my merge skills - {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);
            
            _health.SetAbility(Ability);
        }
    }
}