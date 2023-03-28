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
            Vector3 position = new Vector3(_inputService.MousePosition.x, _inputService.MousePosition.y,
                _camera.WorldToScreenPoint(_current.transform.position).z);
            Vector3 worldPosition = _camera.ScreenToWorldPoint(position);
            worldPosition.y = 2f;
            _current.transform.position = worldPosition + _offset;
        }

        private Vector3 GetMouseAsWorldPoint()
        {
            Vector3 position = new Vector3(_inputService.MousePosition.x, _inputService.MousePosition.y,
                _camera.WorldToScreenPoint(_current.transform.position).z);
            Vector3 worldPosition = _camera.ScreenToWorldPoint(position);

            return worldPosition;
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