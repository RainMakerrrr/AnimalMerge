using Cysharp.Threading.Tasks;
using Code.Services.Random;
using UnityEngine;

namespace Code.Abilities
{
    public class Dodge : IAbility
    {
        private readonly ITransformable _transformable;
        private readonly Collider[] _colliders;
        private readonly int _successChance;
        private readonly IRandomProvider _randomProvider;
        private int _counter;
        private bool _blockDamage;

        /// <summary>
        /// Returns true if the last dodge was successful and should block damage.
        /// Updated by Apply() based on Shift() result.
        /// </summary>
        public bool IsBlockingDamage => _blockDamage;
        public int Priority => 1;

        public bool CanUse(IAttacker attacker)
        {
            // Dodge does NOT work against AoE attacks
            if (attacker != null && attacker.IsAoE)
            {
                Debug.Log("[Dodge] Cannot dodge AoE attack");
                return false;
            }

            // First use: always 100%
            if (_counter == 0) return true;

            // Subsequent uses: success chance is configured per animal (e.g. Fox owner/inherited)
            return _randomProvider.Range(0, 100) < _successChance;
        }

        public Dodge(ITransformable transformable, Collider[] colliders, int successChance, IRandomProvider randomProvider)
        {
            _transformable = transformable;
            _colliders = colliders;
            _successChance = successChance;
            _randomProvider = randomProvider;
        }

        public async UniTask Apply()
        {
            // Assume dodge will succeed by default
            _blockDamage = true;

            // Disable colliders before shift (to avoid being hit during teleport)
            foreach (Collider collider in _colliders)
            {
                collider.enabled = false;
            }

            _counter++;

            // Execute shift and check if it was successful
            bool dodgeSuccessful = await _transformable.Shift();

            // If shift failed (no valid dodge positions), don't block damage
            if (!dodgeSuccessful)
            {
                _blockDamage = false;
                Debug.Log("[Dodge] Shift failed (no valid positions) - damage will NOT be blocked");
            }
            else
            {
                Debug.Log("[Dodge] Shift successful - damage blocked");
            }

            // Re-enable colliders after shift completes (whether successful or not)
            foreach (Collider collider in _colliders)
            {
                if (collider != null) // Safety check
                {
                    collider.enabled = true;
                }
            }

            Debug.Log("[Dodge] Colliders re-enabled after shift");
        }
    }
}