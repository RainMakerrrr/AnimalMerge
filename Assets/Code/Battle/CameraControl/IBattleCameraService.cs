using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.Battle.CameraControl
{
    public interface IBattleCameraService
    {
        UniTask ZoomInAsync(CancellationToken cancellationToken);
        UniTask ZoomOutAsync(CancellationToken cancellationToken);
    }
}
