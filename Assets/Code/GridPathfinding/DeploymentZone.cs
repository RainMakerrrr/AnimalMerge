using System.Collections.Generic;

namespace Code.GridPathfinding
{
    public static class DeploymentZone
    {
        public const int Depth = 2;

        public static bool Contains(int y)
        {
            return y >= 0 && y < Depth;
        }

        public static bool ContainsAll(IReadOnlyList<IGridCell> cells)
        {
            if (cells == null)
                return false;

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == null || !Contains(cells[i].Y))
                    return false;
            }

            return true;
        }
    }
}
