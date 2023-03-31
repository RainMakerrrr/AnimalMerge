using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Code.Pathfinding
{
    public class Pathfinder : IPathfinder
    {
        private const int MoveStraightCost = 10;
        private const int MoveDiagonalCost = 14;

        private List<PathNode> _openList;
        private List<PathNode> _closedList;

        private readonly Grid _grid;

        [Inject]
        public Pathfinder(Grid grid)
        {
            _grid = grid;
        }

        public List<PathNode> FindPath(int startX, int startY, int endX, int endY, ObjectSizeType objectSizeType,
            Vector3 direction)
        {
            PathNode startNode = _grid.GetGridObject(startX, startY);
            PathNode endNode = _grid.GetGridObject(endX, endY);

            _openList = new List<PathNode> {startNode};

            _closedList = new List<PathNode>();
            
            List<PathNode> neighbours = startNode.GetTilesInRadius(objectSizeType, direction).Select(c => c.GetComponent<PathNode>()).Except(new []{startNode})
                .ToList();

            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    PathNode pathNode = _grid.GetGridObject(x, y);
                    pathNode.gCost = int.MaxValue;
                    pathNode.previousNode = null;
                }
            }

            startNode.gCost = 0;
            startNode.hCost = CalculateDistanceCost(startNode, endNode);

            while (_openList.Count > 0)
            {
                PathNode current = GetLowestCostNode(_openList);

                if (current == endNode && current.IsWalkable)
                {
                    
                    if (current.IsNeighboursFree(objectSizeType, direction))
                    {
                        return CalculatePath(endNode);

                    }
                }

                _openList.Remove(current);
                _closedList.Add(current);

                foreach (PathNode neighbour in GetNeighbours(current))
                {
                    if (_closedList.Contains(neighbour)) continue;

                    if (!neighbours.Contains(neighbour))
                    {
                        if (!neighbour.IsWalkable || !neighbour.IsNeighboursFree(objectSizeType, direction))
                        {
                            _closedList.Add(neighbour);
                            continue;
                        }
                    }

                    int tentativeGCost = current.gCost + CalculateDistanceCost(current, neighbour);

                    if (tentativeGCost < neighbour.gCost)
                    {
                        neighbour.previousNode = current;
                        neighbour.gCost = tentativeGCost;
                        neighbour.hCost = CalculateDistanceCost(neighbour, endNode);

                        if (!_openList.Contains(neighbour))
                            _openList.Add(neighbour);
                    }
                }
            }

            return null;
        }

        private List<PathNode> GetNeighbours(PathNode node)
        {
            List<PathNode> neighbours = new List<PathNode>();

            if (node.x - 1 >= 0)
            {
                //Left
                neighbours.Add(_grid.GetGridObject(node.x - 1, node.y));
                //LeftDown
                if (node.y - 1 >= 0) neighbours.Add(_grid.GetGridObject(node.x - 1, node.y - 1));
                //LeftUp
                if (node.y + 1 < _grid.Height) neighbours.Add(_grid.GetGridObject(node.x - 1, node.y + 1));
            }

            if (node.x + 1 < _grid.Width)
            {
                //Right
                neighbours.Add(_grid.GetGridObject(node.x + 1, node.y));
                //RightDown
                if (node.y - 1 >= 0) neighbours.Add(_grid.GetGridObject(node.x + 1, node.y - 1));
                //RightUp
                if (node.y + 1 < _grid.Height) neighbours.Add(_grid.GetGridObject(node.x + 1, node.y + 1));
            }

            //Down
            if (node.y - 1 >= 0) neighbours.Add(_grid.GetGridObject(node.x, node.y - 1));
            //Up
            if (node.y + 1 < _grid.Height) neighbours.Add(_grid.GetGridObject(node.x, node.y + 1));

            return neighbours;
        }

        private List<PathNode> CalculatePath(PathNode endNode)
        {
            List<PathNode> path = new List<PathNode>();
            path.Add(endNode);

            PathNode currentNode = endNode;

            while (currentNode.previousNode != null)
            {
                path.Add(currentNode.previousNode);
                currentNode = currentNode.previousNode;
            }

            path.Reverse();
            return path;
        }

        private int CalculateDistanceCost(PathNode a, PathNode b)
        {
            int xDistance = Mathf.Abs(a.x - b.x);
            int yDistance = Mathf.Abs(a.y - b.y);
            int remaining = Mathf.Abs(xDistance - yDistance);

            return MoveDiagonalCost * Mathf.Min(xDistance, yDistance) + MoveStraightCost * remaining;
        }

        private PathNode GetLowestCostNode(List<PathNode> nodes)
        {
            PathNode pathNode = nodes[0];

            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].FCost < pathNode.FCost)
                    pathNode = nodes[i];
            }

            return pathNode;
        }
    }
}