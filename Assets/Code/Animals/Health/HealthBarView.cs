using Code.Animals.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Animals.Health
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private AnimalHealth _health;
        [SerializeField] private Image _fillImage;
        [SerializeField] private float _widthPerGridCell = 100f;
        [SerializeField] private float _barHeight = 20f;

        private void Awake()
        {
            var layer = LayerMask.NameToLayer("HealthBar");
            SetLayerRecursively(gameObject, layer);
            ApplyFootprintSize();
        }

        public void ApplyFootprintSize()
        {
            if (!(transform is RectTransform rect)) return;

            var movement = GetComponentInParent<AnimalMovement>();
            if (movement == null) return;

            var ownerScale = movement.transform.localScale;
            if (Mathf.Approximately(ownerScale.x, 0f) || Mathf.Approximately(ownerScale.y, 0f)) return;

            var cells = Mathf.Max(1, movement.UnitSize.Width);

            rect.sizeDelta = new Vector2(
                _widthPerGridCell * cells / ownerScale.x,
                _barHeight / ownerScale.y);
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.HealthChanged += Refresh;
            _health.Died += OnDied;
            Refresh();
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.HealthChanged -= Refresh;
            _health.Died -= OnDied;
        }

        private void Refresh()
        {
            if (_health.Max <= 0f) return;
            _fillImage.fillAmount = _health.Current / _health.Max;
        }

        private void OnDied() => gameObject.SetActive(false);

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
