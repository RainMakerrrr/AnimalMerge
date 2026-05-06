using Cysharp.Threading.Tasks;

namespace Code.Battle.StateMachine
{
    public interface IBattleState
    {
        UniTask Enter();
        UniTask Exit();
    }
}
