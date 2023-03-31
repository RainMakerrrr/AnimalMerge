using Code.Animals.Merge.MergeSkills;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class DeerFacade : AnimalFacade
    {
        [SerializeField] private float _mergeSkillMultiplier = 1.5f;

        public override void InitBehaviours()
        {
            MergeSkill = new DeerMergeSkill(_mergeSkillMultiplier);
        }
    }
}