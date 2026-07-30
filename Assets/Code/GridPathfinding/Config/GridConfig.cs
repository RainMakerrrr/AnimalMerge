using UnityEngine;

namespace Code.GridPathfinding.Config
{
    /// <summary>
    /// Serialized configuration of a single grid's dimensions.
    /// Assigned to a GridManager in the inspector so that MergeGrid and GameGrid
    /// can be resized from assets instead of per-scene inspector values.
    /// </summary>
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Game/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        public const int DefaultWidth = 8;
        public const int DefaultHeight = 10;
        public const float DefaultCellSize = 1f;

        /// <summary>
        /// The deployment zone is two rows deep (GridCell.CanPlace accepts Y == 0 and Y == 1),
        /// so a grid shorter than two rows cannot hold a legal placement. MergeGrid, the
        /// smallest grid in the game, is exactly this tall.
        /// </summary>
        public const int MinHeight = 2;

        [SerializeField, Min(1)] private int _width = DefaultWidth;
        [SerializeField, Min(MinHeight)] private int _height = DefaultHeight;
        [SerializeField, Min(0.01f)] private float _cellSize = DefaultCellSize;

        public int Width => _width;

        /// <summary>
        /// Number of rows. Never below <see cref="MinHeight"/>: the deployment zone spans
        /// rows 0 and 1 (see GridCell.CanPlace), so a one-row grid would report placement
        /// positions on row 1 that resolve to no cell at all.
        /// </summary>
        public int Height => _height;
        public float CellSize => _cellSize;
    }
}
