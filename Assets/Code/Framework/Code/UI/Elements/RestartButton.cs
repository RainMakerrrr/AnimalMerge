using Framework.Code.Infrastructure.Services.GameRestart;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Framework.Code.UI.Elements
{
    public class RestartButton : MonoBehaviour
    {
        private Button _button;
        private IGameRestartService _gameRestartService;

        [Inject]
        private void Construct(IGameRestartService gameRestartService)
        {
            _gameRestartService = gameRestartService;
        }

        private void Start()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(RestartLevel);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(RestartLevel);
        }

        private void RestartLevel() => _gameRestartService.RetryCurrentLevel();
    }
}
