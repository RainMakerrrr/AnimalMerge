using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.Battle.Input
{
    public interface IPlayerInputLock
    {
        bool IsLocked { get; }

        event Action LockChanged;

        IDisposable Acquire();

        UniTask<bool> WaitUntilUnlockedAsync(CancellationToken cancellationToken);
    }
}
