using System.Collections.Generic;
using Code.Logic.Animals;
using Code.Logic.Boards;
using Lean.Touch;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Code.Logic
{
    public class AnimalMover : MonoBehaviour
    {
        private Camera _camera;
        private Animal _currentAnimal;

        private void Start()
        {
            _camera = Camera.main;

            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUpdate += OnFingerUpdate;
            LeanTouch.OnFingerUp += OnFingerUp;
        }

        private void OnDestroy()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            LeanTouch.OnFingerUpdate -= OnFingerUpdate;
            LeanTouch.OnFingerUp -= OnFingerUp;
        }

        private void OnFingerDown(LeanFinger finger)
        {
            if (finger.IsOverGui) return;
            
            RaycastHit hit = CastRay(finger.ScreenPosition);

            SelectAnimal(ref hit);
        }

        private void OnFingerUpdate(LeanFinger finger)
        {
            if (_currentAnimal == null) return;

            Vector3 screenPosition = new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y,
                _camera.WorldToScreenPoint(_currentAnimal.transform.position).z);
            Vector3 worldPosition = _camera.ScreenToWorldPoint(screenPosition);
            _currentAnimal.transform.position = new Vector3(worldPosition.x, .25f, worldPosition.z);
        }


        private void SelectAnimal(ref RaycastHit hit)
        {
            var animal = hit.collider.GetComponentInParent<Animal>();
            if (animal == null) return;

            _currentAnimal = animal;
            foreach (Tile tile in _currentAnimal.Tiles)
            {
                tile.ReleaseAnimal();
            }

            Debug.Log(animal.name);
        }

        private void OnFingerUp(LeanFinger finger)
        {
            if (TryPlaceAnimal())
            {
                _currentAnimal = null;
            }
        }

        private bool TryPlaceAnimal()
        {
            if (Physics.Raycast(_currentAnimal.transform.position, Vector3.down, out RaycastHit hit))
            {
                var tile = hit.collider.GetComponent<Tile>();
                if (tile == null) return false;

                if (tile.CanAssignAnimal(_currentAnimal.TilesCount, out List<Tile> neighbours))
                {
                    _currentAnimal.PlaceOnTile(neighbours);
                    _currentAnimal.Tiles = neighbours.ToArray();

                    foreach (Tile neighbour in neighbours)
                    {
                        neighbour.AssignAnimal(_currentAnimal);
                    }

                    return true;
                }
            }

            return false;
        }

        private RaycastHit CastRay(Vector2 screenPosition)
        {
            Vector3 screenMousePosFar = new Vector3(
                screenPosition.x,
                screenPosition.y,
                _camera.farClipPlane);

            Vector3 screenMousePosNear = new Vector3(
                screenPosition.x,
                screenPosition.y,
                _camera.nearClipPlane);

            Vector3 worldMousePosFar = _camera.ScreenToWorldPoint(screenMousePosFar);
            Vector3 worldMousePosNear = _camera.ScreenToWorldPoint(screenMousePosNear);

            Physics.Raycast(worldMousePosNear, worldMousePosFar - worldMousePosNear, out RaycastHit hit);

            return hit;
        }
    }
}