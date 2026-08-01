using System;
using System.Collections.Generic;
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

        private RendererState[] _savedState;
        private MergeSweepGlow _glow;
        private CancellationTokenSource _cts;

        public override void Apply(Transform target)
        {
            FinishRunningSequenceImmediately();

            base.Apply(target);

            var renderers = CollectPaintableRenderers();

            if (renderers.Count == 0)
            {
                Debug.LogWarning($"[CheetahMergeAttribute] No paintable renderers on {Target.name}");
                return;
            }

            _savedState = new RendererState[renderers.Count];

            for (var i = 0; i < renderers.Count; i++)
            {
                _savedState[i] = new RendererState
                {
                    Renderer = renderers[i],
                    OriginalSharedMaterials = CaptureOriginalMaterials(renderers[i])
                };
            }

            if (_glowMaterial == null)
            {
                ApplyComposedMaterials(true, false);
                Debug.Log($"[CheetahMergeAttribute] Applied cheetah material to {renderers.Count} renderers on {Target.name}");
                return;
            }

            _glow = new MergeSweepGlow(_glowMaterial, _bandsPerBody, _bandMargin, _sweepEase);
            _glow.Bind(Target, renderers, _sweepAxis);

            ApplyComposedMaterials(false, true);

            _cts = new CancellationTokenSource();
            RunSweepSequenceAsync(_glow, _cts.Token).Forget();
        }

        public override void Undo()
        {
            CancelSequence();
            DisposeGlow();
            RestoreOriginalMaterials();

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

            if (glow != _glow)
                return;

            ApplyComposedMaterials(true, true);

            if (_swapPause > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_swapPause), cancellationToken: cancellationToken);

            await glow.PlayPassAsync(true, _sweepDuration, cancellationToken);

            if (glow != _glow)
                return;

            DisposeGlow();
            ApplyComposedMaterials(true, false);

            Debug.Log("[CheetahMergeAttribute] Merge sweep finished, cheetah material kept");
        }

        private void FinishRunningSequenceImmediately()
        {
            CancelSequence();
            ApplyComposedMaterials(true, false);
            DisposeGlow();
            _savedState = null;
        }

        private void CancelSequence()
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void DisposeGlow()
        {
            if (_glow == null)
                return;

            _glow.Dispose();
            _glow = null;
        }

        private void RestoreOriginalMaterials()
        {
            if (_savedState == null)
                return;

            foreach (var state in _savedState)
            {
                if (state.Renderer != null)
                    state.Renderer.sharedMaterials = state.OriginalSharedMaterials;
            }

            _savedState = null;
        }

        private void ApplyComposedMaterials(bool useCheetahSkin, bool includeGlow)
        {
            if (_savedState == null)
                return;

            foreach (var state in _savedState)
            {
                if (state.Renderer != null)
                    state.Renderer.sharedMaterials = Compose(state, useCheetahSkin, includeGlow);
            }
        }

        private Material[] Compose(RendererState state, bool useCheetahSkin, bool includeGlow)
        {
            var original = state.OriginalSharedMaterials;
            var glowInstance = includeGlow && _glow != null ? _glow.Instance : null;
            var composed = new Material[glowInstance != null ? original.Length + 1 : original.Length];

            for (var i = 0; i < original.Length; i++)
                composed[i] = original[i];

            if (useCheetahSkin && composed.Length > 0)
                composed[0] = _cheetahMaterial;

            if (glowInstance != null)
                composed[original.Length] = glowInstance;

            return composed;
        }

        private Material[] CaptureOriginalMaterials(Renderer renderer)
        {
            var current = renderer.sharedMaterials;
            var overlayShader = _glowMaterial != null ? _glowMaterial.shader : null;

            if (overlayShader == null)
                return current;

            var originals = new List<Material>(current.Length);

            foreach (var material in current)
            {
                if (material != null && material.shader == overlayShader)
                    continue;

                originals.Add(material);
            }

            return originals.ToArray();
        }

        private List<Renderer> CollectPaintableRenderers()
        {
            var paintable = new List<Renderer>();

            foreach (var candidate in Target.GetComponentsInChildren<Renderer>())
            {
                var isMeshBased = candidate is MeshRenderer || candidate is SkinnedMeshRenderer;

                if (isMeshBased && candidate.sharedMaterials.Length > 0)
                    paintable.Add(candidate);
            }

            return paintable;
        }

        private struct RendererState
        {
            public Renderer Renderer;
            public Material[] OriginalSharedMaterials;
        }
    }
}
