using Code.Animals.Movement;
using Code.GridPathfinding;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Code.Animals.Health
{
    public class HealthBarView : MonoBehaviour
    {
        private const string HealthBarLayerName = "HealthBar";

        [SerializeField] private AnimalHealth _health;
        [SerializeField] private HealthBarLayout _layout;
        [SerializeField] private SegmentedHealthBar _segments;
        [SerializeField] private HealthBarBlinker _blinker;

        private HealthBarConfig _config;
        private Transform _owner;
        private UnitSize _footprint = UnitSize.Small;
        private Vector3 _appliedOwnerScale = Vector3.one;
        private Tween _fillTween;
        private float _displayed;
        private bool _isLowHealth;
        private bool _hasShownHealth;

        [Inject]
        private void Construct(HealthBarConfig config) => _config = config;

        private void Awake()
        {
            SetLayerRecursively(gameObject, LayerMask.NameToLayer(HealthBarLayerName));
            ResolveOwner();
            BuildBar();
        }

        private void OnEnable()
        {
            if (_health == null) return;

            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;

            KillFillTween();
            _hasShownHealth = HasHealthData();
            ShowImmediately(_hasShownHealth ? TargetFill() : 1f);
        }

        private void OnDisable()
        {
            KillFillTween();
            ResetLowHealth();

            if (_health == null) return;

            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        private void LateUpdate()
        {
            if (_owner == null || _layout == null) return;

            var ownerScale = _owner.localScale;

            if (ownerScale == _appliedOwnerScale || IsDegenerate(ownerScale)) return;

            ApplyLayout(ownerScale);
        }

        private void OnDestroy() => KillFillTween();

        private void ResolveOwner()
        {
            var movement = GetComponentInParent<AnimalMovement>();

            if (movement == null) return;

            _owner = movement.transform;
            _footprint = movement.UnitSize;
        }

        private void BuildBar()
        {
            if (_layout == null || _segments == null) return;

            var ownerScale = _owner != null ? _owner.localScale : Vector3.one;

            if (IsDegenerate(ownerScale))
                ownerScale = Vector3.one;

            var segmentCount = ApplyLayout(ownerScale);
            _segments.Build(segmentCount);
        }

        private int ApplyLayout(Vector3 ownerScale)
        {
            _appliedOwnerScale = ownerScale;
            return _layout.Apply(ownerScale, _footprint);
        }

        private void OnHealthChanged()
        {
            if (!HasHealthData()) return;

            if (_hasShownHealth)
            {
                AnimateTo(TargetFill(), null);
                return;
            }

            _hasShownHealth = true;
            KillFillTween();
            ShowImmediately(TargetFill());
        }

        private void OnDied() => AnimateTo(0f, Hide);

        private void AnimateTo(float target, TweenCallback onComplete)
        {
            KillFillTween();

            if (_config == null || _config.FillDuration <= 0f)
            {
                ShowImmediately(target);
                onComplete?.Invoke();
                return;
            }

            _fillTween = DOTween
                .To(() => _displayed, ApplyDisplayed, target, _config.FillDuration)
                .SetEase(_config.FillEase)
                .SetLink(gameObject);

            if (onComplete != null)
                _fillTween.OnComplete(onComplete);
        }

        private void ShowImmediately(float value) => ApplyDisplayed(value);

        private void ApplyDisplayed(float value)
        {
            _displayed = value;

            if (_segments != null)
                _segments.SetFill(value);

            UpdateLowHealthState();
        }

        private void UpdateLowHealthState()
        {
            var isLowHealth = _config != null
                && _displayed > 0f
                && _displayed <= _config.LowHealthThreshold;

            if (isLowHealth == _isLowHealth) return;

            _isLowHealth = isLowHealth;

            if (_segments != null)
                _segments.SetLowHealth(isLowHealth);

            if (_blinker == null) return;

            if (isLowHealth)
                _blinker.Play(_config.BlinkHalfPeriod, _config.BlinkMinAlpha);
            else
                _blinker.Stop();
        }

        private void ResetLowHealth()
        {
            _isLowHealth = false;

            if (_segments != null)
                _segments.SetLowHealth(false);

            if (_blinker != null)
                _blinker.Stop();
        }

        private bool HasHealthData() => _health.Max > 0f;

        private static bool IsDegenerate(Vector3 scale) =>
            Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f);

        private float TargetFill() =>
            HasHealthData() ? Mathf.Clamp01(_health.Current / _health.Max) : 0f;

        private void Hide() => gameObject.SetActive(false);

        private void KillFillTween()
        {
            if (_fillTween == null) return;

            _fillTween.Kill();
            _fillTween = null;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
