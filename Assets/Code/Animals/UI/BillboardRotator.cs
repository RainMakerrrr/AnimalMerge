using UnityEngine;

namespace Code.Animals.UI
{
    public class BillboardRotator : MonoBehaviour
    {
        [SerializeField] private float _heightOffset = 2.0f;

        private Transform _cameraTransform;
        private Transform _parentTransform;

        private void Start()
        {
            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;
            _parentTransform = transform.parent;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || _parentTransform == null) return;
            transform.position = _parentTransform.position + _cameraTransform.up * _heightOffset;
            transform.rotation = _cameraTransform.rotation;
        }
    }
}
