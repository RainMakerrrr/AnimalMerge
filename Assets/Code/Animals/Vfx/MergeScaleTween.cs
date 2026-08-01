using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public static class MergeScaleTween
    {
        public static async UniTask RunAsync(
            Transform target,
            Vector3 from,
            Vector3 to,
            float duration,
            AnimationCurve curve,
            CancellationToken cancellationToken)
        {
            if (target == null)
                return;

            var elapsed = 0f;

            while (true)
            {
                var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

                target.localScale = Vector3.LerpUnclamped(from, to, Evaluate(curve, progress));

                if (progress >= 1f)
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (target == null)
                    return;

                elapsed += Time.deltaTime;
            }
        }

        private static float Evaluate(AnimationCurve curve, float progress)
        {
            if (curve == null || curve.length == 0)
                return progress;

            return curve.Evaluate(progress);
        }
    }
}
