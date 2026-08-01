using System;
using System.Collections.Generic;
using System.Threading;
using Code.Battle.Signals;
using Code.Tutorial.Progress;
using Code.Tutorial.Signals;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Code.Tutorial
{
    public class TutorialRunner : IInitializable, IDisposable
    {
        private readonly List<ITutorialStep> _steps;
        private readonly ITutorialProgressService _tutorialProgress;
        private readonly SignalBus _signalBus;

        private CancellationTokenSource _sequenceCancellation;
        private bool _isRunning;

        public TutorialRunner(
            List<ITutorialStep> steps,
            ITutorialProgressService tutorialProgress,
            SignalBus signalBus)
        {
            _steps = steps;
            _tutorialProgress = tutorialProgress;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            CancelSequence();
        }

        private void OnPreBattlePhaseStarted(PreBattlePhaseStartedSignal signal)
        {
            if (!signal.IsLevelStart || _isRunning)
                return;

            CancelSequence();

            _sequenceCancellation = new CancellationTokenSource();
            RunSequenceAsync(_sequenceCancellation.Token).Forget();
        }

        private async UniTaskVoid RunSequenceAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;

            try
            {
                foreach (var step in _steps)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (_tutorialProgress.IsCompleted(step.Id) || !step.CanRun)
                        continue;

                    var completed = await step.RunAsync(cancellationToken);

                    if (!completed)
                        continue;

                    _tutorialProgress.MarkCompleted(step.Id);
                    _signalBus.Fire(new TutorialStepCompletedSignal { StepId = step.Id });
                }
            }
            finally
            {
                _isRunning = false;
                DisposeSequenceCancellation();
            }
        }

        private void CancelSequence()
        {
            if (_sequenceCancellation == null)
                return;

            _sequenceCancellation.Cancel();
            DisposeSequenceCancellation();
        }

        private void DisposeSequenceCancellation()
        {
            if (_sequenceCancellation == null)
                return;

            _sequenceCancellation.Dispose();
            _sequenceCancellation = null;
        }
    }
}
