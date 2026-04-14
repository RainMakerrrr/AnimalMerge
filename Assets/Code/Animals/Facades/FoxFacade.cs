using Code.Abilities;
using Code.Animals.Merge.MergeSkills;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class FoxFacade : PlayerAnimalFacade
    {
        public override void InitBehaviours()
        {
            Ability = new Dodge(_movement, Colliders, isOwner: true, _randomProvider);
            MergeSkill = new FoxMergeSkill();

            //MergeSkills.Add(MergeSkill);
            // Register in facade's AbilityManager (used by both AnimalAttack and AnimalHealth)
            AddAbility(Ability);

            Debug.Log($"ME - {gameObject.name}, my merge skills - {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);
        }
    }
}