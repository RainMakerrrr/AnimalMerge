namespace Code.Abilities
{
    public readonly struct AbilityDescription
    {
        public AbilityDescription(AbilityKind kind, int chancePercent)
        {
            Kind = kind;
            ChancePercent = chancePercent;
        }

        public AbilityKind Kind { get; }

        public int ChancePercent { get; }
    }
}
