using Code.Animals;

namespace Code.Abilities
{
    /// <summary>
    /// Marker interface for abilities that execute after the attacker deals damage.
    /// Post-attack abilities need access to the attack target (who was attacked).
    /// </summary>
    public interface IPostAttackAbility : IAbility
    {
        /// <summary>
        /// Sets the attack target for this post-attack ability.
        /// Called by AnimalAttack before ExecuteAbilitiesAsync().
        /// </summary>
        /// <param name="target">The target that was attacked.</param>
        void SetAttackTarget(ITarget target);
    }
}
