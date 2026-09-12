using Cysharp.Threading.Tasks;
using Code.Animals;
using Code.Data.Animals;
using Code.Services.Random;
using UnityEngine;

namespace Code.Abilities
{
    /// <summary>
    /// Retreat ability: unit retreats 1-4 cells backward after attacking.
    /// Used by Velociraptor boss.
    /// </summary>
    public class RetreatAbility : IPostAttackAbility, IDescribableAbility
    {
        private const int MaxChancePercent = 100;

        private readonly ITransformable _transformable;
        private readonly IRandomProvider _randomProvider;
        private readonly int _minDistance;
        private readonly int _maxDistance;
        private readonly int _successChance;

        // Attack target set by AnimalAttack before execution
        private ITarget _attackTarget;

        public bool IsBlockingDamage => false;  // Does NOT block damage
        public int Priority => -1;              // Executes after all other abilities

        public RetreatAbility(
            ITransformable transformable,
            IRandomProvider randomProvider,
            int minDistance = 1,
            int maxDistance = 4,
            bool isOwner = true,
            int? successChance = null)
        {
            _transformable = transformable;
            _randomProvider = randomProvider;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _successChance = successChance ?? DefaultChanceFor(isOwner);
        }

        public void SetAttackTarget(ITarget target)
        {
            _attackTarget = target;
        }

        public bool CanUse(IAttacker attacker) =>
            _randomProvider.Range(0, MaxChancePercent) < _successChance;

        public AbilityDescription Describe() =>
            new AbilityDescription(AbilityKind.Retreat, _successChance);

        private static int DefaultChanceFor(bool isOwner) =>
            isOwner
                ? VelociraptorStats.DefaultOwnerRetreatChance
                : VelociraptorStats.DefaultInheritedRetreatChance;

        public async UniTask Apply()
        {
            if (_attackTarget == null)
            {
                Debug.LogWarning("[RetreatAbility] Attack target is null, cannot retreat");
                return;
            }

            // Random retreat distance: 1-4 cells
            var retreatDistance = _randomProvider.Range(_minDistance, _maxDistance + 1);

            // Get target position for retreat calculation
            var targetPosition = _attackTarget.Transformable.CurrentPathNode.GridPosition;

            Debug.Log($"[RetreatAbility] Retreating {retreatDistance} cells from {targetPosition}");

            // Execute retreat using existing movement system
            await _transformable.RetreatFrom(targetPosition, retreatDistance);
        }
    }
}
