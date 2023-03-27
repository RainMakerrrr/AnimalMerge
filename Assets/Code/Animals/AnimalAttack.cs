using System;
using System.Threading.Tasks;
using Code.Animals.Health;
using UnityEngine;

namespace Code.Animals
{
    public class AnimalAttack : MonoBehaviour
    {
        [SerializeField] private AnimalAnimator _animator;
        [SerializeField] private Transform _attackPoint;
        [SerializeField] private float _radius;
        [SerializeField] private float _damage;
        [SerializeField] private LayerMask _mask;

        private readonly Collider[] _colliders = new Collider[1];

        private IDamageable _target;


        public float Damage => _damage;
        public void SetTarget(IDamageable target) => _target = target;

        public async Task Attack()
        {
            if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;
            
            await _animator.WaitForAttackAnimation();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_attackPoint.position, _radius);
        }
        
        public void AttackAnimationHandler()
        {
            Debug.Log("Attack HANDLER");

            int count = Physics.OverlapSphereNonAlloc(_attackPoint.position, _radius, _colliders, _mask);
            if (count == 0) return;
            
            foreach (Collider col in _colliders)
            {
                var health = col.GetComponentInParent<IDamageable>();
                health?.TakeDamage(this);
            }

            //_target?.TakeDamage(_damage);
        }
    }
}