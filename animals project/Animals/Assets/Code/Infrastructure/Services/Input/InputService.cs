using Lean.Touch;
using UnityEngine;

namespace Code.Infrastructure.Services.Input
{
    public class InputService : IInputService
    {
        public Vector2 PointerPosition { get; private set; }
        public bool IsPressed { get; private set; }
        
        public InputService()
        {
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUpdate += OnFingerUpdate;
            LeanTouch.OnFingerUp += OnFingerUp;
        }

        private void OnFingerDown(LeanFinger finger)
        {
            if(finger.IsOverGui) return;

            IsPressed = true;
            PointerPosition = finger.ScreenPosition;
        }

        private void OnFingerUpdate(LeanFinger finger)
        {
            PointerPosition = finger.ScreenPosition;
        }

        private void OnFingerUp(LeanFinger finger)
        {
            IsPressed = false;
        }

        ~InputService()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
            LeanTouch.OnFingerUpdate -= OnFingerUpdate;
            LeanTouch.OnFingerUp -= OnFingerUp;
        }
    }
}