using System;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.Code.UI.Windows
{
    public class DefeatWindowView : MonoBehaviour
    {
        [SerializeField] private Button _tryAgainButton;

        public event Action TryAgainClicked;

        private void Awake()
        {
            if (_tryAgainButton == null)
            {
                Debug.LogError($"[DefeatWindowView] {nameof(_tryAgainButton)} is not assigned", this);
                return;
            }

            _tryAgainButton.onClick.AddListener(OnTryAgainClicked);
        }

        private void OnDestroy()
        {
            if (_tryAgainButton != null)
                _tryAgainButton.onClick.RemoveListener(OnTryAgainClicked);
        }

        private void OnTryAgainClicked() => TryAgainClicked?.Invoke();
    }
}
