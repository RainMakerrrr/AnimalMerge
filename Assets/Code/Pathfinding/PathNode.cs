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

        private readonly Collider[] _colliders = new Collider[8];

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

        public bool HasNeighbours(ObjectSizeType objectSizeType, Vector3 direction)
        {
            if (objectSizeType == ObjectSizeType.Small) return true;

            Collider[] overlapSphere = GetTilesInRadius(objectSizeType, direction);

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

            return nodes.Count == Utilities.GetNodeCount(objectSizeType);
        }

        public bool IsNeighboursFree(ObjectSizeType objectSizeType, Vector3 direction)
        {
            if (objectSizeType == ObjectSizeType.Small) return true;

            Collider[] overlapSphere = GetTilesInRadius(objectSizeType, direction);

            List<PathNode> pathNodes = new List<PathNode>();
            
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

            return pathNodes.Count == Utilities.GetNodeCount(objectSizeType);
        }

        public Collider[] GetTilesInRadius(ObjectSizeType objectSizeType, Vector3 direction)
        {
            float radius = 0f;
            Vector3 offset = Vector3.zero;
            float directionOffset = direction == Vector3.forward ? 0.5f : -0.5f;

            switch (objectSizeType)
            {
                case ObjectSizeType.Small:
                    return Array.Empty<Collider>();
                case ObjectSizeType.Medium:
                    radius = 0.25f;
                    offset = new Vector3(0f, 0f, directionOffset);
                    break;
                case ObjectSizeType.Big:
                    radius = 0.5f;
                    offset = x == 7 ? new Vector3(-0.5f, 0f, 0.5f) : new Vector3(directionOffset, 0f, directionOffset);
                    break;
            }

            int count = Physics.OverlapSphereNonAlloc(transform.position + offset, radius, _colliders,
                LayerMask.GetMask(PathNodeLayer));

            return _colliders.Take(count).ToArray();
        }

        public List<PathNode> GetNeighbours(ObjectSizeType sizeType, Vector3 direction)
        {
            Collider[] colliders = GetTilesInRadius(sizeType, direction);

            return colliders.Select(c => c.GetComponent<PathNode>()).ToList();
        }

        public override string ToString() => x + "," + y;
    }
}