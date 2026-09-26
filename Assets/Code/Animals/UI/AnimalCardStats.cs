namespace Code.Animals.UI
{
    public readonly struct AnimalCardStats
    {
        public AnimalCardStats(int attack, int health, int tilesPerMove, int healthBonusPercent)
        {
            Attack = attack;
            Health = health;
            TilesPerMove = tilesPerMove;
            HealthBonusPercent = healthBonusPercent;
        }

        public int Attack { get; }
        public int Health { get; }
        public int TilesPerMove { get; }
        public int HealthBonusPercent { get; }
    }
}
