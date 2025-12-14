using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.NewTestPathfinding
{
    /// <summary>
    /// Структура для хранения размера юнита на сетке
    /// </summary>
    [System.Serializable]
    public struct UnitSize
    {
        public int Width;  // Ширина по горизонтали (X)
        public int Height; // Высота по вертикали (Y)

        public UnitSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Предустановленные размеры для удобства
        /// </summary>
        public static UnitSize Size1X1 => new UnitSize(1, 1);
        public static UnitSize Size2X1 => new UnitSize(1, 2);
        public static UnitSize Size2X2 => new UnitSize(2, 2);

        /// <summary>
        /// Получить все клетки, которые занимает юнит с заданной базовой позицией
        /// </summary>
        /// <param name="bottomLeft">Нижняя левая позиция юнита</param>
        public List<Vector2Int> GetAllOccupiedCells(Vector2Int bottomLeft)
        {
            var cells = new List<Vector2Int>();
            
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells.Add(new Vector2Int(bottomLeft.x + x, bottomLeft.y + y));
                }
            }
            
            return cells;
        }

        /// <summary>
        /// Получить центральную позицию юнита
        /// </summary>
        public Vector2 GetCenter(Vector2Int bottomLeft)
        {
            return new Vector2(
                bottomLeft.x + Width * 0.5f - 0.5f,
                bottomLeft.y + Height * 0.5f - 0.5f
            );
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}

