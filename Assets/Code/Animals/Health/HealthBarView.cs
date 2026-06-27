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

        private void Awake()
        {
            var layer = LayerMask.NameToLayer("HealthBar");
            SetLayerRecursively(gameObject, layer);
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
