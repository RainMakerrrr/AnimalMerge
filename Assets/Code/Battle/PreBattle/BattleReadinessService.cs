using System;
using System.Collections.Generic;
using System.Linq;
using Code.Battle.Services;
using Code.Battle.Signals;
using Zenject;

namespace Code.Battle.PreBattle
{
    public class BattleReadinessService : IBattleReadinessService, IDisposable
    {
        private readonly List<IBattleReadinessRule> _rules;
        private readonly IUnitTracker _unitTracker;
        private readonly SignalBus _signalBus;

        private bool _isActive;
        private bool _lastReported;

        public BattleReadinessService(
            List<IBattleReadinessRule> rules,
            IUnitTracker unitTracker,
            SignalBus signalBus)
        {
            _rules = rules;
            _unitTracker = unitTracker;
            _signalBus = signalBus;
        }

        public bool CanStartBattle => _isActive && _rules.All(rule => rule.IsSatisfied);

        public void Activate()
        {
            if (_isActive)
                return;

            _unitTracker.PlayerUnitsChanged += OnPlayerUnitsChanged;
            _isActive = true;
            _lastReported = false;

            Evaluate();
        }

        public void Deactivate()
        {
            if (!_isActive)
                return;

            _unitTracker.PlayerUnitsChanged -= OnPlayerUnitsChanged;
            _isActive = false;
            _lastReported = false;
        }

        public void Evaluate()
        {
            bool canStart = CanStartBattle;

            if (canStart == _lastReported)
                return;

            _lastReported = canStart;
            _signalBus.Fire(new BattleReadinessChangedSignal { CanStartBattle = canStart });
        }

        public void Dispose() => Deactivate();

        private void OnPlayerUnitsChanged() => Evaluate();
    }
}
