using Cysharp.Threading.Tasks;

namespace Code.Abilities
{
    public interface IAbility
    {
        bool IsBlockingDamage { get; }
        int Priority { get; }
        bool CanUse(IAttacker attacker);
        UniTask Apply();
    }
}