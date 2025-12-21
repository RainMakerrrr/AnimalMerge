using System.Collections.Generic;
using UnityEngine;

namespace Code.NewPathfinding
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Settings")] [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 10;
        [SerializeField] private float _cellSize = 1.0f;
        [SerializeField] private GridNode _nodePrefab;
        [SerializeField] private Transform _gridOrigin;

        private GridNode[,] _grid;
        
        public int Width => _width;

        public int Height => _height;

        public float CellSize => _cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            CreateGrid();
        }

        private void CreateGrid()
        {
            _grid = new GridNode[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 worldPos = _gridOrigin.position + new Vector3(x * _cellSize, 0, y * _cellSize);
                    var gridNode = Instantiate(_nodePrefab, worldPos, Quaternion.identity, transform);
                    gridNode.name = $"Node_{x}_{y}";

                    gridNode.Initialize(x, y);
                    _grid[x, y] = gridNode;
                }
            }
        }

        public GridNode GetNode(int x, int y)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                return _grid[x, y];
            }

            return null;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            var gridNode = GetNode(x, y);
            
            if (gridNode != null)
            {
                return gridNode.transform.position;
            }

            return Vector3.zero;
        }

        // Перевод мировых координат в координаты сетки
        public void GetGridCoordinates(Vector3 worldPosition, out int x, out int y)
        {
            Vector3 localPos = worldPosition - _gridOrigin.position;
            x = Mathf.RoundToInt(localPos.x / _cellSize);
            y = Mathf.RoundToInt(localPos.z / _cellSize);
        }
        
    }
}