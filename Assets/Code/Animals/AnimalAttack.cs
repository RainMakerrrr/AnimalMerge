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
        [SerializeField] protected AnimalAnimator _animator;
        [SerializeField] protected Transform _attackPoint;
        [SerializeField] protected float _radius;
        [SerializeField] protected float _forwardReach = 0.25f;
        [SerializeField] protected float _damage;
        [SerializeField] protected int _maxTargets;
        [SerializeField] protected LayerMask _mask;
        [SerializeField] protected bool _isAoE;

        protected Collider[] _colliders;
        protected IPhysicsService _physicsService;
        protected IDamageable _damageable;
        protected ITarget _targetOverride; // Specific target set via Attack(ITarget)
        private AbilityManager _abilityManager;

        public float Damage => _damage;
        public bool IsAoE => _isAoE;

        // Protected properties for derived classes
        protected AnimalAnimator Animator => _animator;
        protected Transform AttackPoint => _attackPoint;
        protected float Radius => _radius;
        protected float ForwardReach => _forwardReach;
        protected int MaxTargets => _maxTargets;
        protected LayerMask AttackMask => _mask;
        protected IPhysicsService PhysicsService => _physicsService;
        protected ITarget TargetOverride
        {
            get => _targetOverride;
            set => _targetOverride = value;
        }

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

        /// <summary>
        /// Injects AbilityManager for post-attack ability execution.
        /// Called by AnimalFacade.Awake() after AbilityManager is created.
        /// </summary>
        public void Construct(AbilityManager abilityManager)
        {
            _abilityManager = abilityManager;
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

        public virtual async Task Attack()
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
        public virtual async Task Attack(ITarget target)
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
        // Virtual to allow derived classes to override attack behavior
        public virtual async Task AttackAnimationHandlerAsync()
        {
            var animal = GetComponent<Animal>();
            if (animal != null && animal.Type == AnimalType.Hedgehog) return;
            if (_attackPoint == null) return;

            // If specific target was set via Attack(ITarget), attack it directly
            if (_targetOverride != null)
            {
                Debug.Log($"[Attack] Using target override: {_targetOverride.Damageable}");
                await _targetOverride.Damageable.TakeDamageAsync(this);

                // Execute post-attack abilities if AbilityManager exists
                await ExecutePostAttackAbilitiesAsync(_targetOverride);
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

        /// <summary>
        /// Executes post-attack abilities (like Retreat) after successful attack.
        /// </summary>
        protected virtual async Task ExecutePostAttackAbilitiesAsync(ITarget target)
        {
            if (_abilityManager == null)
            {
                return;
            }

            // Create context for post-attack abilities
            var context = new AbilityContext(
                attacker: this,              // Self (attacker)
                target: target.Damageable,   // Who we attacked
                damage: _damage
            );

            // Set target for abilities that need it (like RetreatAbility)
            foreach (var ability in _abilityManager.Abilities)
            {
                if (ability is IPostAttackAbility postAttackAbility)
                {
                    postAttackAbility.SetAttackTarget(target);
                }
            }

            Debug.Log($"[AnimalAttack] Executing post-attack abilities for {name}");
            await _abilityManager.ExecuteAbilitiesAsync(context);
        }
    }
}