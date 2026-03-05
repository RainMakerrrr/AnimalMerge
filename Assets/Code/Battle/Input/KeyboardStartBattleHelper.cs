using UnityEngine;
using Zenject;

namespace Code.Battle.Input
{
    public class KeyboardStartBattleHelper : MonoBehaviour
    {
        private StartBattleService _startBattleService;

        [Inject]
        private void Construct(StartBattleService startBattleService)
        {
            _startBattleService = startBattleService;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[KeyboardStartBattleHelper] P pressed - requesting battle start");
                _startBattleService.RequestStart();
            }
        }
    }
}
