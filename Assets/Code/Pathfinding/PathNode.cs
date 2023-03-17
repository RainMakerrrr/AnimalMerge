using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Code.Pathfinding
{
    public class PathNode : MonoBehaviour
    {
        private const string PathNodeLayer = "PathNode";

        public int x;
        public int y;

        public int gCost;
        public int hCost;
        public PathNode previousNode;

        public Vector3 WorldPosition => new Vector3(x, 0f, y);

        public int FCost => gCost + hCost;
        public bool IsWalkable;
        public bool CanPlace;

        public void Construct(int x, int y)
        {
            this.x = x;
            this.y = y;
            IsWalkable = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + new Vector3(0f, 0f, 0.5f), 0.25f);
        }

        public bool HasNeighbours(ObjectSizeType objectSizeType)
        {
            if (objectSizeType == ObjectSizeType.Small) return true;

            Collider[] overlapSphere = GetTilesInRadius(objectSizeType);

            List<PathNode> nodes = new List<PathNode>();
            
            foreach (Collider collider1 in overlapSphere)
            {
                var pathNode = collider1.GetComponent<PathNode>();
                if (pathNode != null)
                {
                    if (pathNode.CanPlace && pathNode.IsWalkable)
                        nodes.Add(pathNode);
                }
            }

            return nodes.Count == GetNodeCount(objectSizeType);
        }

        public bool IsNeighboursFree(ObjectSizeType objectSizeType)
        {
            if (objectSizeType == ObjectSizeType.Small) return true;

            Collider[] overlapSphere = GetTilesInRadius(objectSizeType);

            List<PathNode> pathNodes = new List<PathNode>();

            Debug.Log($"Colliders count - {overlapSphere.Length}");

            foreach (Collider collider1 in overlapSphere)
            {
                var pathNode = collider1.GetComponent<PathNode>();
                if (pathNode != null)
                {
                    Debug.Log(pathNode.name);

                    if (pathNode.IsWalkable)
                        pathNodes.Add(pathNode);
                }
            }

            Debug.Log(pathNodes.Count);

            
            return pathNodes.Count == GetNodeCount(objectSizeType);
        }
        
        public Collider[] GetTilesInRadius(ObjectSizeType objectSizeType)
        {
            float radius = 0f;
            Vector3 offset = Vector3.zero;

            switch (objectSizeType)
            {
                case ObjectSizeType.Small:
                    return Array.Empty<Collider>();
                case ObjectSizeType.Medium:
                    radius = 0.25f;
                    offset = new Vector3(0f, 0f, 0.5f);
                    break;
                case ObjectSizeType.Big:
                    radius = 0.5f;
                    offset = x == 7 ? new Vector3(-0.5f, 0f, 0.5f) : new Vector3(0.5f, 0f, 0.5f);
                    break;
            }


            return Physics.OverlapSphere(transform.position + offset, radius, LayerMask.GetMask(PathNodeLayer));
        }

        private int GetNodeCount(ObjectSizeType sizeType)
        {
            switch (sizeType)
            {
                case ObjectSizeType.Small:
                    return 1;
                case ObjectSizeType.Medium:
                    return 2;
                case ObjectSizeType.Big:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null);
            }
        }

        public List<PathNode> GetNeighbours(ObjectSizeType sizeType)
        {
            Collider[] colliders = GetTilesInRadius(sizeType);

            return colliders.Select(c => c.GetComponent<PathNode>()).ToList();
        }

        private void TryDetectObstacle()
        {
            IsWalkable = !Physics.Raycast(transform.position, Vector3.up, 1f);

            //RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.up, 100f);

            //IsWalkable = hits.Length <= 0;
        }

        public override string ToString() => x + "," + y;
    }
}