using System;
using System.Threading;
using Code.Battle.Config;
using Code.Battle.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Battle.UI.StageBanner
{
    public class StageBannerAnnouncer : IStageAnnouncer, IDisposable
    {
        private readonly IStageBannerView _view;
        private readonly StageBannerConfig _config;
        private readonly CancellationTokenSource _lifetimeCts = new CancellationTokenSource();

        private CancellationTokenSource _playCts;
        private bool _isDisposed;

        public StageBannerAnnouncer(IStageBannerView view, StageBannerConfig config)
        {
            _view = view;
            _config = config;
        }

        public async UniTask<bool> AnnounceAsync(StageBannerKind kind, CancellationToken cancellationToken)
        {
            if (_isDisposed || cancellationToken.IsCancellationRequested)
                return false;

            _playCts?.Cancel();

            if (!_config.TryGetStyle(kind, out var style))
            {
                Debug.LogWarning($"[{nameof(StageBannerAnnouncer)}] No stage banner style configured for {kind}.");
                return true;
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);
            _playCts = linkedCts;

            try
            {
                await _view.PlayAsync(style, linkedCts.Token);
                return !linkedCts.IsCancellationRequested;
            }
            finally
            {
                if (_playCts == linkedCts)
                    _playCts = null;

                linkedCts.Dispose();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _playCts?.Cancel();
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
        }
    }
}
