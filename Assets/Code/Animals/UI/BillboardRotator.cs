using UnityEngine;

namespace Code.Animals.UI
{
    public class BillboardRotator : MonoBehaviour
    {
        [SerializeField] private float _heightOffset = 2.0f;
        [SerializeField] private float _lateralOffset;

        private Transform _cameraTransform;
        private Transform _parentTransform;
        private Vector3 _referenceParentScale = Vector3.one;

        private void Awake()
        {
            _parentTransform = transform.parent;

            if (_parentTransform != null)
                _referenceParentScale = _parentTransform.lossyScale;
        }

        private void Start()
        {
            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || _parentTransform == null) return;

            var parentScale = _parentTransform.lossyScale;

            transform.position = _parentTransform.position
                + _cameraTransform.up * (_heightOffset * ScaleRatio(parentScale.y, _referenceParentScale.y))
                + _cameraTransform.right * (_lateralOffset * ScaleRatio(parentScale.x, _referenceParentScale.x));
            transform.rotation = _cameraTransform.rotation;
        }

        private static float ScaleRatio(float current, float reference) =>
            Mathf.Approximately(reference, 0f) ? 1f : current / reference;
    }
}
