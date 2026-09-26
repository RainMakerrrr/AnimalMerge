using Code.Animals.Facades;

namespace Code.Animals.Merge
{
    public readonly struct MergeStatValues
    {
        public MergeStatValues(float maxHealth, float currentHealth, float damage, int tilesPerMove)
        {
            MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
            Damage = damage;
            TilesPerMove = tilesPerMove;
        }

        public float MaxHealth { get; }
        public float CurrentHealth { get; }
        public float Damage { get; }
        public int TilesPerMove { get; }

        public static MergeStatValues From(AnimalFacade animal)
        {
            if (animal == null)
                return default;

            var hasHealth = animal.Health != null;

            return new MergeStatValues(
                hasHealth ? animal.GetMaxHealth() : 0f,
                hasHealth ? animal.GetCurrentHealth() : 0f,
                animal.AttackInstance != null ? animal.GetDamage() : 0f,
                animal.Movement != null ? animal.GetTilesPerMove() : 0);
        }
    }
}
