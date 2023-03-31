namespace Code.Abilities
{
    public interface IAbility
    {
        bool IsBlockingDamage { get; }
        int Priority { get; }
        bool CanUse { get; }
        void Apply();
    }
}