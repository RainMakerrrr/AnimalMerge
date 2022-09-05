using UnityEngine;

namespace Code.Infrastructure.Services.Input
{
    public interface IInputService
    {
        Vector2 PointerPosition { get; }
        bool IsPressed { get; }
    }
}