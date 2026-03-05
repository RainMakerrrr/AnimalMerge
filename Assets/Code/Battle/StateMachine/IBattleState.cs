using System.Threading.Tasks;

namespace Code.Battle.StateMachine
{
    public interface IBattleState
    {
        Task Enter();
        Task Exit();
    }
}
