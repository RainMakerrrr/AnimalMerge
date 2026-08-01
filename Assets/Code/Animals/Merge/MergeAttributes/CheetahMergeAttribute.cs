using System;
using System.Threading;
using Code.Animals.Vfx;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class CheetahMergeAttribute : VisualMergeAttribute
    {
        [SerializeField] private Material _cheetahMaterial;
        [SerializeField] private Material _glowMaterial;
        [SerializeField] private float _sweepDuration = 0.4f;
        [SerializeField] private float _swapPause = 0.05f;
        [SerializeField] private Vector3 _sweepAxis = Vector3.up;
        [SerializeField] private float _bandsPerBody = 0.5f;
        [SerializeField] private float _bandMargin = 0.5f;
        [SerializeField] private AnimationCurve _sweepEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private RendererMaterialOverlay _overlay;
        private CancellationTokenSource _cts;

        private MergeSweepGlow CurrentGlow => _overlay?.Glow;

        public override void Apply(Transform target)
        {
            FinishRunningSequenceImmediately();

            base.Apply(target);

            var renderers = RendererMaterialOverlay.CollectPaintableRenderers(Target);

            if (renderers.Count == 0)
            {
                Debug.LogWarning($"[CheetahMergeAttribute] No paintable renderers on {Target.name}");
                return;
            }

            _overlay = new RendererMaterialOverlay(_glowMaterial);
            _overlay.Capture(renderers);

            if (_glowMaterial == null)
            {
                _overlay.Apply(_cheetahMaterial, false);
                Debug.Log($"[CheetahMergeAttribute] Applied cheetah material to {renderers.Count} renderers on {Target.name}");
                return;
            }

            _overlay.BeginGlow(Target, renderers, _sweepAxis, _bandsPerBody, _bandMargin, _sweepEase);
            _overlay.Apply(null, true);

            _cts = new CancellationTokenSource();
            RunSweepSequenceAsync(CurrentGlow, _cts.Token).Forget();
        }

        public override void Undo()
        {
            CancelSequence();

            if (_overlay != null)
            {
                _overlay.DisposeGlow();
                _overlay.RestoreOriginalMaterials();
                _overlay = null;
            }

            Debug.Log("[CheetahMergeAttribute] Restored original materials");
            base.Undo();
        }

        private void OnDestroy()
        {
            FinishRunningSequenceImmediately();
        }

        private async UniTaskVoid RunSweepSequenceAsync(MergeSweepGlow glow, CancellationToken cancellationToken)
        {
            await glow.PlayPassAsync(false, _sweepDuration, cancellationToken);

            if (glow != CurrentGlow)
                return;

            _overlay.Apply(_cheetahMaterial, true);

            if (_swapPause > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_swapPause), cancellationToken: cancellationToken);

            await glow.PlayPassAsync(true, _sweepDuration, cancellationToken);

            if (glow != CurrentGlow)
                return;

            _overlay.DisposeGlow();
            _overlay.Apply(_cheetahMaterial, false);

            Debug.Log("[CheetahMergeAttribute] Merge sweep finished, cheetah material kept");
        }

        private void FinishRunningSequenceImmediately()
        {
            CancelSequence();

            if (_overlay == null)
                return;

            _overlay.Apply(_cheetahMaterial, false);
            _overlay.DisposeGlow();
            _overlay.ForgetCapturedMaterials();
            _overlay = null;
        }

        private void CancelSequence()
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
