using System.Collections.Generic;
using Code.Pathfinding;
using UnityEngine;

namespace Code.NewPathfinding
{
    public static class Pathfinding 
    {
        private const int MoveStraightCost = 10;

        public static List<GridNode> FindPath(Vector2Int startPos, Vector2Int targetPos, Vector2Int unitSize)
        {
            GridNode startNode = GridManager.Instance.GetNode(startPos.x, startPos.y);
            GridNode targetNode = GridManager.Instance.GetNode(targetPos.x, targetPos.y);

            if (startNode == null || targetNode == null) return null;

            // Важно: проверяем, может ли юнит вообще встать на финишную точку своими габаритами
            if (!CanUnitOccupy(targetPos.x, targetPos.y, unitSize)) return null;

            List<GridNode> openList = new List<GridNode> { startNode };
            HashSet<GridNode> closedList = new HashSet<GridNode>();

            // Инициализация узлов
            for (int x = 0; x < GridManager.Instance.Width; x++)
            {
                for (int y = 0; y < GridManager.Instance.Height; y++)
                {
                    GridNode node = GridManager.Instance.GetNode(x, y);
                    node.GCost = int.MaxValue;
                    node.ParentNode = null;
                }
            }

            startNode.GCost = 0;
            startNode.HCost = CalculateDistance(startNode, targetNode);

            while (openList.Count > 0)
            {
                GridNode currentNode = GetLowestFCostNode(openList);

                if (currentNode == targetNode)
                {
                    return CalculatePath(targetNode);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                foreach (GridNode neighbor in GetNeighbors(currentNode))
                {
                    if (closedList.Contains(neighbor)) continue;

                    // Ключевое изменение: Проверка, помещается ли юнит размера unitSize,
                    // если его "якорная" точка (левый нижний угол) будет в neighbor
                    if (!CanUnitOccupy(neighbor.X, neighbor.Y, unitSize)) continue;

                    int tentativeGCost = currentNode.GCost + MoveStraightCost;

                    if (tentativeGCost < neighbor.GCost)
                    {
                        neighbor.ParentNode = currentNode;
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateDistance(neighbor, targetNode);

                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            return null; // Путь не найден
        }

        // Проверяет, свободны ли все клетки, которые займет юнит
        private static bool CanUnitOccupy(int rootX, int rootY, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    int checkX = rootX + x;
                    int checkY = rootY + y;

                    GridNode node = GridManager.Instance.GetNode(checkX, checkY);

                    // Если вышли за границы карты или тайл непроходим (или там стоит другой юнит)
                    if (node == null || !node.IsWalkable)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<GridNode> GetNeighbors(GridNode node)
        {
            List<GridNode> neighbors = new List<GridNode>();
            int[] xDirs = { 0, 0, -1, 1 };
            int[] yDirs = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int x = node.X + xDirs[i];
                int y = node.Y + yDirs[i];

                GridNode neighbor = GridManager.Instance.GetNode(x, y);
                if (neighbor != null) neighbors.Add(neighbor);
            }

            return neighbors;
        }

        private static List<GridNode> CalculatePath(GridNode endNode)
        {
            List<GridNode> path = new List<GridNode>();
            path.Add(endNode);
            GridNode currentNode = endNode;
            while (currentNode.ParentNode != null)
            {
                path.Add(currentNode.ParentNode);
                currentNode = currentNode.ParentNode;
            }

            path.Reverse();
            return path;
        }

        private static int CalculateDistance(GridNode a, GridNode b)
        {
            // Манхэттенское расстояние (так как движение только по 4 сторонам)
            int xDist = Mathf.Abs(a.X - b.X);
            int yDist = Mathf.Abs(a.Y - b.Y);
            return xDist + yDist;
        }

        private static GridNode GetLowestFCostNode(List<GridNode> nodeList)
        {
            GridNode lowestFCostNode = nodeList[0];
            for (int i = 1; i < nodeList.Count; i++)
            {
                if (nodeList[i].FCost < lowestFCostNode.FCost)
                {
                    lowestFCostNode = nodeList[i];
                }
            }

            return lowestFCostNode;
        }
    }
}