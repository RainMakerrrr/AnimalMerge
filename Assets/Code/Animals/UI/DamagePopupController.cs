using Code.Animals.Health;
using UnityEngine;

namespace Code.Animals.UI
{
    public class DamagePopupController : MonoBehaviour
    {
        [SerializeField] private AnimalHealth _health;
        [SerializeField] private DamagePopupView _popupPrefab;
        [SerializeField] private float _spawnOffsetY = 1.5f;
        [SerializeField] private float _randomXRange = 0.3f;

        private static readonly Color DamageColor = new Color(1f, 0.15f, 0.15f);
        private static readonly Color MissColor = new Color(1f, 0.9f, 0.3f);

        private Canvas _canvas;
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.isRootCanvas)
                {
                    _canvas = c;
                    break;
                }
            }
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.TakenDamage += OnDamageTaken;
            _health.DamageBlocked += OnDamageBlocked;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.TakenDamage -= OnDamageTaken;
            _health.DamageBlocked -= OnDamageBlocked;
        }

        private void OnDamageTaken(float damage) =>
            SpawnPopup($"-{Mathf.RoundToInt(damage)}", DamageColor);

        private void OnDamageBlocked() =>
            SpawnPopup("Miss!", MissColor);

        private void SpawnPopup(string text, Color color)
        {
            if (_canvas == null || _camera == null) return;

            var worldPos = transform.position + new Vector3(
                Random.Range(-_randomXRange, _randomXRange), _spawnOffsetY, 0f);

            var screenPos = _camera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return;

            var popup = Instantiate(_popupPrefab, _canvas.transform);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                screenPos,
                null,
                out var localPos);

            popup.GetComponent<RectTransform>().anchoredPosition = localPos;
            popup.Play(text, color);
        }
    }
}
