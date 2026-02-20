using Code.Animals.Health;

namespace Code.Abilities
{
    /// <summary>
    /// Abstraction for an attacker in the ability system.
    /// Exposes only the necessary information for abilities to make decisions.
    /// </summary>
    public interface IAttacker
    {
        /// <summary>
        /// The amount of damage this attacker deals.
        /// </summary>
        float Damage { get; }

        /// <summary>
        /// Whether this attack is an Area of Effect (AoE) attack.
        /// Abilities like Dodge and CounterAttack cannot work against AoE attacks.
        /// </summary>
        bool IsAoE { get; }

        /// <summary>
        /// The IDamageable component of the attacker.
        /// Used by CounterAttack to deal damage back to the attacker.
        /// </summary>
        IDamageable Damageable { get; }
    }
}
