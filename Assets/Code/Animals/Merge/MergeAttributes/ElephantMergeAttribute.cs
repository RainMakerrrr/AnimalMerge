using Code.Animals.Vfx;
using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class ElephantMergeAttribute : VisualMergeAttribute
    {
        private const float FallbackScaleMultiplier = 1.5f;

        private float _appliedMultiplier;
        private MergeScaleAnimator _scaleAnimator;

        public override void Apply(Transform target)
        {
            base.Apply(target);

            _appliedMultiplier = AnimationConfig != null
                ? AnimationConfig.GrowScaleMultiplier
                : FallbackScaleMultiplier;

            var duration = AnimationConfig != null ? AnimationConfig.GrowDuration : 0f;
            var curve = AnimationConfig != null ? AnimationConfig.GrowCurve : null;

            var animator = ResolveScaleAnimator(createIfMissing: true);
            animator.ScaleBy(_appliedMultiplier, duration, curve);

            Debug.Log($"[ElephantMergeAttribute] Growing {Target.name} by {_appliedMultiplier}x over {duration}s");
        }

        public override void Undo()
        {
            if (Target != null && _appliedMultiplier > 0f)
            {
                var animator = ResolveScaleAnimator(createIfMissing: false);

                if (animator != null)
                {
                    animator.UndoScaleBy(_appliedMultiplier);
                    Debug.Log($"[ElephantMergeAttribute] Restored scale to {animator.LogicalScale} on {Target.name}");
                }
            }

            _appliedMultiplier = 0f;
            _scaleAnimator = null;
            base.Undo();
        }

        private MergeScaleAnimator ResolveScaleAnimator(bool createIfMissing)
        {
            if (_scaleAnimator != null && _scaleAnimator.transform == Target)
                return _scaleAnimator;

            _scaleAnimator = Target.GetComponent<MergeScaleAnimator>();

            if (_scaleAnimator == null && createIfMissing)
                _scaleAnimator = Target.gameObject.AddComponent<MergeScaleAnimator>();

            return _scaleAnimator;
        }
    }
}
