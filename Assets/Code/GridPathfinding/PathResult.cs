using System.Collections.Generic;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Value object containing the result of a pathfinding request
    /// </summary>
    public class PathResult
    {
        public bool Success { get; }
        public List<Vector2Int> Path { get; }
        public string ErrorMessage { get; }

        private PathResult(bool success, List<Vector2Int> path, string errorMessage = null)
        {
            Success = success;
            Path = path ?? new List<Vector2Int>();
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a successful path result
        /// </summary>
        public static PathResult CreateSuccess(List<Vector2Int> path)
        {
            return new PathResult(true, path);
        }

        /// <summary>
        /// Creates a failed path result with error message
        /// </summary>
        public static PathResult CreateFailure(string errorMessage)
        {
            return new PathResult(false, null, errorMessage);
        }

        public override string ToString()
        {
            if (Success)
                return $"PathResult: Success, {Path.Count} waypoints";
            else
                return $"PathResult: Failed - {ErrorMessage}";
        }
    }
}
