using System.Collections.Generic;
using System.Linq;
using Code.Logic.Animals;
using Pathfinding;
using UnityEngine;

namespace Code.Logic.Boards
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private TileType _type;
        [SerializeField] private string _layerName;
        [SerializeField] private float _radius;
        public Vector2Int Position { get; set; }

        public bool IsEmpty => _animal == null;

        public TileType Type => _type;

        public IReadOnlyList<Tile> Neighbours => _neighbours;

        public Tile Next { get; private set; }

        private Animal _animal;

        private List<Tile> _neighbours = new List<Tile>();

        public GraphNode Node { get; private set; }
        
        public void TryFindNode() =>
            Node = AstarPath.active.graphs[0].GetNearest(transform.position, NNConstraint.None).node;

        public void AssignAnimal(Animal animal) => _animal = animal;

        public void ReleaseAnimal() => _animal = null;

        public bool CanAssignAnimal(int tilesCount, out List<Tile> neighbours)
        {
            neighbours = new List<Tile>();

            if (tilesCount == 1 && IsEmpty)
            {
                neighbours.Add(this);
                return true;
            }

            List<Tile> freeNeighbours = _neighbours.Where(tile => tile.IsEmpty).ToList();

            if (freeNeighbours.Count == 0 || freeNeighbours.Count < tilesCount - 1)
            {
                Debug.Log("No free neighbours");

                neighbours = new List<Tile>();
                return false;
            }

            Tile closestTile = freeNeighbours.FirstOrDefault(neighbour =>
                neighbour.Position.x == Position.x && neighbour.Position.y != Position.y);

            if (closestTile != null && closestTile.IsEmpty == false)
            {
                Debug.Log("Closest tile is full");

                neighbours = new List<Tile>();
                return false;
            }

            if (tilesCount == 2)
            {
                Debug.Log("Count is 2 and find neighbours");
                neighbours.Add(this);
                neighbours.Add(closestTile);
                return true;
            }


            List<Tile> leftNeighbours = freeNeighbours.Where(neighbour => neighbour.Position.x < Position.x).ToList();

            if (leftNeighbours.Count >= 2)
            {
                if (leftNeighbours.All(neighbour => neighbour.IsEmpty))
                {
                    Debug.Log("Find Left neighbours");

                    neighbours.Add(this);
                    neighbours.Add(closestTile);
                    neighbours.AddRange(leftNeighbours);

                    return true;
                }
            }

            List<Tile> rightNeighbours = freeNeighbours.Where(neighbour => neighbour.Position.x > Position.x).ToList();

            if (rightNeighbours.Count >= 2)
            {
                if (rightNeighbours.All(neighbour => neighbour.IsEmpty))
                {
                    Debug.Log("Find Right neighbours");

                    neighbours.Add(this);
                    neighbours.Add(closestTile);
                    neighbours.AddRange(rightNeighbours);

                    return true;
                }
            }

            Debug.Log("No neightbours");
            neighbours = new List<Tile>();
            return false;
        }

        public void TryFindNeighbours()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _radius, LayerMask.GetMask(_layerName));
            if (colliders.Length == 0) return;
            
            foreach (Collider tileCollider in colliders)
            {
                var tile = tileCollider.GetComponent<Tile>();
                if (tile == this) continue;

                if (tile != null)
                {
                    if (_neighbours.Contains(tile) == false)
                    {
                        _neighbours.Add(tile);
                    }
                }
            }

            TryFindNext();
        }

        private void TryFindNext()
        {
            Next = _neighbours.FirstOrDefault(neighbour =>
                neighbour.Position.x == Position.x && neighbour.Position.y > Position.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.4f);
        }
    }
}