using System.Collections.Generic;
using System.Linq;
using Code.Logic.Animals;
using UnityEngine;

namespace Code.Logic.Boards
{
    public class Tile : MonoBehaviour
    {
        private const string TileLayerName = "Tile";
        public Vector2Int Position { get; set; }

        public bool IsEmpty => _animal == null;

        public IReadOnlyList<Tile> Neighbours => _neighbours;

        private Animal _animal;

        private Vector3[] _directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            new Vector3(1f, 0f, 1f),
            new Vector3(-1f, 0f, 1f),
            new Vector3(1f, 0f, -1f),
            new Vector3(-1f, 0f, -1f),
        };

        private readonly List<Tile> _neighbours = new List<Tile>();

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

            if (leftNeighbours.Count == 2)
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

            if (rightNeighbours.Count == 2)
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
            Collider[] colliders = Physics.OverlapSphere(transform.position, 1f, LayerMask.GetMask(TileLayerName));
            if (colliders.Length == 0) return;

            foreach (Collider tileCollider in colliders)
            {
                var tile = tileCollider.GetComponent<Tile>();
                if (tile == this) continue;

                if (tile != null)
                {
                    if (Vector3.Distance(transform.position, tile.transform.position) <= 2f)
                    {
                        if (_neighbours.Contains(tile) == false)
                        {
                            _neighbours.Add(tile);
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            //Gizmos.DrawRay(transform.position, new Vector3(1f, 0f, -1f));
            Gizmos.DrawSphere(transform.position, 1f);
        }
    }
}