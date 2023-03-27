using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Health;
using UnityEngine;

namespace Code.Animals
{
    public class AnimalFacade : MonoBehaviour, ITarget
    {
        [SerializeField] private AnimalType _type;
        [SerializeField] private AnimalMovement _movement;
        [SerializeField] private AnimalAttack _attack;
        [SerializeField] private AnimalHealth _health;

        public IDamageable Damageable => _health;
        public ITransformable Transformable => _movement;

        private ITarget _target;

        protected IAbility Ability;

        public virtual void InitBehaviours()
        {
            
        }
        
        public AnimalType Type => _type;

        public void SetTarget(ITarget target)
        {
            _target = target;
            _attack.SetTarget(target.Damageable);
        }

        public void ClearNodes() => _movement.ClearNodes();

        public async Task Move() => await _movement.Move(_target.Transformable.Position);

        public void Attack() => _attack.Attack();
    }

    public class FoxFacade : AnimalFacade
    {
        public override void InitBehaviours()
        {
        }
    }
}