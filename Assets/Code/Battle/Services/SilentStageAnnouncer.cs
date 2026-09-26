using System.Threading;
using Code.Battle.Config;
using Cysharp.Threading.Tasks;

namespace Code.Battle.Services
{
    public class SilentStageAnnouncer : IStageAnnouncer
    {
        public UniTask<bool> AnnounceAsync(StageBannerKind kind, CancellationToken cancellationToken) =>
            UniTask.FromResult(true);
    }
}
