#if UNITY_EDITOR
using UnityEngine;
using Zenject;

namespace Code.Battle.Input
{
    /// <summary>
    /// Keyboard shortcuts for battle debug commands
    /// Ctrl+H: Heal all player units
    /// Ctrl+R: Restart battle
    /// Only available in Unity Editor builds
    /// </summary>
    public class KeyboardBattleDebugHelper : MonoBehaviour
    {
        private BattleDebugCommands _debugCommands;

        [Inject]
        private void Construct(BattleDebugCommands debugCommands)
        {
            _debugCommands = debugCommands;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                UnityEngine.Input.GetKey(KeyCode.RightControl))
            {
                // Ctrl+H: Heal all player units
                if (UnityEngine.Input.GetKeyDown(KeyCode.H))
                {
                    Debug.Log("[KeyboardBattleDebugHelper] Ctrl+H pressed - healing all player units");
                    _debugCommands.HealAllPlayerUnits();
                }

                // Ctrl+R: Restart battle
                if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                {
                    Debug.Log("[KeyboardBattleDebugHelper] Ctrl+R pressed - restarting battle");
                    _debugCommands.RestartBattle();
                }
            }
        }
    }
}
#endif
