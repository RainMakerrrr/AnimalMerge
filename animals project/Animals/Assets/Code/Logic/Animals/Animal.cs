using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using UnityEngine;

namespace Code.Logic.Animals
{
    public abstract class Animal : MonoBehaviour
    {
        [SerializeField] private string _tileMask;
        [SerializeField] private int _tilesCount;
        [SerializeField] private int _tilesPerStep;

        public int TilesCount => _tilesCount;

        public int TilesPerStep => _tilesPerStep;

        public string TileMask => _tileMask;

        public Tile[] Tiles { get; set; }

        public abstract void PlaceOnTile(IEnumerable<Tile> tiles);


        //public void Move() => StartCoroutine(MoveToNextNode());

        public IEnumerator MoveToNextNode(List<Tile> tiles)
        {
            List<Tile> lowerTiles = tiles.OrderBy(tile => tile.Position.y).ToList();

            float center = (float) (lowerTiles[0].Position.x + lowerTiles[1].Position.x) / 2;

            Vector3 targetPosition = new Vector3(center, 0f, lowerTiles.FirstOrDefault()!.transform.position.z);

            while (Vector3.Distance(transform.position, targetPosition) > 1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, 5f * Time.deltaTime);
                yield return null;
            }
        }
    }
}