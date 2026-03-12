using Code.Battle;
using UnityEngine;

namespace Code.Battle.Services
{
    public class VictoryConditionChecker : IVictoryConditionChecker
    {
        private readonly IUnitTracker _unitTracker;

        public VictoryConditionChecker(IUnitTracker unitTracker)
        {
            _unitTracker = unitTracker;
        }

        public BattleResult CheckBattleConditions()
        {
            var allPlayersDead = _unitTracker.AlivePlayerUnitsCount == 0;
            var allEnemiesDead = _unitTracker.AliveEnemyUnitsCount == 0;
            var bossAlive = _unitTracker.HasAliveBoss;
            var wasBossRegistered = _unitTracker.WasBossRegistered;

            Debug.Log($"[VictoryConditionChecker] Players: {_unitTracker.AlivePlayerUnitsCount}, " +
                     $"Enemies: {_unitTracker.AliveEnemyUnitsCount}, Boss Alive: {bossAlive}, " +
                     $"Was Boss Registered: {wasBossRegistered}");

            // Defeat: all player units dead (check first - defeat has priority)
            if (allPlayersDead)
            {
                Debug.Log("[VictoryConditionChecker] DEFEAT! All player units dead");
                return BattleResult.Defeat;
            }

            // Victory conditions:
            // 1. If boss was registered: boss must be dead (regular enemies don't matter)
            // 2. If no boss: all enemies must be dead
            bool victoryConditionMet;
            if (wasBossRegistered)
            {
                victoryConditionMet = !bossAlive;
                if (victoryConditionMet)
                {
                    Debug.Log("[VictoryConditionChecker] VICTORY! Boss defeated");
                }
            }
            else
            {
                victoryConditionMet = allEnemiesDead;
                if (victoryConditionMet)
                {
                    Debug.Log("[VictoryConditionChecker] VICTORY! All enemies defeated");
                }
            }

            if (victoryConditionMet)
            {
                return BattleResult.Victory;
            }

            // Battle continues
            return BattleResult.Ongoing;
        }
    }
}
