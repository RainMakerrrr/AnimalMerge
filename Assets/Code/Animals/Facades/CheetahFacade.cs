using Code.Animals.Merge.MergeSkills;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class CheetahFacade : PlayerAnimalFacade
    {
        [SerializeField] private int _mergeSkillMultiplier = 2;

        public override void InitBehaviours()
        {
            MergeSkill = new CheetahMergeSkill(_mergeSkillMultiplier);
        }
    }
}