namespace Code.GridPathfinding
{
    /// <summary>
    /// Cardinal directions for unit orientation
    /// Affects footprint calculation for non-square units (e.g., 2x1)
    /// </summary>
    public enum Direction
    {
        North,  // Facing forward (positive Z)
        East,   // Facing right (positive X)
        South,  // Facing backward (negative Z)
        West    // Facing left (negative X)
    }
}
