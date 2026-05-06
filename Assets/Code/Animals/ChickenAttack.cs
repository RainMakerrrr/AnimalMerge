using Code.Animals.Health;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Code.Animals
{
    /// <summary>
    /// Chicken-specific attack implementation with jump mechanic.
    /// The chicken jumps towards the enemy, deals damage at the peak, and lands near original position.
    /// </summary>
    public class ChickenAttack : AnimalAttack
    {
        [Header("Chicken Jump Settings")]
        [SerializeField] private float _jumpDuration = 0.5f;
        [SerializeField] private float _jumpPower = 2f;
        [SerializeField] private int _numJumps = 1;
        [SerializeField] private float _jumpDistance = 1.5f;

        private bool _isJumping;

        /// <summary>
        /// Override to attack without target (physics-based detection).
        /// Not used by AutoFight, but kept for compatibility.
        /// </summary>
        public override async UniTask Attack()
        {
            if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;
            if (_isJumping) return;

            Debug.Log("[ChickenAttack] Attack() without target - using physics detection");

            var target = FindClosestTargetPhysics();
            if (target != null)
            {
                await PerformJumpAttack(target);
            }
            else
            {
                Debug.LogWarning("[ChickenAttack] No target found via physics");
            }
        }

        /// <summary>
        /// Override to attack specific target.
        /// Used by AutoFight system.
        /// </summary>
        public override async UniTask Attack(ITarget target)
        {
            if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;
            if (_isJumping) return;

            if (target == null || target.Damageable == null)
            {
                Debug.LogWarning("[ChickenAttack] Attack(ITarget) called with null target");
                return;
            }

            Debug.Log($"[ChickenAttack] Attack(ITarget) with target: {target.Damageable}");
            await PerformJumpAttack(target);
        }

        /// <summary>
        /// Performs the jump attack sequence: jump to target -> deal damage -> jump back to original position.
        /// Jump and damage execution happen in parallel for better performance.
        /// </summary>
        private async UniTask PerformJumpAttack(ITarget target)
        {
            _isJumping = true;
            var originalPosition = transform.position;

            try
            {
                // Calculate jump target position (near the enemy)
                var directionToTarget = (target.Transformable.Position - transform.position).normalized;
                var jumpTarget = transform.position + directionToTarget * _jumpDistance;

                Debug.Log($"[ChickenAttack] Jumping from {transform.position} to {jumpTarget}");

                _animator.SetEnableFlappingAnimation(true);

                var halfDuration = _jumpDuration * 0.5f;

                // Create parallel tasks: jump sequence (synchronous) and damage sequence
                var jumpTask = PerformJumpSequence(jumpTarget, originalPosition);
                var damageTask = PerformDamageSequence(target, halfDuration);

                // Execute both tasks in parallel
                await UniTask.WhenAll(jumpTask, damageTask);

                _animator.SetEnableFlappingAnimation(false);

                Debug.Log("[ChickenAttack] Jump attack completed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChickenAttack] Error during jump attack: {e.Message}");
            }
            finally
            {
                _isJumping = false;
            }
        }

        /// <summary>
        /// Performs the jump sequence: jump to target, then jump back to original position.
        /// This is executed synchronously (await each jump).
        /// </summary>
        private async UniTask PerformJumpSequence(Vector3 jumpTarget, Vector3 originalPosition)
        {
            // Jump to target
            await transform.DOJump(
                endValue: jumpTarget,
                jumpPower: _jumpPower,
                numJumps: _numJumps,
                duration: _jumpDuration
            ).AsyncWaitForCompletion().AsUniTask();

            Debug.Log("[ChickenAttack] Jumping back to original position");

            // Jump back to original position
            await transform.DOJump(
                endValue: originalPosition,
                jumpPower: _jumpPower,
                numJumps: _numJumps,
                duration: _jumpDuration
            ).AsyncWaitForCompletion().AsUniTask();
        }

        /// <summary>
        /// Performs the damage sequence: wait for half duration (peak of jump), then deal damage.
        /// This is executed in parallel with the jump sequence.
        /// </summary>
        private async UniTask PerformDamageSequence(ITarget target, float delaySeconds)
        {
            // Wait for half duration (peak of the jump)
            await UniTask.Delay((int)(delaySeconds * 1000));

            Debug.Log($"[ChickenAttack] Dealing damage to {target}");

            // Deal damage
            await target.Damageable.TakeDamageAsync(this);
        }

        /// <summary>
        /// Finds the closest target using physics detection.
        /// Used when Attack() is called without a specific target.
        /// </summary>
        private ITarget FindClosestTargetPhysics()
        {
            var colliders = new Collider[MaxTargets];
            Vector3 a = AttackPoint.position;
            Vector3 b = AttackPoint.position + transform.forward * (ForwardReach + Radius);

            int count = PhysicsService != null
                ? PhysicsService.OverlapCapsuleNonAlloc(a, b, Radius, colliders, AttackMask)
                : Physics.OverlapCapsuleNonAlloc(a, b, Radius, colliders, AttackMask);

            if (count <= 0)
            {
                // Fallback to sphere
                Vector3 center = AttackPoint.position + transform.forward * ForwardReach;
                count = PhysicsService != null
                    ? PhysicsService.OverlapSphereNonAlloc(center, Radius, colliders, AttackMask)
                    : Physics.OverlapSphereNonAlloc(center, Radius, colliders, AttackMask);
            }

            if (count <= 0) return null;

            ITarget closestTarget = null;
            var minDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = colliders[i];
                if (col == null) continue;

                var target = col.GetComponentInParent<ITarget>();
                if (target == null) continue;
                
                var distance = Vector3.Distance(transform.position, target.Transformable.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }
    }
}
