using Code.Animals.Movement;
using Code.Infrastructure.Services.Input;
using UnityEngine;
using Zenject;

namespace Code.Animals
{
    public class AnimalMover : MonoBehaviour
    {
        private IInputService _inputService;
        private Camera _camera;

        private AnimalMovement _current;
        private Vector3 _originalPosition;
        private Vector3 _offset;


        [Inject]
        private void Construct(IInputService inputService, Camera mainCamera)
        {
            _inputService = inputService;
            _camera = mainCamera;
        }

        private void Update()
        {
            if (_inputService.IsMouseDown)
            {
                TryPickAnimal();
            }

            if (_inputService.IsMouseDrag)
            {
                if (_current != null)
                    DragAnimal();
            }

            else if (_inputService.IsMouseUp)
            {
                if (_current == null) return;

                if (_current.TryPlace() == false)
                {
                    _current.transform.position = _originalPosition;
                    _current = null;
                }
                else
                {
                    _current = null;
                }
            }
        }

        private void TryPickAnimal()
        {
            RaycastHit hit = CastRay();

            if (hit.collider != null)
            {
                _current = hit.collider.GetComponentInParent<AnimalMovement>();
                if (_current != null)
                {
                    _originalPosition = _current.transform.position;
                    _offset = _current.transform.position - GetMouseAsWorldPoint();
                }
            }
        }

        private void DragAnimal()
        {
            Ray ray = _camera.ScreenPointToRay(_inputService.MousePosition);
            Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, _originalPosition.y, 0f));

            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPosition = ray.GetPoint(enter);
                _current.transform.position = worldPosition + _offset;
            }
        }

        private Vector3 GetMouseAsWorldPoint()
        {
            Ray ray = _camera.ScreenPointToRay(_inputService.MousePosition);
            float y = _current != null ? _current.transform.position.y : _originalPosition.y;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));

            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return _current != null ? _current.transform.position : Vector3.zero;
        }

        private RaycastHit CastRay()
        {
            Vector3 screenMousePosFar = new Vector3(_inputService.MousePosition.x, _inputService.MousePosition.y,
                _camera.farClipPlane);
            Vector3 screenMousePosNear = new Vector3(_inputService.MousePosition.x, _inputService.MousePosition.y,
                _camera.nearClipPlane);
            Vector3 worldMousePosFar = _camera.ScreenToWorldPoint(screenMousePosFar);
            Vector3 worldMousePosNear = _camera.ScreenToWorldPoint(screenMousePosNear);

            Physics.Raycast(worldMousePosNear, worldMousePosFar - worldMousePosNear, out RaycastHit hit);

            return hit;
        }
    }
}