using System.Linq;
using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class SlotMergeAttribute : VisualMergeAttribute
    {
        private MergeVisualSlot _slot;

        public override void Apply(Transform target)
        {
            base.Apply(target);

            _slot = target.GetComponentsInChildren<MergeVisualSlot>(true)
                         .FirstOrDefault(s => s.SourceType == Type);

            if (_slot != null)
            {
                _slot.gameObject.SetActive(true);
                Debug.Log($"[SlotMergeAttribute] Activated slot for {Type} on {target.name}");
            }
            else
            {
                Debug.LogWarning($"[SlotMergeAttribute] No MergeVisualSlot with sourceType={Type} found on {target.name}");
            }
        }

        public override void Undo()
        {
            if (_slot != null)
            {
                _slot.gameObject.SetActive(false);
                _slot = null;
            }

            base.Undo();
        }
    }
}
