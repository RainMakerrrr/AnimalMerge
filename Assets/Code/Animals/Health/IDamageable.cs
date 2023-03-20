namespace Code.Animals.Health
{
    public interface IDamageable
    {
        float Current { get; }
        float Max { get; }
        void TakeDamage(float damage);
    }
}