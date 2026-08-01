using System;
using System.Collections.Generic;
using Code.Abilities;
using Cysharp.Threading.Tasks;
using Code.Animals.Health;
using Code.Animals.Merge;
using Code.Animals.Merge.MergeAttributes;
using Code.Animals.Merge.MergeSkills;
using Code.Animals.Movement;
using Code.Animals.Upgrade;
using Code.Animals.Vfx;
using Code.Data.Animals;
using Code.Services.Random;
using UnityEngine;
using Zenject;

namespace Code.Animals.Facades
{
    public abstract class PlayerAnimalFacade : AnimalFacade
    {
        [SerializeField] private AnimalUpgrade _upgrade;
        [SerializeField] private MergeView _mergeView;

        private MergeScaleAnimator _scaleAnimator;

        public MergeScaleAnimator ScaleAnimator
        {
            get
            {
                if (_scaleAnimator == null)
                    _scaleAnimator = GetComponent<MergeScaleAnimator>();

                return _scaleAnimator;
            }
        }

        public MergeView MergeView
        {
            get
            {
                if (_mergeView == null)
                {
                    _mergeView = GetComponentInChildren<MergeView>();
                    if (_mergeView == null)
                    {
                        Debug.LogWarning($"[AnimalFacade] MergeView not found on {name}. Visual undo will not work.");
                    }
                }

                return _mergeView;
            }
        }
        
        public IMergeSkill MergeSkill { get; protected set; }

        public List<IMergeSkill> MergeSkills = new List<IMergeSkill>();

        public List<VisualMergeAttribute> AccumulatedVisualAttributes { get; } = new List<VisualMergeAttribute>();
        
        public void UpgradeHealth(float multiplier) => _upgrade.UpgradeHealth(multiplier);
        public void UpgradeDamage(float multiplier) => _upgrade.UpgradeDamage(multiplier);
        public void UpgradeSpeed(int multiplier) => _upgrade.UpgradeSpeed(multiplier);

    }

    public abstract class EnemyAnimalFacade : AnimalFacade
    {
    }

    public abstract class AnimalFacade : MonoBehaviour, ITarget
    {
        [SerializeField] private AnimalType _type;
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private AnimalAttack _attack;
        [SerializeField] private Collider[] _colliders;
        [SerializeField] protected AnimalHealth _health;
        [SerializeField] protected AnimalMovement _movement;

        private UnitOccupancy _occupancy;

        protected IRandomProvider _randomProvider;
        private AbilityManager _abilityManager;

        /// <summary>
        /// Event fired when this animal is about to be removed (merged or destroyed)
        /// </summary>
        public event Action<AnimalFacade> OnRemoved;

        public IDamageable Damageable => _health;
        public ITransformable Transformable => _movement;
        public AnimalType Type => _type;

        public AnimalHealth Health => _health;
        public AnimalAttack AttackInstance => _attack;
        public AnimalMovement Movement => _movement;
        public UnitOccupancy Occupancy => _occupancy;
        public Collider[] Colliders => _colliders;
        public AnimalAnimator Animator => _animator;
        

        public IRandomProvider RandomProvider => _randomProvider;
        public bool IsBoss { get; set; }

        [Inject]
        private void Construct(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
        }


        protected IAbility Ability;

        private ITarget _target;

        private void Start()
        {
            _occupancy = GetComponent<UnitOccupancy>();

            // Initialize AbilityManager - facade owns it, components use it
            _abilityManager = new AbilityManager();

            InitBehaviours();
            _health.Construct(_colliders, _abilityManager);

            // Inject AbilityManager into AnimalAttack for post-attack abilities
            if (_attack != null)
            {
                _attack.Construct(_abilityManager, _type);
            }

            _health.Died += NotifyRemoved;
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log($"[Abilities] Ability log for {name}");
                Debug.Log($"[Abilities] {_abilityManager.Abilities.Count}");

                foreach (var ability in _abilityManager.Abilities)
                {
                    Debug.Log($"[Abilities] Ability {ability.GetType().Name}");
                }

                Debug.Log($"[Abilities] -------------------");
            }
        }

        private void OnDestroy()
        {
            _health.Died -= NotifyRemoved;
        }

        public abstract void InitBehaviours();
        
        /// <summary>
        /// Sets the primary (base) ability for this animal.
        /// Primary abilities are NOT added to MergedAbilities list (they are not inherited from merge).
        /// Use this in InitBehaviours() for the animal's base ability (e.g., Fox Dodge, Velociraptor Retreat).
        /// </summary>
        public void SetPrimaryAbility(IAbility ability)
        {
            if (ability == null)
            {
                Debug.LogWarning("[AnimalFacade] Attempted to set null primary ability");
                return;
            }

            // Register in AbilityManager (owned by facade)
            _abilityManager.RegisterAbility(ability);

            Debug.Log($"[AnimalFacade] Set primary ability: {ability.GetType().Name}");
        }

        /// <summary>
        /// Adds an ability to this animal's AbilityManager.
        /// Also adds it to Health.MergedAbilities list for tracking.
        /// Use this for abilities obtained through merging.
        /// </summary>
        public void AddAbility(IAbility ability)
        {
            if (ability == null)
            {
                Debug.LogWarning("[AnimalFacade] Attempted to add null ability");
                return;
            }

            // Register in AbilityManager (owned by facade)
            _abilityManager.RegisterAbility(ability);

            // Also add to Health's tracking list for merge inheritance
            _health.MergedAbilities.Add(ability);

            Debug.Log($"[AnimalFacade] Added merged ability: {ability.GetType().Name}");
        }

        /// <summary>
        /// Removes an ability from the AbilityManager.
        /// </summary>
        public void RemoveAbility(IAbility ability)
        {
            if (ability == null)
            {
                Debug.LogWarning("[AnimalFacade] Attempted to remove null ability");
                return;
            }

            var removed = _abilityManager.UnregisterAbility(ability);
            if (removed)
            {
                // Also remove from Health's tracking list
                _health.MergedAbilities.Remove(ability);
                Debug.Log($"[AnimalFacade] Removed ability: {ability.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"[AnimalFacade] Failed to remove ability: {ability.GetType().Name}");
            }
        }

        /// <summary>
        /// Gets the AbilityManager for direct access to abilities.
        /// The facade owns the AbilityManager and provides it to components that need it.
        /// </summary>
        public AbilityManager AbilityManager => _abilityManager;

        /// <summary>
        /// Gets the current tiles per move value
        /// </summary>
        public int GetTilesPerMove() => _movement.TilesPerMove;

        /// <summary>
        /// Sets the tiles per move value
        /// </summary>
        public void SetTilesPerMove(int value) => _movement.SetTilesPerMove(value);


        public void ClearNodes() => _movement.ClearNodes();

        public async UniTask Move() => await _movement.Move(_target.Transformable.Position);

        public async UniTask Attack() => await _attack.Attack();

        // Getters for same-type merge calculations
        public float GetMaxHealth() => _health.Max;
        public float GetCurrentHealth() => _health.Current;
        public float GetDamage() => _attack.Damage;

        // Setters for same-type merge
        public void SetHealth(float newMaxHealth) => _health.SetMaxHealth(newMaxHealth);

        public void SetDamage(float newDamage) => _attack.SetDamage(newDamage);

        public virtual void ApplyStats(AnimalStats stats)
        {
            if (stats == null) return;
            _health.SetMaxHealth(stats.Health);
            _attack.SetDamage(stats.Damage);
            _movement.SetTilesPerMove(stats.TilesPerMove);
        }

        /// <summary>
        /// Notifies subscribers that this animal is about to be removed (merged or destroyed)
        /// Call this before deactivating/destroying the GameObject
        /// </summary>
        public void NotifyRemoved()
        {
            // Clear occupied grid cells before notifying about removal
            if (_movement != null)
            {
                _movement.ClearNodes();
                Debug.Log($"[AnimalFacade] Cleared grid cells for {name}");
            }

            OnRemoved?.Invoke(this);
        }
    }
}