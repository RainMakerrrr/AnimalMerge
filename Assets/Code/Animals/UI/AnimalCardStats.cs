namespace Code.Animals.UI
{
    public readonly struct AnimalCardStats
    {
        public AnimalCardStats(int attack, int health, int tilesPerMove)
        {
            Attack = attack;
            Health = health;
            TilesPerMove = tilesPerMove;
        }

        public int Attack { get; }
        public int Health { get; }
        public int TilesPerMove { get; }
    }
}
