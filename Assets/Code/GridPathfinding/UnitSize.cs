using System;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Represents the size of a unit on the grid
    /// Width = horizontal cells (X axis), Height = vertical cells (Y axis)
    /// </summary>
    [Serializable]
    public struct UnitSize : IEquatable<UnitSize>
    {
        public int Width;
        public int Height;

        public UnitSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        // Predefined sizes
        public static UnitSize Small => new UnitSize(1, 1);
        public static UnitSize Medium => new UnitSize(1, 2);  // 2 vertical × 1 horizontal
        public static UnitSize Large => new UnitSize(2, 2);

        public bool Equals(UnitSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        public static bool operator ==(UnitSize left, UnitSize right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnitSize left, UnitSize right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }

        /// <summary>
        /// Checks if this unit is rectangular (1×2 or 2×1)
        /// </summary>
        public bool IsRectangular()
        {
            return (Width == 1 && Height == 2) || (Width == 2 && Height == 1);
        }

        /// <summary>
        /// Checks if this unit is square (width == height)
        /// </summary>
        public bool IsSquare()
        {
            return Width == Height;
        }
    }
}
