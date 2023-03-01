using System;
using Code.Infrastructure.Factories.Nodes;
using UnityEngine;
using Zenject;

namespace Code.Pathfinding
{
    public class Grid : MonoBehaviour
    {
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private float _cellSize;

        private Vector3 _originPosition = Vector3.zero;

        private PathNode[,] _gridArray;

        private IPathNodeFactory _nodeFactory;

        public float Width => _width;
        public float Height => _height;

        [Inject]
        private void Construct(IPathNodeFactory nodeFactory)
        {
            _nodeFactory = nodeFactory;
        }

        private void Awake()
        {
            CreateGrid();
        }

        private void CreateGrid()
        {
            _gridArray = new PathNode[_width, _height];

            for (int x = 0; x < _gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < _gridArray.GetLength(1); y++)
                {
                    PathNode node = _nodeFactory.Create(new Vector3(x, 0f, y), transform, x, y);

                    //PathNode node = Instantiate(_tilePrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                    node.name = $"Tile {x},{y}";
                    _gridArray[x, y] = node;
                }
            }
        }

        public float GetCellSize()
        {
            return _cellSize;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, 0, y) * _cellSize + _originPosition;
        }

        public void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt((worldPosition - _originPosition).x / _cellSize);
            y = Mathf.FloorToInt((worldPosition - _originPosition).y / _cellSize);
        }

        public PathNode GetGridObject(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < _width && y < _height)
            {
                return _gridArray[x, y];
            }
            else
            {
                return default(PathNode);
            }
        }

        public PathNode GetGridObject(Vector3 worldPosition)
        {
            int x, y;
            GetXY(worldPosition, out x, out y);
            return GetGridObject(x, y);
        }
    }
}