using System.Threading;
using Code.Battle.Config;
using Cysharp.Threading.Tasks;

namespace Code.Battle.UI.StageBanner
{
    public interface IStageBannerView
    {
        UniTask PlayAsync(StageBannerStyle style, CancellationToken cancellationToken);
    }
}
