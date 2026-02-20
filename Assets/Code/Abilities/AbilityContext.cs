using Code.Animals.Health;

namespace Code.Abilities
{
    /// <summary>
    /// Context data passed to abilities during execution.
    /// Contains all information needed for an ability to make decisions and perform actions.
    /// </summary>
    public class AbilityContext
    {
        /// <summary>
        /// The attacker that initiated the damage.
        /// </summary>
        public IAttacker Attacker { get; set; }

        /// <summary>
        /// The target receiving the damage.
        /// </summary>
        public IDamageable Target { get; set; }

        /// <summary>
        /// The amount of damage being dealt.
        /// </summary>
        public float Damage { get; set; }

        /// <summary>
        /// Indicates whether the damage was blocked by an ability.
        /// When true, the damage should not be applied to the target.
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Creates a new ability context.
        /// </summary>
        public AbilityContext()
        {
            IsBlocked = false;
        }

        /// <summary>
        /// Creates a new ability context with specified values.
        /// </summary>
        /// <param name="attacker">The attacking animal (IAttacker abstraction).</param>
        /// <param name="target">The target being damaged.</param>
        /// <param name="damage">The damage amount.</param>
        public AbilityContext(IAttacker attacker, IDamageable target, float damage)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            IsBlocked = false;
        }
    }
}
