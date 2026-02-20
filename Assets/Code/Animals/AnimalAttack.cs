using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Health;
using Code.Services.Physics;
using UnityEngine;
using Zenject;

namespace Code.Animals
{
    public class AnimalAttack : MonoBehaviour, IAttacker
    {
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private float _radius;
        [SerializeField] private float _forwardReach = 0.25f;
        [SerializeField] private float _damage;
        [SerializeField] private int _maxTargets;
        [SerializeField] private LayerMask _mask;
        [SerializeField] private bool _isAoE;

        private Collider[] _colliders;
        private IPhysicsService _physicsService;
        private IDamageable _damageable;
        private ITarget _targetOverride; // Specific target set via Attack(ITarget)

        public float Damage => _damage;
        public bool IsAoE => _isAoE;
        public IDamageable Damageable
        {
            get
            {
                // Lazy initialization for tests where Start() might not be called
                if (_damageable == null)
                {
                    // Search in this order: self -> parent -> children
                    _damageable = GetComponent<IDamageable>();
                    if (_damageable == null)
                    {
                        _damageable = GetComponentInParent<IDamageable>();
                    }
                    if (_damageable == null)
                    {
                        _damageable = GetComponentInChildren<IDamageable>();
                    }
                }
                return _damageable;
            }
        }

        [Inject]
        private void Construct(IPhysicsService physicsService)
        {
            _physicsService = physicsService;
        }

        private void Start()
        {
            _colliders = new Collider[_maxTargets];

            // Search in this order: self -> parent -> children
            _damageable = GetComponent<IDamageable>();
            if (_damageable == null)
            {
                _damageable = GetComponentInParent<IDamageable>();
            }
            if (_damageable == null)
            {
                _damageable = GetComponentInChildren<IDamageable>();
            }

            if (_damageable == null)
            {
                Debug.LogWarning($"[AnimalAttack] {name} has no IDamageable component in hierarchy. CounterAttack will not work against this attacker.");
            }
        }

        public void Upgrade(float multiplier) => _damage *= multiplier;

        public void SetDamage(float newDamage)
        {
            _damage = newDamage;
        }

        public void SetIsAoE(bool isAoE)
        {
            _isAoE = isAoE;
        }

        public async Task Attack()
        {
            if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;

            // Clear target override for generic attack
            _targetOverride = null;
            await _animator.WaitForAttackAnimation();
        }

        /// <summary>
        /// Attacks a specific target. This avoids physics search and directly attacks the provided target.
        /// Used by AutoFight to ensure we attack the same target we're moving towards.
        /// </summary>
        public async Task Attack(ITarget target)
        {
            if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;

            // Set target override for AttackAnimationHandlerAsync
            _targetOverride = target;
            await _animator.WaitForAttackAnimation();
            _targetOverride = null; // Clear after attack
        }

        private void OnDrawGizmos()
        {
            if (_attackPoint == null) return;
            Gizmos.color = Color.red;
            Vector3 center = _attackPoint.position + transform.forward * _forwardReach;
            Gizmos.DrawWireSphere(center, _radius);
        }

        // Public method for Animation Events (must be void)
        public async void AttackAnimationHandler()
        {
            await AttackAnimationHandlerAsync();
        }

        // Internal async method for testing (returns Task)
        public async Task AttackAnimationHandlerAsync()
        {
            var animal = GetComponent<Animal>();
            if (animal != null && animal.Type == AnimalType.Hedgehog) return;
            if (_attackPoint == null) return;

            // If specific target was set via Attack(ITarget), attack it directly
            if (_targetOverride != null)
            {
                Debug.Log($"[Attack] Using target override: {_targetOverride.Damageable}");
                await _targetOverride.Damageable.TakeDamageAsync(this);
                return;
            }

            // Otherwise, perform physics-based target detection
            if (_colliders == null || _colliders.Length != _maxTargets)
                _colliders = new Collider[_maxTargets];

            Vector3 a = _attackPoint.position;
            Vector3 b = _attackPoint.position + transform.forward * (_forwardReach + _radius);

            // Fallback to direct Physics if service not injected (backward compatibility for tests)
            int count = _physicsService != null
                ? _physicsService.OverlapCapsuleNonAlloc(a, b, _radius, _colliders, _mask)
                : Physics.OverlapCapsuleNonAlloc(a, b, _radius, _colliders, _mask);

            if (count <= 0)
            {
                // Fallback to a simple sphere centered slightly forward
                Vector3 center = _attackPoint.position + transform.forward * _forwardReach;
                count = _physicsService != null
                    ? _physicsService.OverlapSphereNonAlloc(center, _radius, _colliders, _mask)
                    : Physics.OverlapSphereNonAlloc(center, _radius, _colliders, _mask);
                if (count <= 0) return;
            }

            // FIX: Используем HashSet для дедупликации IDamageable экземпляров
            var damagedTargets = new HashSet<IDamageable>();

            for (int i = 0; i < count; i++)
            {
                Collider col = _colliders[i];
                if (col == null) continue;

                var health = col.GetComponentInParent<IDamageable>();
                if (health == null) continue;

                Debug.Log($"[Attack] Found collider: {col.name}, IDamageable: {health}");
                damagedTargets.Add(health);  // HashSet автоматически игнорирует дубликаты
            }

            Debug.Log($"[Attack] Total colliders found: {count}, Unique targets: {damagedTargets.Count}");

            // Если не AoE - атакуем только ближайшую цель
            if (!_isAoE && damagedTargets.Count > 0)
            {
                var closestTarget = damagedTargets
                    .OrderBy(target =>
                    {
                        var targetMono = target as MonoBehaviour;
                        if (targetMono == null) return float.MaxValue;
                        return Vector3.Distance(transform.position, targetMono.transform.position);
                    })
                    .First();

                Debug.Log($"[Attack] Single-target: Attacking closest target {closestTarget}");
                await closestTarget.TakeDamageAsync(this);
                return;
            }

            // Если AoE - атакуем всех найденных целей
            Debug.Log($"[Attack] AoE: Attacking {damagedTargets.Count} targets");
            foreach (var health in damagedTargets)
            {
                Debug.Log($"[Attack] Applying damage to: {health}");
                await health.TakeDamageAsync(this);
            }
        }

        private Collider GetClosestCollider() =>
            _colliders.Where(c => c.GetComponentInParent<IDamageable>() != null)
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).First();
    }
}