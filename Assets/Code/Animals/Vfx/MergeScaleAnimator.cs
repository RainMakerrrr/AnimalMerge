using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public class MergeScaleAnimator : MonoBehaviour
    {
        private Vector3 _logicalScale;
        private bool _hasLogicalScale;
        private CancellationTokenSource _cancellation;

        public Vector3 LogicalScale
        {
            get
            {
                CaptureLogicalScale();
                return _logicalScale;
            }
        }

        private void Awake() => CaptureLogicalScale();

        private void OnDestroy() => CancelRunningTween();

        public void ScaleBy(float multiplier, float duration, AnimationCurve curve)
        {
            CaptureLogicalScale();
            CancelRunningTween();

            var scaleBeforeChange = transform.localScale;
            _logicalScale *= multiplier;

            if (duration <= 0f)
            {
                transform.localScale = _logicalScale;
                return;
            }

            _cancellation = new CancellationTokenSource();

            MergeScaleTween
                .RunAsync(transform, scaleBeforeChange, _logicalScale, duration, curve, _cancellation.Token)
                .Forget();
        }

        public void Resync(Vector3 scale)
        {
            CancelRunningTween();

            _logicalScale = scale;
            _hasLogicalScale = true;
            transform.localScale = scale;
        }

        public void UndoScaleBy(float multiplier)
        {
            CaptureLogicalScale();
            CancelRunningTween();

            if (Mathf.Approximately(multiplier, 0f))
                return;

            _logicalScale /= multiplier;
            transform.localScale = _logicalScale;
        }

        private void CaptureLogicalScale()
        {
            if (_hasLogicalScale)
                return;

            _logicalScale = transform.localScale;
            _hasLogicalScale = true;
        }

        private void CancelRunningTween()
        {
            if (_cancellation == null)
                return;

            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
        }
    }
}
