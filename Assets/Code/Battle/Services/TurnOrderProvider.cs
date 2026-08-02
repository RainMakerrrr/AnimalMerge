using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public class TurnOrderProvider : ITurnOrderProvider, ITurnOrderRecorder
    {
        private readonly IUnitTracker _unitTracker;
        private readonly List<AnimalFacade> _playerTurns = new List<AnimalFacade>();
        private readonly List<AnimalFacade> _enemyTurns = new List<AnimalFacade>();

        private bool _hasRecordedRound;

        public TurnOrderProvider(IUnitTracker unitTracker)
        {
            _unitTracker = unitTracker;
        }

        public IReadOnlyList<TurnOrderUnit> GetRoundOrder()
        {
            var order = new List<TurnOrderUnit>();

            if (_hasRecordedRound == false)
            {
                Append(TurnOrderCalculator.OrderPlayerUnits(_unitTracker.GetAlivePlayerUnits()), false, order);
                Append(TurnOrderCalculator.OrderEnemyUnits(_unitTracker.GetAliveEnemyUnits()), true, order);

                return order;
            }

            AppendAlive(_playerTurns, false, order);
            AppendAlive(_enemyTurns, true, order);

            return order;
        }

        public void RecordPlayerTurns(IReadOnlyList<AnimalFacade> order)
        {
            _playerTurns.Clear();
            _playerTurns.AddRange(order);

            _enemyTurns.Clear();
            _enemyTurns.AddRange(TurnOrderCalculator.OrderEnemyUnits(_unitTracker.GetAliveEnemyUnits()));

            _hasRecordedRound = true;
        }

        public void RecordEnemyTurns(IReadOnlyList<AnimalFacade> order)
        {
            _enemyTurns.Clear();
            _enemyTurns.AddRange(order);

            _hasRecordedRound = true;
        }

        public void Clear()
        {
            _playerTurns.Clear();
            _enemyTurns.Clear();

            _hasRecordedRound = false;
        }

        private static void Append(List<AnimalFacade> units, bool isEnemy, List<TurnOrderUnit> destination)
        {
            for (int i = 0; i < units.Count; i++)
                destination.Add(new TurnOrderUnit(units[i], isEnemy));
        }

        private static void AppendAlive(List<AnimalFacade> turns, bool isEnemy, List<TurnOrderUnit> destination)
        {
            for (int i = 0; i < turns.Count; i++)
            {
                var unit = turns[i];

                if (unit == null || unit.gameObject == null)
                    continue;

                if (unit.Health != null && unit.Health.IsDead)
                    continue;

                destination.Add(new TurnOrderUnit(unit, isEnemy));
            }
        }
    }
}
