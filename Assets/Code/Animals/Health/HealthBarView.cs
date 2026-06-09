using UnityEngine;
using UnityEngine.UI;

namespace Code.Animals.Health
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private AnimalHealth _health;
        [SerializeField] private Image _fillImage;

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
    }
}
