using Code.Animals.Merge.MergeSkills;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class ElephantFacade : AnimalFacade
    {
        [SerializeField] private float _mergeSkillMultiplier = 1.5f;

        public override void InitBehaviours()
        {
            MergeSkill = new ElephantMergeSkill(_mergeSkillMultiplier);
        }
    }
}