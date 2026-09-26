using DG.Tweening;
using UnityEngine;

namespace Code.Animals.Health
{
    [CreateAssetMenu(fileName = "HealthBarConfig", menuName = "Game/Health Bar Config")]
    public class HealthBarConfig : ScriptableObject
    {
        [Header("Fill Animation")]
        [SerializeField, Min(0f)] private float _fillDuration = 0.35f;
        [SerializeField] private Ease _fillEase = Ease.OutCubic;

        [Header("Low Health")]
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;
        [SerializeField, Min(0.01f)] private float _blinkHalfPeriod = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _blinkMinAlpha = 0.35f;

        public float FillDuration => _fillDuration;
        public Ease FillEase => _fillEase;
        public float LowHealthThreshold => _lowHealthThreshold;
        public float BlinkHalfPeriod => _blinkHalfPeriod;
        public float BlinkMinAlpha => _blinkMinAlpha;
    }
}
