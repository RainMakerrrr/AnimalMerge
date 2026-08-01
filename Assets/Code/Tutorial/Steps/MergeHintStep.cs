using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Tutorial.Config;
using Code.Tutorial.UI;
using Cysharp.Threading.Tasks;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Zenject;

namespace Code.Tutorial.Steps
{
    public class MergeHintStep : ITutorialStep
    {
        private readonly MergeHintStepConfig _config;
        private readonly ITutorialHandView _handView;
        private readonly IPersistentProgressService _persistentProgress;
        private readonly IUnitTracker _unitTracker;
        private readonly SignalBus _signalBus;
        private readonly IMergeHintTargetResolver _targetResolver;

        public MergeHintStep(
            MergeHintStepConfig config,
            ITutorialHandView handView,
            IPersistentProgressService persistentProgress,
            IUnitTracker unitTracker,
            SignalBus signalBus,
            List<IMergeHintTargetResolver> targetResolvers)
        {
            _config = config;
            _handView = handView;
            _persistentProgress = persistentProgress;
            _unitTracker = unitTracker;
            _signalBus = signalBus;
            _targetResolver = targetResolvers.FirstOrDefault(resolver => resolver.Mode == config.TargetMode);
        }

        public string Id => _config.StepId;

        public bool CanRun =>
            _targetResolver != null &&
            _persistentProgress.Progress != null &&
            _persistentProgress.Progress.Level == _config.LevelNumber;

        public async UniTask<bool> RunAsync(CancellationToken cancellationToken)
        {
            if (_targetResolver == null)
                return false;

            var phaseEnded = false;

            using (var phaseCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                Action<PreBattlePhaseEndedSignal> onPhaseEnded = _ =>
                {
                    phaseEnded = true;
                    phaseCancellation.Cancel();
                };

                using (new CancelAwareSignalSubscription<PreBattlePhaseEndedSignal>(
                           _signalBus, onPhaseEnded, cancellationToken))
                {
                    try
                    {
                        var merged = await ShowHintAsync(phaseCancellation.Token);

                        if (merged)
                            return true;

                        return phaseEnded && _config.MarkCompletedWhenPhaseEnds;
                    }
                    finally
                    {
                        _handView.Hide();
                    }
                }
            }
        }

        private async UniTask<bool> ShowHintAsync(CancellationToken cancellationToken)
        {
            var pair = await WaitForPairAsync(cancellationToken);

            if (!pair.IsValid)
                return false;

            _handView.Show(pair.Source, pair.Target);

            return await WaitForMergeAsync(cancellationToken);
        }

        private async UniTask<MergeHintPair> WaitForPairAsync(CancellationToken cancellationToken)
        {
            if (_targetResolver.TryResolve(out var readyPair))
                return readyPair;

            var pairSource = new UniTaskCompletionSource<MergeHintPair>();

            Action onPlayerUnitsChanged = () =>
            {
                if (_targetResolver.TryResolve(out var spawnedPair))
                    pairSource.TrySetResult(spawnedPair);
            };

            _unitTracker.PlayerUnitsChanged += onPlayerUnitsChanged;

            try
            {
                var (isCanceled, resolvedPair) = await pairSource.Task
                    .AttachExternalCancellation(cancellationToken)
                    .SuppressCancellationThrow();

                return isCanceled ? default : resolvedPair;
            }
            finally
            {
                _unitTracker.PlayerUnitsChanged -= onPlayerUnitsChanged;
            }
        }

        private async UniTask<bool> WaitForMergeAsync(CancellationToken cancellationToken)
        {
            var mergedSource = new UniTaskCompletionSource();

            Action<AllyMergedSignal> onAllyMerged = _ => mergedSource.TrySetResult();

            using (new CancelAwareSignalSubscription<AllyMergedSignal>(_signalBus, onAllyMerged, cancellationToken))
            {
                var isCanceled = await mergedSource.Task
                    .AttachExternalCancellation(cancellationToken)
                    .SuppressCancellationThrow();

                return !isCanceled;
            }
        }
    }
}
