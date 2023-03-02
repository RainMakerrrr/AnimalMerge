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
    public class AnimalMovement : MonoBehaviour
    {
        [SerializeField] private float _yPos;
        [SerializeField] private Vector2Int _targetPoint;
        [SerializeField] private ObjectSizeType _sizeType;

        private IPathfinder _pathfinder;
        private Grid _grid;

        private PathNode _currentPathNode;
        public ObjectSizeType ObjectSizeType => _sizeType;


        [Inject]
        private void Construct(IPathfinder pathfinder, Grid grid)
        {
            _pathfinder = pathfinder;
            _grid = grid;
        }

        // private void Start()
        // {
        //     _currentPathNode = _grid.GetGridObject(0, 0);
        //     Vector3 worldPosition = _currentPathNode.WorldPosition;
        //     worldPosition.y = _yPos;
        //     transform.position = worldPosition + GetMovementOffset();
        // }

        public void Place(Vector3 position)
        {
            position.y = _yPos;
            transform.position = position + GetMovementOffset();
        }

        private void Place(Vector3 position, Vector3 offset)
        {
            position.y = _yPos;
            transform.position = position + offset;
        }

        public bool TryPlace()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 20f,
                    LayerMask.GetMask("PathNode")))
            {
                var pathNode = hit.collider.GetComponent<PathNode>();

                Debug.Log(pathNode.name);

                if (pathNode.CanPlace && pathNode.HasNeighbours(_sizeType))
                {
                    Place(pathNode.WorldPosition, GetMovementOffset(pathNode));
                    return true;
                }

                PathNode lowerNeighbour = _grid.GetGridObject(pathNode.x, pathNode.y - 1);

                if (lowerNeighbour != null)
                {
                    if (lowerNeighbour.CanPlace && lowerNeighbour.HasNeighbours(_sizeType))
                    {
                        Place(lowerNeighbour.WorldPosition, GetMovementOffset(pathNode));
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                List<PathNode> path =
                    _pathfinder.FindPath(_currentPathNode.x, _currentPathNode.y, _targetPoint.x, _targetPoint.y,
                        _sizeType);

                if (path == null) return;

                _currentPathNode = path.Last();

                Vector3[] pathPositions = path.Select(node => node.WorldPosition).ToArray();

                for (int i = 0; i < pathPositions.Length; i++)
                {
                    pathPositions[i].y = _yPos;
                    pathPositions[i] += GetMovementOffset();
                }


                transform.DOPath(pathPositions, pathPositions.Length / 2f).SetEase(Ease.Linear);
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

        private Vector3 GetMovementOffset(PathNode node)
        {
            switch (_sizeType)
            {
                case ObjectSizeType.Small:
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    return new Vector3(0f, 0f, 0.5f);
                case ObjectSizeType.Big:
                    return node.x == 7 ? new Vector3(-0.5f, 0f, 0.5f) : new Vector3(0.5f, 0f, 0.5f);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}