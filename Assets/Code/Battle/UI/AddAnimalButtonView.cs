using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Battle.UI
{
    [RequireComponent(typeof(Button))]
    public class AddAnimalButtonView : MonoBehaviour
    {
        [SerializeField] private Graphic _targetGraphic;
        [SerializeField] private TextMeshProUGUI _remainingLabel;
        [SerializeField] private TextMeshProUGUI _totalLabel;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private Button _button;

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

        public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);

        public void SetInteractable(bool isInteractable)
        {
            Button.interactable = isInteractable;

            if (_targetGraphic != null)
                _targetGraphic.color = isInteractable ? _normalColor : _disabledColor;
        }

        public void SetRemaining(int remaining, int total)
        {
            if (_remainingLabel != null)
                _remainingLabel.text = remaining.ToString("D2");

            if (_totalLabel != null)
                _totalLabel.text = $"/ {total:D2}";
        }

        private void OnButtonClick() => Clicked?.Invoke();
    }
}
