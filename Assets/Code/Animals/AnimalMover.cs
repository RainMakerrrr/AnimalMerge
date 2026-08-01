using Code.Animals.Facades;
using Code.Animals.Selection;
using Code.Infrastructure.Services.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Code.Animals
{
    public class AnimalMover : MonoBehaviour
    {
        [SerializeField] private float _dragThresholdPixels = 15f;
        [SerializeField] private float _holdTimeToShow = 0.15f;

        private IInputService _inputService;
        private Camera _camera;
        private IAnimalSelectionService _selectionService;

        private AnimalFacade _current;
        private Vector3 _originalPosition;
        private Vector3 _offset;

        private Vector2 _pressScreenPos;
        private float _pressTime;
        private bool _isDragging;
        private bool _selectionShown;
        private bool _pressedOnSelected;


        [Inject]
        private void Construct(IInputService inputService, Camera mainCamera,
            IAnimalSelectionService selectionService)
        {
            _inputService = inputService;
            _camera = mainCamera;
            _selectionService = selectionService;
        }

        private void Update()
        {
            if (_inputService.IsMouseDown)
            {
                OnPress();
            }

            if (_current == null)
            {
                if (_selectionShown || _isDragging)
                {
                    _selectionService.Clear();
                    ResetGesture();
                }

                return;
            }

            if (_inputService.IsMouseDrag)
            {
                OnHeld();
            }
            else if (_inputService.IsMouseUp)
            {
                OnRelease();
            }
        }

        private void OnPress()
        {
            if (IsPointerOverUi()) return;

            _isDragging = false;
            _selectionShown = false;

            TryPickAnimal();

            if (_current == null)
            {
                _selectionService.Clear();
                return;
            }

            _pressedOnSelected = ReferenceEquals(_current, _selectionService.Selected);

            _pressScreenPos = _inputService.MousePosition;
            _pressTime = Time.time;
        }

        private bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;

            if (eventSystem == null) return false;

            if (UnityEngine.Input.touchCount > 0)
                return eventSystem.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId);

            return eventSystem.IsPointerOverGameObject();
        }

        private void OnHeld()
        {
            if (_isDragging)
            {
                DragAnimal();
                return;
            }

            var moved = ((Vector2)_inputService.MousePosition - _pressScreenPos).magnitude;

            if (moved > DragThreshold())
            {
                BeginDrag();
            }
            else if (!_selectionShown && Time.time - _pressTime >= _holdTimeToShow)
            {
                _selectionService.Select(_current);
                _selectionShown = true;
                _pressedOnSelected = false;
            }
        }

        private void BeginDrag()
        {
            _selectionService.Clear();

            var unitOccupancy = _current.Occupancy;
            unitOccupancy?.SaveState();
            _current.Movement.ClearNodes();

            _isDragging = true;
            _selectionShown = false;
        }

        private void OnRelease()
        {
            if (_isDragging)
            {
                var unitOccupancy = _current.Occupancy;

                if (_current.Movement.TryPlace() == false)
                {
                    // Placement failed - restore original position and grid occupancy
                    _current.transform.position = _originalPosition;
                    unitOccupancy?.RestoreState();
                }
                else
                {
                    // Placement succeeded - clear saved state
                    unitOccupancy?.ClearSavedState();
                }

                _selectionService.Clear();
            }
            else if (_pressedOnSelected)
            {
                _selectionService.Clear();
            }
            else
            {
                _selectionService.Select(_current);
            }

            ResetGesture();
        }

        private void ResetGesture()
        {
            _current = null;
            _isDragging = false;
            _selectionShown = false;
            _pressedOnSelected = false;
        }

        private void TryPickAnimal()
        {
            _current = null;

            var hit = CastRay();

            if (hit.collider != null)
            {
                _current = hit.collider.GetComponentInParent<AnimalFacade>();
                if (_current != null)
                {
                    _originalPosition = _current.transform.position;
                    _offset = _current.transform.position - GetMouseAsWorldPoint();
                }
            }
        }

        private void DragAnimal()
        {
            var ray = _camera.ScreenPointToRay(_inputService.MousePosition);
            var dragPlane = new Plane(Vector3.up, new Vector3(0f, _originalPosition.y, 0f));

            if (dragPlane.Raycast(ray, out var enter))
            {
                var worldPosition = ray.GetPoint(enter);
                _current.transform.position = worldPosition + _offset;
            }
        }

        private float DragThreshold() => _dragThresholdPixels * (Screen.dpi > 0 ? Screen.dpi / 160f : 1f);

        private Vector3 GetMouseAsWorldPoint()
        {
            var ray = _camera.ScreenPointToRay(_inputService.MousePosition);
            var y = _current != null ? _current.transform.position.y : _originalPosition.y;
            var plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));

            if (plane.Raycast(ray, out var enter))
            {
                return ray.GetPoint(enter);
            }

            return _current != null ? _current.transform.position : Vector3.zero;
        }

        private RaycastHit CastRay()
        {
            var screenMousePosFar = new Vector3(_inputService.MousePosition.x, _inputService.MousePosition.y,
                _camera.farClipPlane);
            var screenMousePosNear = new Vector3(_inputService.MousePosition.x, _inputService.MousePosition.y,
                _camera.nearClipPlane);
            var worldMousePosFar = _camera.ScreenToWorldPoint(screenMousePosFar);
            var worldMousePosNear = _camera.ScreenToWorldPoint(screenMousePosNear);

            Physics.Raycast(worldMousePosNear, worldMousePosFar - worldMousePosNear, out var hit, Mathf.Infinity,
                layerMask: LayerMask.GetMask("Animal"));

            return hit;
        }
    }
}
