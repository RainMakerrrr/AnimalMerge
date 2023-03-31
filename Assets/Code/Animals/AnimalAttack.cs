using System;
using System.Linq;
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
        [SerializeField] private int _maxTargets;
        [SerializeField] private LayerMask _mask;

        private Collider[] _colliders;
        public float Damage => _damage;

        private void Start() => _colliders = new Collider[_maxTargets];

        public void Upgrade(float multiplier) => _damage *= multiplier;

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
            int count = Physics.OverlapSphereNonAlloc(_attackPoint.position, _radius, _colliders, _mask);
            if (count == 0) return;
            
            //Collider closestCollider = GetClosestCollider();

            //closestCollider.GetComponentInParent<IDamageable>()?.TakeDamage(this);

            foreach (Collider col in _colliders)
            {
                var health = col.GetComponentInParent<IDamageable>();
                health?.TakeDamage(this);
            }
        }

        private Collider GetClosestCollider() =>
            _colliders.Where(c => c.GetComponentInParent<IDamageable>() != null)
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).First();
    }
}