using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.Battle.Input
{
    public class PlayerInputLock : IPlayerInputLock
    {
        private int _holders;

        public bool IsLocked => _holders > 0;

        public event Action LockChanged;

        public IDisposable Acquire()
        {
            _holders++;

            if (_holders == 1)
                LockChanged?.Invoke();

            return new PlayerInputLockHandle(Release);
        }

        public async UniTask<bool> WaitUntilUnlockedAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (!IsLocked)
                return true;

            var unlocked = new UniTaskCompletionSource();

            Action onLockChanged = () =>
            {
                if (!IsLocked)
                    unlocked.TrySetResult();
            };

            LockChanged += onLockChanged;

            try
            {
                var isCanceled = await unlocked.Task
                    .AttachExternalCancellation(cancellationToken)
                    .SuppressCancellationThrow();

                return !isCanceled;
            }
            finally
            {
                LockChanged -= onLockChanged;
            }
        }

        private void Release()
        {
            if (_holders == 0)
                return;

            _holders--;

            if (_holders == 0)
                LockChanged?.Invoke();
        }
    }
}
