using UnityEngine;

namespace Code.Animals.Vfx.Config
{
    [CreateAssetMenu(fileName = "MergeAnimationConfig", menuName = "Game/Merge Animation Config")]
    public class MergeAnimationConfig : ScriptableObject
    {
        [Header("Merge Scale Change")]
        [SerializeField, Min(0.01f)] private float _growScaleMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float _growDuration = 0.35f;
        [SerializeField] private AnimationCurve _growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float _cloneScaleMultiplier = 0.75f;

        [Header("Appear")]
        [SerializeField, Range(0f, 1f)] private float _appearStartScale;
        [SerializeField, Min(0f)] private float _appearDuration = 0.35f;
        [SerializeField] private AnimationCurve _appearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] private float _appearDelay;

        [Header("Glow")]
        [SerializeField] private Material _glowMaterial;
        [SerializeField, Min(0f)] private float _glowDuration = 0.45f;
        [SerializeField, Min(0.0001f)] private float _glowBandsPerBody = 0.5f;
        [SerializeField, Min(0f)] private float _glowBandMargin = 0.18f;
        [SerializeField] private AnimationCurve _glowEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Vector3 _glowAxis = Vector3.up;

        public float GrowScaleMultiplier => _growScaleMultiplier;
        public float GrowDuration => _growDuration;
        public AnimationCurve GrowCurve => _growCurve;
        public float CloneScaleMultiplier => _cloneScaleMultiplier;

        public float AppearStartScale => _appearStartScale;
        public float AppearDuration => _appearDuration;
        public AnimationCurve AppearCurve => _appearCurve;
        public float AppearDelay => _appearDelay;

        public Material GlowMaterial => _glowMaterial;
        public float GlowDuration => _glowDuration;
        public float GlowBandsPerBody => _glowBandsPerBody;
        public float GlowBandMargin => _glowBandMargin;
        public AnimationCurve GlowEase => _glowEase;
        public Vector3 GlowAxis => _glowAxis;

        private void OnValidate()
        {
            var bandsPerBodyThatKeepsBandOnBody = 1f / (1f + 2f * _glowBandMargin);
            _glowBandsPerBody = Mathf.Min(_glowBandsPerBody, bandsPerBodyThatKeepsBandOnBody);
        }
    }
}
