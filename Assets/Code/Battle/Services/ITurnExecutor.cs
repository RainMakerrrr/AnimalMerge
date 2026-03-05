using System.Threading.Tasks;

namespace Code.Battle.Services
{
    public interface ITurnExecutor
    {
        Task ExecutePlayerTurnsAsync();
        Task ExecuteEnemyTurnsAsync();
    }
}
