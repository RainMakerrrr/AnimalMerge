using UnityEngine;

namespace Code
{
    public interface ITransformable
    {
        Vector3 Position { get; }
        Vector2Int IntPosition { get; }
    }
}