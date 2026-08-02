#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    [CreateAssetMenu(fileName = "AllyAnimalRecipe", menuName = "Data/Ally Animal Recipe")]
    public class AllyAnimalRecipe : AnimalRecipe
    {
        [Header("Merge")]
        [SerializeField] private MonoScript _mergeAttributeScript;
        [SerializeField, Min(0f)] private float _mergePopupOffsetY = 2f;
        [SerializeField, TextArea] private string _mergeInfo;
        [SerializeField] private bool _overrideMergeSkillMultiplier;
        [SerializeField] private float _mergeSkillMultiplier = 1.5f;
        [SerializeField] private bool _overrideMergeTriggerShape;
        [SerializeField] private float _mergeTriggerRadius = 0.5f;
        [SerializeField] private Vector3 _mergeTriggerCenter;
        [SerializeField] private MergeVisualSlotRecipe[] _visualSlots = new MergeVisualSlotRecipe[0];

        public override AnimalSideProfile Side => AnimalSideProfile.Ally;

        public MonoScript MergeAttributeScript => _mergeAttributeScript;
        public float MergePopupOffsetY => _mergePopupOffsetY;
        public string MergeInfo => _mergeInfo;
        public bool OverrideMergeSkillMultiplier => _overrideMergeSkillMultiplier;
        public float MergeSkillMultiplier => _mergeSkillMultiplier;
        public bool OverrideMergeTriggerShape => _overrideMergeTriggerShape;
        public float MergeTriggerRadius => _mergeTriggerRadius;
        public Vector3 MergeTriggerCenter => _mergeTriggerCenter;
        public MergeVisualSlotRecipe[] VisualSlots => _visualSlots;
    }
}
#endif
