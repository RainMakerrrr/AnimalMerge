using System;
using Code.Battle.Config;
using Code.Battle.Services;
using Code.Battle.Signals;
using UnityEngine;
using Zenject;

namespace Code.Battle.PreBattle.Rules
{
    public class MinAllyCountRule : IBattleReadinessRule, IDisposable
    {
        private readonly PreBattleConfig _config;
        private readonly IUnitTracker _unitTracker;
        private readonly SignalBus _signalBus;

        private bool _phaseUsesStartingPool;
        private int _alliesAvailableThisPhase;
        private int _highWaterMark;

        public MinAllyCountRule(
            PreBattleConfig config,
            IUnitTracker unitTracker,
            SignalBus signalBus)
        {
            _config = config;
            _unitTracker = unitTracker;
            _signalBus = signalBus;

            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPhaseStarted);
            _unitTracker.PlayerUnitsChanged += OnPlayerUnitsChanged;
        }

        public bool IsSatisfied
        {
            get
            {
                if (_phaseUsesStartingPool)
                    return true;

                int aliveNow = _unitTracker.AlivePlayerUnitsCount;

                return aliveNow >= 1 && Mathf.Max(_highWaterMark, aliveNow) >= EffectiveMin;
            }
        }

        private int EffectiveMin => Mathf.Min(_config.MinAlliesToStart, _alliesAvailableThisPhase);

        public void Dispose()
        {
            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPhaseStarted);
            _unitTracker.PlayerUnitsChanged -= OnPlayerUnitsChanged;
        }

        private void OnPhaseStarted(PreBattlePhaseStartedSignal signal)
        {
            int aliveAtStart = _unitTracker.AlivePlayerUnitsCount;

            _phaseUsesStartingPool = signal.IsStartingPool;
            _alliesAvailableThisPhase = aliveAtStart + signal.PoolRemaining;
            _highWaterMark = aliveAtStart;
        }

        private void OnPlayerUnitsChanged() =>
            _highWaterMark = Mathf.Max(_highWaterMark, _unitTracker.AlivePlayerUnitsCount);
    }
}
