using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.Battle.Services;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Animals.Health
{
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private AnimalHealth _health;
        [SerializeField] private Image _fillImage;
        [SerializeField] private RectTransform _enemyIcon;
        [SerializeField] private float _widthPerGridCell = 100f;
        [SerializeField] private float _barHeight = 20f;
        [SerializeField] private float _enemyIconSize = 36f;

        private IUnitTracker _unitTracker;
        private AnimalFacade _owner;

        [Inject]
        private void Construct(IUnitTracker unitTracker)
        {
            _unitTracker = unitTracker;
            _owner = GetComponentInParent<AnimalFacade>(true);

            _unitTracker.EnemyUnitRegistered += OnEnemyUnitRegistered;

            SetEnemyIconVisible(IsTrackedEnemy());
        }

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

            if (_enemyIcon != null)
                _enemyIcon.sizeDelta = new Vector2(
                    _enemyIconSize / ownerScale.x,
                    _enemyIconSize / ownerScale.y);
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

        private void OnDestroy()
        {
            if (_unitTracker == null) return;
            _unitTracker.EnemyUnitRegistered -= OnEnemyUnitRegistered;
        }

        private void Refresh()
        {
            if (_health.Max <= 0f) return;
            _fillImage.fillAmount = _health.Current / _health.Max;
        }

        private void OnDied() => gameObject.SetActive(false);

        private void OnEnemyUnitRegistered(AnimalFacade unit)
        {
            if (_owner == null || unit != _owner) return;
            SetEnemyIconVisible(true);
        }

        private bool IsTrackedEnemy()
        {
            if (_owner == null) return false;

            var enemies = _unitTracker.GetAliveEnemyUnits();

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == _owner)
                    return true;
            }

            return false;
        }

        private void SetEnemyIconVisible(bool isVisible)
        {
            if (_enemyIcon == null) return;
            _enemyIcon.gameObject.SetActive(isVisible);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
