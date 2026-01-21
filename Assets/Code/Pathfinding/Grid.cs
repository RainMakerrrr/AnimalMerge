using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Animals;
using Code.Animals.Movement;
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

        [Header("Debug Visualization")]
        [SerializeField] private bool _debugDrawGrid = true;
        [SerializeField] private float _debugCellHeight = 0.02f;
        [SerializeField] private Color _freeColor = new Color(0f, 1f, 0f, 0.35f);
        [SerializeField] private Color _occupiedColor = new Color(1f, 0f, 0f, 0.35f);

        private Vector3 _originPosition;

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
            _originPosition = transform.position;
            CreateGrid();
        }

        private void CreateGrid()
        {
            _gridArray = new PathNode[_width, _height];

            for (int x = 0; x < _gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < _gridArray.GetLength(1); y++)
                {
                    PathNode node = _nodeFactory.Create(_originPosition + new Vector3(x, 0f, y), transform, x, y, this);

                    if (y == 0 || y == 1)
                    {
                        node.CanPlace = true;
                    }

                    node.name = $"Tile {x},{y}";
                    _gridArray[x, y] = node;
                    node.UpdateVisual();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!_debugDrawGrid) return;
            if (_gridArray == null) return;

            float visualSize = 0.9f; // slightly smaller than 1 to see borders
            Vector3 boxSize = new Vector3(visualSize, _debugCellHeight, visualSize);

            for (int x = 0; x < _gridArray.GetLength(0); x++)
            {
                for (int y = 0; y < _gridArray.GetLength(1); y++)
                {
                    PathNode node = _gridArray[x, y];
                    if (node == null) continue;

                    Gizmos.color = node.IsWalkable ? _freeColor : _occupiedColor;
                    Vector3 pos = node.transform.position + new Vector3(0f, _debugCellHeight * 0.5f, 0f);
                    Gizmos.DrawCube(pos, boxSize);
                }
            }
        }

        public bool HasPathNode(PathNode node)
        {
            PathNode[] nodes = _gridArray.Cast<PathNode>().ToArray();

            return nodes.Any(n => n == node);
        }
        
        public bool HasNodeFor(AnimalType animalType)
        {
            List<PathNode> nodes = SortNodes().ToList();

            switch (animalType)
            {
                case AnimalType.Elephant:
                    return nodes.Count >= 2;
                case AnimalType.Cheetah:
                    return nodes.Count >= 1;
                case AnimalType.Deer:
                    return nodes.Count >= 1;
                case AnimalType.Fox:
                    return nodes.Count >= 1;
                case AnimalType.Hedgehog:
                    return nodes.Count >= 1;
                case AnimalType.Chicken:
                    return nodes.Count >= 1;
            }

            return false;
        }

        public void PlaceOnGrid(AnimalMovement animal)
        {
            IEnumerable<PathNode> nodes = SortNodes();

            PathNode node = nodes.FirstOrDefault();
            if (node == null) return;

            List<PathNode> neighbours = node.GetNeighbours(animal.ObjectSizeType, animal.Direction);
            if (neighbours.Count == 0)
            {
                node.IsWalkable = false;
                node.UpdateVisual();
               // animal.SetCurrentNode(node);
            }
            else
            {
               // animal.SetCurrentNode(node);
                
                neighbours.ForEach(neighbour => { neighbour.IsWalkable = false; neighbour.UpdateVisual(); });

                //animal.FillNodes(neighbours);
            }

            animal.Place(node.WorldPosition);
        }

        private IEnumerable<PathNode> SortNodes()
        {
            PathNode[] nodes = _gridArray.Cast<PathNode>().ToArray();

            return nodes.Where(node => Math.Abs(node.y - _originPosition.z) < 0.1f && node.IsWalkable)
                .OrderBy(node => node.x).ToArray();
        }

        private void GetXY(Vector3 worldPosition, out int x, out int y)
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