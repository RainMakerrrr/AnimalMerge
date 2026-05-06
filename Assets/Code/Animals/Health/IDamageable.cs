using Cysharp.Threading.Tasks;

namespace Code.Animals.Health
{
    public interface IDamageable
    {
        float Current { get; }
        float Max { get; }
        bool IsDead { get; }
        UniTask TakeDamageAsync(AnimalAttack attacker);
    }
}