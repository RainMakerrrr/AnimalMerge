using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Battle.UI
{
    [RequireComponent(typeof(Button))]
    public class BattleButtonView : MonoBehaviour
    {
        [SerializeField] private Graphic _targetGraphic;
        [SerializeField] private UiPulseAnimator _pulse;
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _notReadyColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private bool _hideUntilReady;

        private Button _button;
        private bool _isVisible;
        private bool _isReady;

        public event Action Clicked;

        private Button Button
        {
            get
            {
                if (_button == null)
                    _button = GetComponent<Button>();

                return _button;
            }
        }

        private void Awake() => Button.onClick.AddListener(OnButtonClick);

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClick);
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            ApplyState();
        }

        public void SetReady(bool isReady)
        {
            _isReady = isReady;
            ApplyState();
        }

        private void ApplyState()
        {
            bool shouldShow = _isVisible && (_isReady || !_hideUntilReady);

            gameObject.SetActive(shouldShow);
            Button.interactable = _isReady;

            if (_targetGraphic != null)
                _targetGraphic.color = _isReady ? _readyColor : _notReadyColor;

            if (_pulse == null)
                return;

            if (_isReady && shouldShow)
                _pulse.Play();
            else
                _pulse.Stop();
        }

        private void OnButtonClick() => Clicked?.Invoke();
    }
}
