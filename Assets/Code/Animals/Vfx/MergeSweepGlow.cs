using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public class MergeSweepGlow
    {
        private static readonly int SweepSpeed = Shader.PropertyToID("_SweepSpeed");
        private static readonly int SweepTiling = Shader.PropertyToID("_SweepTiling");
        private static readonly int SweepAxisExtent = Shader.PropertyToID("_SweepAxisExtent");
        private static readonly int SweepWorldAxis = Shader.PropertyToID("_SweepWorldAxis");
        private static readonly int SweepWorldOrigin = Shader.PropertyToID("_SweepWorldOrigin");
        private static readonly int PhaseOffset = Shader.PropertyToID("_PhaseOffset");

        private readonly float _bandsPerBody;
        private readonly float _bandMargin;
        private readonly AnimationCurve _ease;

        private Transform _target;
        private IReadOnlyList<Renderer> _renderers;
        private Vector3 _localAxis = Vector3.up;

        public Material Instance { get; private set; }

        public MergeSweepGlow(Material template, float bandsPerBody, float bandMargin, AnimationCurve ease)
        {
            _bandsPerBody = Mathf.Max(bandsPerBody, 0.0001f);
            _bandMargin = Mathf.Max(bandMargin, 0f);
            _ease = ease;

            if (template == null)
                return;

            Instance = new Material(template);
            Instance.SetFloat(SweepSpeed, 0f);
            Instance.SetFloat(SweepTiling, _bandsPerBody);
            Instance.SetVector(SweepWorldAxis, new Vector4(0f, 1f, 0f, 1f));
        }

        public void Bind(Transform target, IReadOnlyList<Renderer> renderers, Vector3 localAxis)
        {
            if (Instance == null)
                return;

            _target = target;
            _renderers = renderers;
            _localAxis = localAxis.sqrMagnitude > 1e-6f ? localAxis.normalized : Vector3.up;

            WriteSweepFrame();
            Instance.SetFloat(PhaseOffset, PhaseAt(0f));
        }

        public async UniTask PlayPassAsync(bool reversed, float duration, CancellationToken cancellationToken)
        {
            if (Instance == null || _target == null)
                return;

            var from = PhaseAt(reversed ? 1f : 0f);
            var to = PhaseAt(reversed ? 0f : 1f);
            var elapsed = 0f;

            while (true)
            {
                var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

                WriteSweepFrame();
                Instance.SetFloat(PhaseOffset, Mathf.LerpUnclamped(from, to, Evaluate(progress)));

                if (progress >= 1f)
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (Instance == null || _target == null)
                    return;

                elapsed += Time.deltaTime;
            }
        }

        public void Dispose()
        {
            if (Instance != null)
                Object.Destroy(Instance);

            Instance = null;
            _target = null;
            _renderers = null;
        }

        private float PhaseAt(float bodyFraction)
        {
            var offBodyFraction = Mathf.LerpUnclamped(-_bandMargin, 1f + _bandMargin, bodyFraction);
            return 0.5f - _bandsPerBody * offBodyFraction;
        }

        private float Evaluate(float progress)
        {
            if (_ease == null || _ease.length == 0)
                return progress;

            return _ease.Evaluate(progress);
        }

        private void WriteSweepFrame()
        {
            if (Instance == null || _target == null || _renderers == null)
                return;

            if (!TryGetWorldBounds(out var bounds))
                return;

            var worldAxis = (_target.rotation * _localAxis).normalized;
            var axisMagnitudes = new Vector3(Mathf.Abs(worldAxis.x), Mathf.Abs(worldAxis.y), Mathf.Abs(worldAxis.z));
            var extent = Mathf.Max(2f * Vector3.Dot(axisMagnitudes, bounds.extents), 0.0001f);
            var origin = bounds.center - worldAxis * (extent * 0.5f);

            Instance.SetFloat(SweepAxisExtent, extent);
            Instance.SetVector(SweepWorldAxis, new Vector4(worldAxis.x, worldAxis.y, worldAxis.z, 1f));
            Instance.SetVector(SweepWorldOrigin, new Vector4(origin.x, origin.y, origin.z, 0f));
        }

        private bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            for (var i = 0; i < _renderers.Count; i++)
            {
                var meshRenderer = _renderers[i];

                if (meshRenderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = meshRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(meshRenderer.bounds);
            }

            return hasBounds;
        }
    }
}
