using System.Linq;
using System.Threading;
using Code.Animals.Vfx;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class SlotMergeAttribute : VisualMergeAttribute
    {
        private MergeVisualSlot _slot;
        private MergeAppearAnimation _appearAnimation;
        private CancellationTokenSource _cancellation;

        private void OnDestroy() => StopAppearAnimation();

        public override void Apply(Transform target)
        {
            base.Apply(target);

            _slot = target.GetComponentsInChildren<MergeVisualSlot>(true)
                         .FirstOrDefault(s => s.SourceType == Type);

            if (_slot == null)
            {
                Debug.LogWarning($"[SlotMergeAttribute] No MergeVisualSlot with sourceType={Type} found on {target.name}");
                return;
            }

            StopAppearAnimation();

            var authoredScale = _slot.AuthoredScale;
            _slot.gameObject.SetActive(true);

            Debug.Log($"[SlotMergeAttribute] Activated slot for {Type} on {target.name}");

            if (AnimationConfig == null)
                return;

            _appearAnimation = MergeAppearAnimation.Begin(_slot.transform, authoredScale, AnimationConfig);
            _cancellation = new CancellationTokenSource();
            _appearAnimation.PlayAsync(_cancellation.Token).Forget();
        }

        public override void Undo()
        {
            StopAppearAnimation();

            if (_slot != null)
            {
                _slot.gameObject.SetActive(false);
                _slot = null;
            }

            base.Undo();
        }

        private void StopAppearAnimation()
        {
            if (_cancellation != null)
            {
                _cancellation.Cancel();
                _cancellation.Dispose();
                _cancellation = null;
            }

            if (_appearAnimation == null)
                return;

            _appearAnimation.Dispose();
            _appearAnimation = null;
        }
    }
}
