using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class ElephantMergeAttribute : VisualMergeAttribute
    {
        private const float ScaleFactor = 1.5f;
        private Vector3 _scaleBeforeApply;

        public override void Apply()
        {
            // Store scale before applying
            _scaleBeforeApply = transform.root.localScale;

            // Apply scale increase
            transform.root.localScale *= ScaleFactor;

            Debug.Log($"[ElephantMergeAttribute] Applied scale {ScaleFactor}x (from {_scaleBeforeApply} to {transform.root.localScale})");
        }

        public override void Undo()
        {
            // Restore original scale
            if (_scaleBeforeApply != Vector3.zero)
            {
                transform.root.localScale = _scaleBeforeApply;
                Debug.Log($"[ElephantMergeAttribute] Restored scale to {_scaleBeforeApply}");
            }

            // Call base to deactivate visual GameObject
            base.Undo();
        }
    }
}