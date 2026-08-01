using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public class RendererMaterialOverlay : IDisposable
    {
        private readonly Material _glowTemplate;

        private RendererState[] _savedState;
        private MergeSweepGlow _glow;
        private Material _appliedGlowInstance;

        public MergeSweepGlow Glow => _glow;

        public RendererMaterialOverlay(Material glowTemplate)
        {
            _glowTemplate = glowTemplate;
        }

        public static List<Renderer> CollectPaintableRenderers(Transform root)
        {
            var paintable = new List<Renderer>();

            if (root == null)
                return paintable;

            foreach (var candidate in root.GetComponentsInChildren<Renderer>())
            {
                var isMeshBased = candidate is MeshRenderer || candidate is SkinnedMeshRenderer;

                if (isMeshBased && candidate.sharedMaterials.Length > 0)
                    paintable.Add(candidate);
            }

            return paintable;
        }

        public void Capture(IReadOnlyList<Renderer> renderers)
        {
            _savedState = new RendererState[renderers.Count];

            for (var i = 0; i < renderers.Count; i++)
            {
                _savedState[i] = new RendererState
                {
                    Renderer = renderers[i],
                    OriginalSharedMaterials = CaptureOriginalMaterials(renderers[i])
                };
            }
        }

        public void BeginGlow(
            Transform target,
            IReadOnlyList<Renderer> renderers,
            Vector3 localAxis,
            float bandsPerBody,
            float bandMargin,
            AnimationCurve ease)
        {
            _glow = new MergeSweepGlow(_glowTemplate, bandsPerBody, bandMargin, ease);
            _glow.Bind(target, renderers, localAxis);
        }

        public void Apply(Material skinOverride, bool includeGlow)
        {
            if (_savedState == null)
                return;

            for (var i = 0; i < _savedState.Length; i++)
            {
                var state = _savedState[i];

                if (state.Renderer == null)
                    continue;

                state.AppliedSharedMaterials = Compose(state, skinOverride, includeGlow);
                state.Renderer.sharedMaterials = state.AppliedSharedMaterials;
                _savedState[i] = state;
            }

            _appliedGlowInstance = includeGlow && _glow != null ? _glow.Instance : null;
        }

        public void RestoreOriginalMaterials()
        {
            if (_savedState == null)
                return;

            foreach (var state in _savedState)
            {
                if (state.Renderer == null)
                    continue;

                state.Renderer.sharedMaterials = StillOwnsRenderer(state)
                    ? state.OriginalSharedMaterials
                    : WithoutAppliedGlow(state.Renderer.sharedMaterials);
            }

            _savedState = null;
            _appliedGlowInstance = null;
        }

        public void ForgetCapturedMaterials()
        {
            _savedState = null;
            _appliedGlowInstance = null;
        }

        public void DisposeGlow()
        {
            if (_glow == null)
                return;

            _glow.Dispose();
            _glow = null;
        }

        public void Dispose()
        {
            RestoreOriginalMaterials();
            DisposeGlow();
        }

        private bool StillOwnsRenderer(RendererState state)
        {
            var applied = state.AppliedSharedMaterials;

            if (applied == null)
                return true;

            var current = state.Renderer.sharedMaterials;

            if (current.Length != applied.Length)
                return false;

            for (var i = 0; i < current.Length; i++)
            {
                if (!ReferenceEquals(current[i], applied[i]))
                    return false;
            }

            return true;
        }

        private Material[] WithoutAppliedGlow(Material[] current)
        {
            if (ReferenceEquals(_appliedGlowInstance, null))
                return current;

            var kept = new List<Material>(current.Length);

            foreach (var material in current)
            {
                if (ReferenceEquals(material, _appliedGlowInstance))
                    continue;

                kept.Add(material);
            }

            return kept.Count == current.Length ? current : kept.ToArray();
        }

        private Material[] Compose(RendererState state, Material skinOverride, bool includeGlow)
        {
            var original = state.OriginalSharedMaterials;
            var glowInstance = includeGlow && _glow != null ? _glow.Instance : null;
            var composed = new Material[glowInstance != null ? original.Length + 1 : original.Length];

            for (var i = 0; i < original.Length; i++)
                composed[i] = original[i];

            if (skinOverride != null && composed.Length > 0)
                composed[0] = skinOverride;

            if (glowInstance != null)
                composed[original.Length] = glowInstance;

            return composed;
        }

        private Material[] CaptureOriginalMaterials(Renderer renderer)
        {
            var current = renderer.sharedMaterials;
            var overlayShader = _glowTemplate != null ? _glowTemplate.shader : null;

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

        private struct RendererState
        {
            public Renderer Renderer;
            public Material[] OriginalSharedMaterials;
            public Material[] AppliedSharedMaterials;
        }
    }
}
