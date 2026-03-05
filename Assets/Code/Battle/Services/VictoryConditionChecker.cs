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

            Debug.Log($"[VictoryConditionChecker] Players: {_unitTracker.AlivePlayerUnitsCount}, " +
                     $"Enemies: {_unitTracker.AliveEnemyUnitsCount}, Boss Alive: {bossAlive}");

            // Victory: all enemies dead AND boss defeated
            if (allEnemiesDead && !bossAlive)
            {
                Debug.Log("[VictoryConditionChecker] VICTORY! All enemies defeated");
                return BattleResult.Victory;
            }

            // Defeat: all player units dead
            if (allPlayersDead)
            {
                Debug.Log("[VictoryConditionChecker] DEFEAT! All player units dead");
                return BattleResult.Defeat;
            }

            // Battle continues
            return BattleResult.Ongoing;
        }
    }
}
