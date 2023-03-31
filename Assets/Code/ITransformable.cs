using System.Threading.Tasks;
using Code.Pathfinding;
using UnityEngine;

namespace Code
{
    public interface ITransformable
    {
        Vector3 Position { get; }
        Vector2Int IntPosition { get; }
        ObjectSizeType ObjectSizeType { get; }
        int SizeEffect { get; }
        PathNode CurrentPathNode { get; }

        Task Shift();
    }
}