using System.Threading;
using Code.Battle.Config;
using Cysharp.Threading.Tasks;

namespace Code.Battle.Services
{
    public interface IStageAnnouncer
    {
        UniTask<bool> AnnounceAsync(StageBannerKind kind, CancellationToken cancellationToken);
    }
}
