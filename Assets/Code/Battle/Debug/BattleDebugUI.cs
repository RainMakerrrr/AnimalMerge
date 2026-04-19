#if UNITY_EDITOR
using UnityEngine;
using Zenject;

namespace Code.Battle
{
    /// <summary>
    /// UI component for battle debug commands
    /// Add this to a GameObject in the scene (e.g., Canvas)
    /// Connect UI buttons to public methods via Inspector
    /// Only available in Unity Editor builds
    /// </summary>
    public class BattleDebugUI : MonoBehaviour
    {
        private BattleDebugCommands _debugCommands;

        [Inject]
        private void Construct(BattleDebugCommands debugCommands)
        {
            _debugCommands = debugCommands;
        }

        /// <summary>
        /// Call this method from UI Button's OnClick event
        /// Heals all alive player units to full health
        /// </summary>
        public void OnHealButtonClicked()
        {
            if (_debugCommands == null)
            {
                Debug.LogError("[BattleDebugUI] BattleDebugCommands not injected! Make sure this GameObject has Zenject injection.");
                return;
            }

            Debug.Log("[BattleDebugUI] Heal button clicked");
            _debugCommands.HealAllPlayerUnits();
        }

        /// <summary>
        /// Call this method from UI Button's OnClick event
        /// Restarts the current battle
        /// </summary>
        public void OnRestartButtonClicked()
        {
            if (_debugCommands == null)
            {
                Debug.LogError("[BattleDebugUI] BattleDebugCommands not injected! Make sure this GameObject has Zenject injection.");
                return;
            }

            Debug.Log("[BattleDebugUI] Restart button clicked");
            _debugCommands.RestartBattle();
        }
    }
}
#endif
