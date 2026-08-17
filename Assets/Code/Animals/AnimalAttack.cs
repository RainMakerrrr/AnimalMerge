using System;
using System.Collections.Generic;
using System.Linq;
using Code.Abilities;
using Code.GridPathfinding;
using Cysharp.Threading.Tasks;
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
        protected float _damage;
        [SerializeField] protected int _maxTargets;
        [SerializeField] protected LayerMask _mask;
        [SerializeField] protected bool _isAoE;
        [SerializeField] private Color _aoeHighlightColor = new Color(1f, 0.5f, 0f, 0.4f); // Alpha = cell fill opacity

        protected Collider[] _colliders;
        protected IPhysicsService _physicsService;
        protected IGridManager _gridManager;
        protected IDamageable _damageable;
        protected ITarget _targetOverride; // Specific target set via Attack(ITarget)
        private AbilityManager _abilityManager;

        private bool _isAttackDone;
        private readonly List<IGridCell> _highlightedCells = new List<IGridCell>();
        private Vector3 _lastAttackOrigin = Vector3.positiveInfinity;
        private Vector3 _lastForward = Vector3.positiveInfinity;
        protected AnimalType _animalType;

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
        private void Construct(
            IPhysicsService physicsService,
            IGridManager gridManager)
        {
            _physicsService = physicsService;
            _gridManager = gridManager;
        }

        /// <summary>
        /// Injects AbilityManager for post-attack ability execution.
        /// Called by AnimalFacade.Awake() after AbilityManager is created.
        /// </summary>
        public void Construct(AbilityManager abilityManager, AnimalType animalType)
        {
            _abilityManager = abilityManager;
            _animalType = animalType;
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
                Debug.LogWarning(
                    $"[AnimalAttack] {name} has no IDamageable component in hierarchy. CounterAttack will not work against this attacker.");
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

        public virtual async UniTask Attack()
        {
            if (_animalType == AnimalType.Hedgehog) return;

            // Clear target override for generic attack
            _targetOverride = null;
            await _animator.WaitForAttackAnimation();
        }

        /// <summary>
        /// Attacks a specific target. This avoids physics search and directly attacks the provided target.
        /// Used by AutoFight to ensure we attack the same target we're moving towards.
        /// </summary>
        public virtual async UniTask Attack(ITarget target)
        {
            if (_animalType == AnimalType.Hedgehog) return;

            // Set target override for AttackAnimationHandlerAsync
            _targetOverride = target;
            Debug.Log($"[AttackingDebug] Target for {name} is {target.Transformable.CurrentPathNode.GridPosition}");
            _animator.PlayAttackAnimation();

            while (!_isAttackDone)
            {
                await UniTask.Yield();
            }

            _isAttackDone = false;
        }

        private void OnDrawGizmos()
        {
            if (_attackPoint == null) return;

            Gizmos.color = Color.red;
            Vector3 center = _attackPoint.position + transform.forward * _forwardReach;
            Gizmos.DrawWireSphere(center, _radius);

            if (!_isAoE) return;

            // AOE: full capsule from AttackAnimationHandlerAsync
            var a = _attackPoint.position;
            var b = _attackPoint.position + transform.forward * (_forwardReach + _radius);

            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawSphere(a, _radius);
            Gizmos.DrawSphere(b, _radius);

            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            Gizmos.DrawWireSphere(a, _radius);
            Gizmos.DrawWireSphere(b, _radius);

            var right = transform.right * _radius;
            var up = transform.up * _radius;
            Gizmos.DrawLine(a + right, b + right);
            Gizmos.DrawLine(a - right, b - right);
            Gizmos.DrawLine(a + up, b + up);
            Gizmos.DrawLine(a - up, b - up);

            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(b, _radius);
        }

        // Public method for Animation Events (must be void)
        public void AttackAnimationHandler()
        {
            AttackAnimationHandlerAsync().Forget();
        }

        // Internal async method for testing
        // Virtual to allow derived classes to override attack behavior
        public virtual async UniTask AttackAnimationHandlerAsync()
        {
            if (_animalType == AnimalType.Hedgehog) return;
            if (_attackPoint == null) return;

            // FIX: Capture target IMMEDIATELY to avoid race condition
            // _targetOverride can be cleared by Attack(ITarget) before this method executes
            var targetSnapshot = _targetOverride;

            // If specific target was set via Attack(ITarget), attack it directly
            if (targetSnapshot != null && !_isAoE)
            {
                Debug.Log($"[Attack] Using target override: {targetSnapshot.Damageable}");
                await targetSnapshot.Damageable.TakeDamageAsync(this);

                // Execute post-attack abilities if AbilityManager exists
                await ExecutePostAttackAbilitiesAsync(targetSnapshot);
                _isAttackDone = true;

                return;
            }

            // Otherwise, perform physics-based target detection
            if (_colliders == null || _colliders.Length != _maxTargets)
                _colliders = new Collider[_maxTargets];

            var a = _attackPoint.position;
            var b = _attackPoint.position + transform.forward * (_forwardReach + _radius);

            // Fallback to direct Physics if service not injected (backward compatibility for tests)
            var count = _physicsService != null
                ? _physicsService.OverlapCapsuleNonAlloc(a, b, _radius, _colliders, _mask)
                : Physics.OverlapCapsuleNonAlloc(a, b, _radius, _colliders, _mask);

            if (count <= 0)
            {
                // Fallback to a simple sphere centered slightly forward
                var center = _attackPoint.position + transform.forward * _forwardReach;
                count = _physicsService != null
                    ? _physicsService.OverlapSphereNonAlloc(center, _radius, _colliders, _mask)
                    : Physics.OverlapSphereNonAlloc(center, _radius, _colliders, _mask);
                if (count <= 0) return;
            }

            // FIX: Используем HashSet для дедупликации IDamageable экземпляров
            var damagedTargets = new HashSet<IDamageable>();

            for (var i = 0; i < count; i++)
            {
                var col = _colliders[i];
                if (col == null) continue;

                var health = col.GetComponentInParent<IDamageable>();
                if (health == null) continue;

                Debug.Log($"[Attack] Found collider: {col.name}, IDamageable: {health}");
                damagedTargets.Add(health); // HashSet автоматически игнорирует дубликаты
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
                _isAttackDone = true;
                return;
            }

            // Если AoE - атакуем всех найденных целей
            Debug.Log($"[Attack] AoE: Attacking {damagedTargets.Count} targets");
            foreach (var health in damagedTargets)
            {
                Debug.Log($"[Attack] Applying damage to: {health}");
                await health.TakeDamageAsync(this);
            }

            _isAttackDone = true;
        }

        private void LateUpdate()
        {
            // Temporarily disabled — use Gizmos to verify attack radius first
            // if (!_isAoE || _gridManager == null || _attackPoint == null) return;
            // Vector3 currentOrigin = _attackPoint.position;
            // Vector3 currentForward = transform.forward;
            // if (currentOrigin == _lastAttackOrigin && currentForward == _lastForward) return;
            // _lastAttackOrigin = currentOrigin;
            // _lastForward = currentForward;
            // HighlightAoeCells();
        }

        private void OnDisable()
        {
            ClearAoeHighlight();
        }

        private void HighlightAoeCells()
        {
            if (!_isAoE || _gridManager == null || _attackPoint == null) return;

            ClearAoeHighlight();
            var a = _attackPoint.position;
            var b = a + transform.forward * (_forwardReach + _radius);

            for (var x = 0; x < _gridManager.Width; x++)
            for (var y = 0; y < _gridManager.Height; y++)
            {
                var cell = _gridManager.GetCell(x, y);
                if (cell != null && IsPointInCapsule(cell.WorldPosition, a, b, _radius))
                {
                    Debug.Log($"[AOE] Highlighting cell {cell}");
                    cell.SetColor(_aoeHighlightColor);
                    _highlightedCells.Add(cell);
                }
            }
        }

        private void ClearAoeHighlight()
        {
            foreach (var cell in _highlightedCells)
                cell.UpdateVisual();
            _highlightedCells.Clear();
        }

        private static bool IsPointInCapsule(Vector3 point, Vector3 a, Vector3 b, float radius)
        {
            // Flatten to XZ plane — cells are at y=0, attack point may be elevated
            point.y = 0f;
            a.y = 0f;
            b.y = 0f;
            var ab = b - a;
            var lenSq = ab.sqrMagnitude;
            var t = lenSq > 0f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / lenSq) : 0f;
            return (point - (a + t * ab)).sqrMagnitude <= radius * radius;
        }

        private Collider GetClosestCollider() =>
            _colliders.Where(c => c.GetComponentInParent<IDamageable>() != null)
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).First();

        /// <summary>
        /// Executes post-attack abilities (like Retreat) after successful attack.
        /// Only executes IPostAttackAbility, not defensive abilities (Dodge, CounterAttack).
        /// </summary>
        protected virtual async UniTask ExecutePostAttackAbilitiesAsync(ITarget target)
        {
            if (_abilityManager == null)
            {
                return;
            }

            // Set attack target for all post-attack abilities
            foreach (var ability in _abilityManager.Abilities)
            {
                if (ability is IPostAttackAbility postAttackAbility)
                {
                    postAttackAbility.SetAttackTarget(target);
                }
            }

            // Create context for post-attack abilities
            var context = new AbilityContext(
                attacker: this, // Self (attacker)
                target: target.Damageable, // Who we attacked
                damage: _damage
            );

            // Execute only post-attack abilities using AbilityManager
            await _abilityManager.ExecuteAbilitiesOfTypeAsync<IPostAttackAbility>(context);
        }
    }
}