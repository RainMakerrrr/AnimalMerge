using UnityEngine;

namespace Code.Infrastructure.Services.Input
{
    public interface IInputService
    {
        Vector3 MousePosition { get; }
        bool IsMouseDown { get; }
        bool IsMouseDrag { get; }
        bool IsMouseUp { get; }
    }
}