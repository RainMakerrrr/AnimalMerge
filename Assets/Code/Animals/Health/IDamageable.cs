namespace Code.Animals.Health
{
    public interface IDamageable
    {
        float Current { get; }
        float Max { get; }
        bool IsDead { get; }
        void TakeDamage(AnimalAttack attacker);
    }

    public interface IAttacker
    {
        IDamageable Target { get; }
    }
}