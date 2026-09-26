using System;

namespace Code.Battle.Input
{
    public class PlayerInputLockHandle : IDisposable
    {
        private readonly Action _release;

        private bool _released;

        public PlayerInputLockHandle(Action release)
        {
            _release = release;
        }

        public void Dispose()
        {
            if (_released)
                return;

            _released = true;
            _release();
        }
    }
}
