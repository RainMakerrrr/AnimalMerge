using System;
using System.Collections;
using UnityEngine;

namespace Code.Pathfinding
{
    public class GridObjectsMover : MonoBehaviour
    {
        [SerializeField] private GridObject[] _gridObjects;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                StartCoroutine(MoveAllObjects());
        }

        private IEnumerator MoveAllObjects()
        {
            foreach (GridObject gridObject in _gridObjects)
            {
                gridObject.DetectNextTile();
                gridObject.MoveToTile(gridObject.selected_tile_s);

                while (gridObject.moving)
                {
                    yield return null;
                }
            }
        }
    }
}