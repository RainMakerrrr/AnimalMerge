using System;
using UnityEngine;

namespace Code
{
    public class TestRaycast : MonoBehaviour
    {
        [SerializeField] private GameObject[] _tiles;
        [SerializeField] private float _radius;

        private void Update()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
            {
                if (hit.collider != null)
                {
                    Debug.Log(hit.collider.name);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_tiles == null || _tiles.Length < 4) return;

            Vector3 center = Vector3.zero;

            foreach (GameObject tile in _tiles)
            {
                center += tile.transform.position;
            }

            center /= _tiles.Length;
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(center, Vector3.up);
            Gizmos.DrawSphere(center, _radius);
        }
    }
}