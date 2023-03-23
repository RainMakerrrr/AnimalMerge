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

        public void SetTarget(IDamageable target) => _target = target;

        //public void Attack() => _animator.PlayAttackAnimation();
        public async Task Attack() => await _animator.WaitForAttackAnimation();

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_attackPoint.position, _radius);
        }

        public void AttackAnimationHandler()
        {
            Debug.Log("Attack HANDLER");

            int count = Physics.OverlapSphereNonAlloc(_attackPoint.position, _radius, _colliders, _mask);
            Debug.Log(count);
            if (count == 0) return;

            foreach (Collider col in _colliders)
            {
                Debug.Log(col.name);
                var health = col.GetComponent<IDamageable>();
                Debug.Log(health == null);
                health?.TakeDamage(_damage);
            }

            //_target?.TakeDamage(_damage);
        }
    }
}