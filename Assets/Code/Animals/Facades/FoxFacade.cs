using Code.Abilities;
using Code.Animals.Merge.MergeSkills;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class FoxFacade : AnimalFacade
    {
        public override void InitBehaviours()
        {
            Ability = new Dodge(_movement, Colliders, 20);
            MergeSkill = new FoxMergeSkill();
            
            //MergeSkills.Add(MergeSkill);
            _health.SetAbility(Ability);
            
            Debug.Log($"ME - {gameObject.name}, my merge skills - {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);
        }
    }
}