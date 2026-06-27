using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.Infrastructure.Services.Input;
using UnityEngine;
using Zenject;

namespace Code.Animals
{
    public class AnimalMover : MonoBehaviour
    {
        [SerializeField] private float _dragThresholdPixels = 15f;
        [SerializeField] private float _holdTimeToShow = 0.15f;

        private IInputService _inputService;
        private Camera _camera;
        private IMoveRangeHighlighter _moveRangeHighlighter;

        private AnimalFacade _current;
        private Vector3 _originalPosition;
        private Vector3 _offset;

        private Vector2 _pressScreenPos;
        private float _pressTime;
        private bool _isDragging;
        private bool _rangeShown;


        [Inject]
        private void Construct(IInputService inputService, Camera mainCamera,
            IMoveRangeHighlighter moveRangeHighlighter)
        {
            _inputService = inputService;
            _camera = mainCamera;
            _moveRangeHighlighter = moveRangeHighlighter;
        }

        private void Update()
        {
            if (_inputService.IsMouseDown)
            {
                OnPress();
            }

            if (_current == null)
            {
                // Animal was destroyed/removed mid-gesture - clear any leftover highlight/gesture state.
                if (_rangeShown || _isDragging)
                    ResetGesture();
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
            _moveRangeHighlighter.Hide();
            _isDragging = false;
            _rangeShown = false;

            TryPickAnimal();

            if (_current == null) return;

            _pressScreenPos = _inputService.MousePosition;
            _pressTime = Time.time;
            _isDragging = false;
            _rangeShown = false;
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
            else if (!_rangeShown && Time.time - _pressTime >= _holdTimeToShow)
            {
                _moveRangeHighlighter.Show(_current.Movement);
                _rangeShown = true;
            }
        }

        private void BeginDrag()
        {
            if (_rangeShown)
                _moveRangeHighlighter.Hide();

            var unitOccupancy = _current.Occupancy;
            unitOccupancy?.SaveState();
            _current.Movement.ClearNodes();

            _isDragging = true;
            _rangeShown = false;
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
            }

            // For a non-drag hold this also clears the move-range highlight; for a drag it is a no-op (already hidden).
            ResetGesture();
        }

        private void ResetGesture()
        {
            _moveRangeHighlighter.Hide();
            _current = null;
            _isDragging = false;
            _rangeShown = false;
        }

        private void TryPickAnimal()
        {
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
