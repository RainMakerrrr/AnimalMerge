using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using DG.Tweening;
using Pathfinding;
using UnityEngine;

namespace Code.Logic.Animals
{
    public class Elephant : Animal
    {
        [SerializeField] private AnimalAnimator _animator;

        private GraphNode _targetNode;
        
        public override void PlaceOnTile(IEnumerable<Tile> tiles)
        {
            List<Tile> lowerTiles = tiles.OrderBy(tile => tile.Position.y).ToList();

            float center = (float) (lowerTiles[0].Position.x + lowerTiles[1].Position.x) / 2;

            transform.position = new Vector3(center, 0f, lowerTiles.FirstOrDefault()!.transform.position.z);
        }
        
        
        [ContextMenu("Move")]
        private void Move()
        {
            int i = 0;

            Tile current = GetUpperTile();

            if (current == null)
            {
                Debug.Log("you finish board");
                return;
            }

            current.ReleaseAnimal();

            Tile next = current.Next;


            while (i != 2)
            {
                next = current.Next;
                current = next;

                i++;
            }

            if (next.CanAssignAnimal(TilesCount, out List<Tile> neighbours))
            {
                List<Tile> lowerTiles = neighbours.OrderBy(tile => tile.Position.y).ToList();

                float center = (float) (lowerTiles[0].Position.x + lowerTiles[1].Position.x) / 2;

                //_path.destination = new Vector3(center, 0f, lowerTiles[0].Position.y);
                //
                // transform.DOMove(new Vector3(center, 0f, lowerTiles[0].Position.y), 2f)
                //     .OnComplete(() => _animator.UpdateMovementAnimation(0f));
                _animator.UpdateMovementAnimation(1f);

                //PlaceOnTile(neighbours);
                Tiles = neighbours.ToArray();

                foreach (Tile neighbour in neighbours)
                {
                    neighbour.AssignAnimal(this);
                }
            }
        }

        private Tile GetUpperTile()
        {
            return Tiles.OrderByDescending(tile => tile.Position.y).FirstOrDefault();
        }
    }
}