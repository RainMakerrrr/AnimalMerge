using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.Tutorial
{
    public interface ITutorialStep
    {
        string Id { get; }

        bool CanRun { get; }

        UniTask<bool> RunAsync(CancellationToken cancellationToken);
    }
}
