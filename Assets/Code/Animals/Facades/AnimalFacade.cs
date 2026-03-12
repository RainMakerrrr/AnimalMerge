using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Health;
using Code.Animals.Merge;
using Code.Animals.Merge.MergeSkills;
using Code.Animals.Movement;
using Code.Animals.Upgrade;
using Code.Services.Random;
using UnityEngine;
using Zenject;

namespace Code.Animals.Facades
{
    public abstract class AnimalFacade : MonoBehaviour, ITarget
    {
        [SerializeField] private AnimalType _type;
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private AnimalAttack _attack;
        [SerializeField] private AnimalUpgrade _upgrade;
        [SerializeField] private Collider[] _colliders;
        [SerializeField] protected AnimalHealth _health;
        [SerializeField] protected AnimalMovement _movement;
        [SerializeField] private MergeView _mergeView;

        protected IRandomProvider _randomProvider;

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
        public Collider[] Colliders => _colliders;
        public AnimalAnimator Animator => _animator;

        /// <summary>
        /// Gets the MergeView component. If not set in inspector, tries to find it on the GameObject.
        /// </summary>
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

        public IRandomProvider RandomProvider => _randomProvider;
        public bool IsBoss { get; set; }
        
        [Inject]
        private void Construct(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
        }


        protected IAbility Ability;
        public IMergeSkill MergeSkill { get; protected set; }

        public List<IMergeSkill> MergeSkills = new List<IMergeSkill>();

        private ITarget _target;

        private void Awake()
        {
            InitBehaviours();
            _health.Construct(_colliders);
            _health.Died += NotifyRemoved;
        }

        private void OnDestroy()
        {
            _health.Died -= NotifyRemoved;
        }

        public abstract void InitBehaviours();

        public void UpgradeHealth(float multiplier) => _upgrade.UpgradeHealth(multiplier);
        public void UpgradeDamage(float multiplier) => _upgrade.UpgradeDamage(multiplier);
        public void UpgradeSpeed(int multiplier) => _upgrade.UpgradeSpeed(multiplier);

        /// <summary>
        /// Adds an ability to this animal. The ability will be registered in AnimalHealth's AbilityManager.
        /// </summary>
        public void AddAbility(IAbility ability)
        {
            _health.AddAbility(ability); // Registers in AbilityManager
        }

        /// <summary>
        /// Removes an ability from the AbilityManager.
        /// </summary>
        public void RemoveAbility(IAbility ability)
        {
            // TODO: Implement AbilityManager.UnregisterAbility() if needed in the future
            Debug.LogWarning("[AnimalFacade] RemoveAbility not yet implemented in AbilityManager");
        }

        /// <summary>
        /// Gets the AbilityManager for direct access to abilities (needed for undo operations)
        /// </summary>
        public AbilityManager AbilityManager => _health?.AbilityManager;

        /// <summary>
        /// Gets the current tiles per move value
        /// </summary>
        public int GetTilesPerMove() => _movement.TilesPerMove;

        /// <summary>
        /// Sets the tiles per move value
        /// </summary>
        public void SetTilesPerMove(int value) => _movement.SetTilesPerMove(value);
        

        public void ClearNodes() => _movement.ClearNodes();

        public async Task Move() => await _movement.Move(_target.Transformable.Position);

        public async Task Attack() => await _attack.Attack();

        // Getters for same-type merge calculations
        public float GetMaxHealth() => _health.Max;
        public float GetCurrentHealth() => _health.Current;
        public float GetDamage() => _attack.Damage;

        // Setters for same-type merge
        public void SetHealth(float newMaxHealth) => _health.SetMaxHealth(newMaxHealth);

        public void SetDamage(float newDamage) => _attack.SetDamage(newDamage);

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