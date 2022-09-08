using System;
using System.Collections.Generic;
using System.Linq;
using Code.Logic.Boards;
using DG.Tweening;
using Pathfinding;
using UnityEngine;

namespace Code.Logic.Animals
{
    public abstract class Animal : MonoBehaviour
    {
        [SerializeField] private int _tilesCount;
        [SerializeField] private int _tilesPerStep;

        public int TilesCount => _tilesCount;

        public int TilesPerStep => _tilesPerStep;

        public Tile[] Tiles { get; set; }

        public abstract void PlaceOnTile(IEnumerable<Tile> tiles);
        


        [ContextMenu("Move Forward")]
        private void MoveForward()
        {
            int i = 0;
            Tile current = Tiles.FirstOrDefault();
            Tile next = current.Next;

            current.ReleaseAnimal();

            while (i != _tilesPerStep)
            {
                next = current.Next;
                current = next;

                i++;
            }

            transform.DOMove(next.transform.position, 1f);

            Tiles.FirstOrDefault().AssignAnimal(this);
        }
    }
}