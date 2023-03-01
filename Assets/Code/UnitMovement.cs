using System;
using System.Collections.Generic;
using System.Linq;
using Code.Pathfinding;
using DG.Tweening;
using UnityEngine;
using Zenject;
using Grid = Code.Pathfinding.Grid;

namespace Code
{
    public class UnitMovement : MonoBehaviour
    {
        [SerializeField] private float _yPos;
        [SerializeField] private ObjectSizeType _sizeType;

        private IPathfinder _pathfinder;
        private Grid _grid;

        private PathNode _currentPathNode;

        [Inject]
        private void Construct(IPathfinder pathfinder, Grid grid)
        {
            _pathfinder = pathfinder;
            _grid = grid;
        }

        private void Start()
        {
            _currentPathNode = _grid.GetGridObject(0, 0);
            Vector3 worldPosition = _currentPathNode.WorldPosition;
            worldPosition.y = _yPos;
            transform.position = worldPosition + GetMovementOffset();
        }

        [SerializeField] private Vector2Int _targetPoint;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                List<PathNode> path =
                    _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, _targetPoint.x, _targetPoint.y,
                        ObjectSizeType.Big);

                if (path == null) return;

                _currentPathNode = path.Last();

                Vector3[] pathPositions = path.Select(node => node.WorldPosition).ToArray();

                for (int i = 0; i < pathPositions.Length; i++)
                {
                    pathPositions[i].y = _yPos;
                    pathPositions[i] += GetMovementOffset();
                }


                transform.DOPath(pathPositions, 4f).SetEase(Ease.Linear);
            }
        }

        private Vector3 GetMovementOffset()
        {
            switch (_sizeType)
            {
                case ObjectSizeType.Small:
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    return new Vector3(0f, 0f, 0.5f);
                case ObjectSizeType.Big:
                    return new Vector3(0.5f, 0f, 0.5f);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}