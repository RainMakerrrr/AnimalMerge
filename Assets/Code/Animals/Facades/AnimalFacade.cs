using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Health;
using Code.Animals.Merge.MergeSkills;
using Code.Animals.Movement;
using Code.Animals.Upgrade;
using UnityEngine;

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

        public IDamageable Damageable => _health;
        public ITransformable Transformable => _movement;
        public AnimalType Type => _type;

        public AnimalHealth Health => _health;
        public AnimalAttack AttackInstance => _attack;
        public AnimalMovement Movement => _movement;
        public Collider[] Colliders => _colliders;
        public AnimalAnimator Animator => _animator;


        protected IAbility Ability;
        public IMergeSkill MergeSkill { get; protected set; }

        public List<IAbility> AdditionalAbilities = new List<IAbility>();

        public List<IMergeSkill> MergeSkills = new List<IMergeSkill>();

        private ITarget _target;

        private void Awake()
        {
            InitBehaviours();
            _health.Construct(_colliders);
        }

        public abstract void InitBehaviours();

        public void UpgradeHealth(float multiplier) => _upgrade.UpgradeHealth(multiplier);
        public void UpgradeDamage(float multiplier) => _upgrade.UpgradeDamage(multiplier);
        public void UpgradeSpeed(int multiplier) => _upgrade.UpgradeSpeed(multiplier);

        public void AddAbility(IAbility ability)
        {
            AdditionalAbilities.Add(ability);
            _health.AddAbility(ability);
        }

        public void RemoveAbility(IAbility ability) => AdditionalAbilities.Remove(ability);
        

        public void ClearNodes() => _movement.ClearNodes();

        public async Task Move() => await _movement.Move(_target.Transformable.Position);

        public async Task Attack() => await _attack.Attack();
    }
}