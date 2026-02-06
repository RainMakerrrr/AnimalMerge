using System.Threading.Tasks;
using Code.Animals;

namespace Code.Abilities
{
    public interface IAbility
    {
        bool IsBlockingDamage { get; }
        int Priority { get; }
        bool CanUse(AnimalAttack attacker);
        Task Apply();
    }
}