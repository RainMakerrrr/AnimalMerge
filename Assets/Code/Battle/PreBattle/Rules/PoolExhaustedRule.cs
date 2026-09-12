using System;
using Code.Battle.Signals;
using Zenject;

namespace Code.Battle.PreBattle.Rules
{
    public class PoolExhaustedRule : IBattleReadinessRule, IDisposable
    {
        private readonly IAllySpawnPool _pool;
        private readonly IAllySpawnService _spawnService;
        private readonly SignalBus _signalBus;

        private bool _phaseUsesStartingPool;

        public PoolExhaustedRule(IAllySpawnPool pool, IAllySpawnService spawnService, SignalBus signalBus)
        {
            _pool = pool;
            _spawnService = spawnService;
            _signalBus = signalBus;

            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPhaseStarted);
        }

        public bool IsSatisfied => !_phaseUsesStartingPool || _pool.Remaining == 0 || !_spawnService.CanSpawn;

        public void Dispose() => _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPhaseStarted);

        private void OnPhaseStarted(PreBattlePhaseStartedSignal signal) =>
            _phaseUsesStartingPool = signal.IsStartingPool;
    }
}
