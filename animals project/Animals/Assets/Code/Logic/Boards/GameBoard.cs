using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Code.Infrastructure.Factories.Tiles;
using UnityEngine;
using Zenject;

namespace Code.Logic.Boards
{
    public class GameBoard : MonoBehaviour
    {
        [SerializeField] private Vector2Int _smallBoardSize;
        [SerializeField] private Vector2Int _mediumBoardSize;
        [SerializeField] private Vector2Int _bigBoardSize;

        [SerializeField] private List<Tile> _tiles = new();

        private ITileFactory _tileFactory;

        public IReadOnlyList<Tile> Tiles => _tiles;

        [Inject]
        private void Construct(ITileFactory tileFactory)
        {
            _tileFactory = tileFactory;
        }

        private IEnumerator Start()
        {
            _tileFactory.Load();

            yield return StartCoroutine(SpawnTiles(_smallBoardSize, TileType.Small, 0f, 0f));
            //yield return StartCoroutine(SpawnTiles(_mediumBoardSize, TileType.Medium, 0f, 0.5f));
            //yield return StartCoroutine(SpawnTiles(_bigBoardSize, TileType.Big, 0.5f, 0.5f));

            _tiles.ForEach(tile =>
            {
                tile.TryFindNeighbours();
                tile.TryFindNode();
            });
        }

        public Tile GetNearest(Vector3 position, int tilesCount, out List<Tile> tileNeighbours)
        {
            List<Tile> freeTiles = _tiles.Where(tile => tile.CanAssignAnimal(tilesCount, out List<Tile> neighbours)).ToList();
            
            Tile closestTile = freeTiles.FirstOrDefault();
            if (closestTile == null)
            {
                tileNeighbours = null;
                return null;
            }
            
            float distance = Vector3.Distance(closestTile.transform.position, position);

            foreach (Tile tile in freeTiles)
            {
                float currentDistance = Vector3.Distance(tile.transform.position, position);
                
                if (currentDistance < distance)
                {
                    distance = currentDistance;
                    closestTile = tile;
                }
            }

            if (closestTile.CanAssignAnimal(tilesCount, out List<Tile> closesNeighbours))
            {
                tileNeighbours = closesNeighbours;
                return closestTile;
            }

            tileNeighbours = null;
            return null;
        }
        
        private IEnumerator SpawnTiles(Vector2Int size, TileType tileType, float xOffset, float zOffset)
        {
            Vector3 position = transform.position;

            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    Tile tile = _tileFactory.Create(tileType);

                    Vector3 tileScale = tile.transform.localScale;

                    Vector3 spawnPosition = new Vector3(i * tileScale.x + position.x + xOffset, 0.01f,
                        j * tileScale.z + position.z + zOffset);

                    tile.transform.SetParent(transform);
                    tile.transform.position = spawnPosition;

                    tile.Position = new Vector2Int((int) spawnPosition.x, (int) spawnPosition.z);
                    tile.name = $"{tile.name} {i}, {j}";
                    _tiles.Add(tile);
                }
            }

            yield break;
        }

        // private void SpawnTiles(Vector2Int size, Tile prefab, float xOffset, float zOffset)
        // {
        //     Vector3 prefabScale = prefab.transform.localScale;
        //
        //     for (int i = 0; i < size.x; i++)
        //     {
        //         for (int j = 0; j < size.y; j++)
        //         {
        //             Vector3 spawnPosition = new Vector3(i * prefabScale.x + transform.position.x + xOffset, 0.01f,
        //                 j * prefabScale.z + transform.position.z + zOffset);
        //
        //             Tile tile = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
        //             tile.Position = new Vector2Int((int) spawnPosition.x, (int) spawnPosition.z);
        //             //tile.TryFindNode(i, j);
        //             tile.name = $"{prefab.name} {i}, {j}";
        //             _tiles.Add(tile);
        //         }
        //     }
        //
        //     _tiles.ForEach(tile => tile.TryFindNeighbours());
        // }
    }
}