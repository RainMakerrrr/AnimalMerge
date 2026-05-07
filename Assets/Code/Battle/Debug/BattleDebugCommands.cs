#if UNITY_EDITOR
using Code.Battle.Services;
using UnityEngine;

namespace Code.Battle
{
    /// <summary>
    /// Debug commands for testing battle system
    /// Core logic without MonoBehaviour - can be called from UI buttons or keyboard shortcuts
    /// Only available in Unity Editor builds
    /// </summary>
    public class BattleDebugCommands
    {
        private readonly IUnitTracker _unitTracker;

        public BattleDebugCommands(IUnitTracker unitTracker)
        {
            _unitTracker = unitTracker;
        }

        /// <summary>
        /// Heals all alive player units to full health
        /// </summary>
        public void HealAllPlayerUnits()
        {
            var players = _unitTracker.GetAlivePlayerUnits();
            var healedCount = 0;

            foreach (var unit in players)
            {
                if (!unit.Health.IsDead)
                {
                    var previousHealth = unit.Health.Current;
                    unit.Health.Restore(unit.Health.Max);
                    Debug.Log($"[BattleDebug] Healed {unit.name}: {previousHealth:F0} → {unit.Health.Current:F0}");
                    healedCount++;
                }
            }

            Debug.Log($"[BattleDebug] Total healed: {healedCount} units");
        }

        /// <summary>
        /// Restarts the current battle from the beginning
        /// </summary>
        public void RestartBattle()
        {
            Debug.Log("[BattleDebug] Restarting battle...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
#endif
