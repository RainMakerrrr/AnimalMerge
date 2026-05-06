using Cysharp.Threading.Tasks;

namespace Code.Battle.Services
{
    public interface ITurnExecutor
    {
        UniTask ExecutePlayerTurnsAsync();
        UniTask ExecuteEnemyTurnsAsync();
    }
}
