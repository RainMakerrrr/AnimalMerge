using System;
using System.Threading;
using Code.Animals.Vfx.Config;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public class MergeAppearAnimation : IDisposable
    {
        private readonly Transform _target;
        private readonly Vector3 _authoredScale;
        private readonly MergeAnimationConfig _config;

        private RendererMaterialOverlay _overlay;

        private MergeAppearAnimation(Transform target, Vector3 authoredScale, MergeAnimationConfig config)
        {
            _target = target;
            _authoredScale = authoredScale;
            _config = config;
        }

        public static MergeAppearAnimation Begin(Transform target, Vector3 authoredScale, MergeAnimationConfig config)
        {
            var animation = new MergeAppearAnimation(target, authoredScale, config);
            animation.CollapseScaleAndAttachGlow();

            return animation;
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_config.AppearDelay > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_config.AppearDelay), cancellationToken: cancellationToken);

                var scalePass = MergeScaleTween.RunAsync(
                    _target,
                    _authoredScale * _config.AppearStartScale,
                    _authoredScale,
                    _config.AppearDuration,
                    _config.AppearCurve,
                    cancellationToken);

                var glow = _overlay?.Glow;

                if (glow == null)
                    await scalePass;
                else
                    await UniTask.WhenAll(scalePass, glow.PlayPassAsync(false, _config.GlowDuration, cancellationToken));
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_target != null)
                _target.localScale = _authoredScale;

            if (_overlay == null)
                return;

            _overlay.Dispose();
            _overlay = null;
        }

        private void CollapseScaleAndAttachGlow()
        {
            _target.localScale = _authoredScale * _config.AppearStartScale;

            if (_config.GlowMaterial == null)
                return;

            var renderers = RendererMaterialOverlay.CollectPaintableRenderers(_target);

            if (renderers.Count == 0)
                return;

            _overlay = new RendererMaterialOverlay(_config.GlowMaterial);
            _overlay.Capture(renderers);
            _overlay.BeginGlow(
                _target,
                renderers,
                _config.GlowAxis,
                _config.GlowBandsPerBody,
                _config.GlowBandMargin,
                _config.GlowEase);
            _overlay.Apply(null, true);
        }
    }
}
