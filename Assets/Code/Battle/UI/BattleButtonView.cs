using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Battle.UI
{
    [RequireComponent(typeof(Button))]
    public class BattleButtonView : MonoBehaviour
    {
        [SerializeField] private Graphic _targetGraphic;
        [SerializeField] private Graphic[] _stateGraphics;
        [SerializeField] private Material _notReadyMaterial;
        [SerializeField] private UiPulseAnimator _pulse;
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _notReadyColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private bool _hideUntilReady;

        private readonly Dictionary<Graphic, Material> _readyMaterials = new Dictionary<Graphic, Material>();

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

        private void Awake()
        {
            Button.onClick.AddListener(OnButtonClick);

            CacheReadyMaterial(_targetGraphic);

            if (_stateGraphics == null)
                return;

            foreach (var graphic in _stateGraphics)
                CacheReadyMaterial(graphic);
        }

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

            ApplyGraphicState(_targetGraphic);

            if (_stateGraphics != null)
            {
                foreach (var graphic in _stateGraphics)
                    ApplyGraphicState(graphic);
            }

            if (_pulse == null)
                return;

            if (_isReady && shouldShow)
                _pulse.Play();
            else
                _pulse.Stop();
        }

        private void ApplyGraphicState(Graphic graphic)
        {
            if (graphic == null)
                return;

            CacheReadyMaterial(graphic);

            bool acceptsMaterial = AcceptsMaterial(graphic);
            bool usesNotReadyMaterial = _isReady == false && acceptsMaterial && _notReadyMaterial != null;

            graphic.color = _isReady || usesNotReadyMaterial ? _readyColor : _notReadyColor;

            if (acceptsMaterial == false)
                return;

            graphic.material = usesNotReadyMaterial ? _notReadyMaterial : _readyMaterials[graphic];
        }

        private void CacheReadyMaterial(Graphic graphic)
        {
            if (graphic == null || AcceptsMaterial(graphic) == false || _readyMaterials.ContainsKey(graphic))
                return;

            _readyMaterials[graphic] = graphic.material;
        }

        private bool AcceptsMaterial(Graphic graphic) => (graphic is TMP_Text) == false;

        private void OnButtonClick() => Clicked?.Invoke();
    }
}
