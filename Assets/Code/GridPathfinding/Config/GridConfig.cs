using UnityEngine;

namespace Code.GridPathfinding.Config
{
    /// <summary>
    /// Serialized configuration of a single grid's dimensions.
    /// Assigned to a GridManager in the inspector so that a grid can be resized
    /// from an asset instead of per-scene inspector values.
    /// </summary>
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Game/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        public const int DefaultWidth = 8;
        public const int DefaultHeight = 10;
        public const float DefaultCellSize = 1f;

        /// <summary>
        /// The deployment zone spans <see cref="DeploymentZone.Depth"/> rows at the bottom of
        /// the grid, so a shorter grid cannot hold a legal placement.
        /// </summary>
        public const int MinHeight = DeploymentZone.Depth;

        [SerializeField, Min(1)] private int _width = DefaultWidth;
        [SerializeField, Min(MinHeight)] private int _height = DefaultHeight;
        [SerializeField, Min(0.01f)] private float _cellSize = DefaultCellSize;

        public int Width => _width;

        /// <summary>
        /// Number of rows. Never below <see cref="MinHeight"/>: a shorter grid would report
        /// placement positions inside the deployment zone that resolve to no cell at all.
        /// </summary>
        public int Height => _height;
        public float CellSize => _cellSize;
    }
}
