using System;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.Code.UI.Windows
{
    public class CampaignVictoryWindowView : MonoBehaviour
    {
        [SerializeField] private Button _restartButton;

        public event Action RestartClicked;

        private void Awake()
        {
            if (_restartButton == null)
            {
                Debug.LogError($"[CampaignVictoryWindowView] {nameof(_restartButton)} is not assigned", this);
                return;
            }

            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void OnRestartClicked() => RestartClicked?.Invoke();
    }
}
