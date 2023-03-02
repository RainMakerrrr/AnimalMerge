using UnityEngine;

namespace Code.Infrastructure.Services.Input
{
    public class InputService : IInputService
    {
        public Vector3 MousePosition => UnityEngine.Input.mousePosition;

        public bool IsMouseDown => UnityEngine.Input.GetMouseButtonDown(0);
        public bool IsMouseDrag => UnityEngine.Input.GetMouseButton(0);

        public bool IsMouseUp => UnityEngine.Input.GetMouseButtonUp(0);
    }
}