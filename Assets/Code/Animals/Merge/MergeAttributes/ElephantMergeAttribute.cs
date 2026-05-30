using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class ElephantMergeAttribute : VisualMergeAttribute
    {
        private const float ScaleFactor = 1.5f;
        private Vector3 _scaleBeforeApply;

        public override void Apply(Transform target)
        {
            base.Apply(target);
            _scaleBeforeApply = Target.localScale;
            Target.localScale *= ScaleFactor;
            Debug.Log($"[ElephantMergeAttribute] Applied scale {ScaleFactor}x to {Target.name} (from {_scaleBeforeApply} to {Target.localScale})");
        }

        public override void Undo()
        {
            if (_scaleBeforeApply != Vector3.zero && Target != null)
            {
                Target.localScale = _scaleBeforeApply;
                Debug.Log($"[ElephantMergeAttribute] Restored scale to {_scaleBeforeApply} on {Target.name}");
            }
            base.Undo();
        }
    }
}
