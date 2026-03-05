using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Battle.Input
{
    /// <summary>
    /// UI Button component for starting battle
    /// Attach to Button GameObject, connect OnClick event in Inspector
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StartBattleButton : MonoBehaviour
    {
        private StartBattleService _startBattleService;
        private Button _button;

        [Inject]
        private void Construct(StartBattleService startBattleService)
        {
            _startBattleService = startBattleService;
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClick);
        }

        public void OnButtonClick()
        {
            Debug.Log("[StartBattleButton] Button clicked - requesting battle start");
            _startBattleService.RequestStart();
        }
    }
}
