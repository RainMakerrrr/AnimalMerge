using UnityEngine;

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "BattleCameraConfig", menuName = "Game/Battle Camera Config")]
    public class BattleCameraConfig : ScriptableObject
    {
        [Header("Zoom In")]
        [SerializeField, Range(0f, 4.5f)] private float _dollyDistance = 3.5f;
        [SerializeField, Min(0f)] private float _zoomInDuration = 0.6f;
        [SerializeField] private AnimationCurve _zoomInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] private float _zoomInDelay;

        [Header("Zoom Out")]
        [SerializeField, Min(0f)] private float _zoomOutDuration = 0.45f;
        [SerializeField] private AnimationCurve _zoomOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float DollyDistance => _dollyDistance;
        public float ZoomInDuration => _zoomInDuration;
        public AnimationCurve ZoomInCurve => _zoomInCurve;
        public float ZoomInDelay => _zoomInDelay;

        public float ZoomOutDuration => _zoomOutDuration;
        public AnimationCurve ZoomOutCurve => _zoomOutCurve;
    }
}
