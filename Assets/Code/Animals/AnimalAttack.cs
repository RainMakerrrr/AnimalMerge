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
            if (_attackPoint == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_attackPoint.position, _radius);
        }

        public void AttackAnimationHandler()
        {
            var animal = GetComponent<Animal>();
            if (animal != null && animal.Type == AnimalType.Hedgehog) return;
            if (_attackPoint == null) return;

            if (_colliders == null || _colliders.Length != _maxTargets)
                _colliders = new Collider[_maxTargets];

            int count = Physics.OverlapSphereNonAlloc(_attackPoint.position, _radius, _colliders, _mask);
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                Collider col = _colliders[i];
                if (col == null) continue;

                var health = col.GetComponentInParent<IDamageable>();
                if (health == null) continue;

                health.TakeDamage(this);
            }
        }

        private Collider GetClosestCollider() =>
            _colliders.Where(c => c.GetComponentInParent<IDamageable>() != null)
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).First();
    }
}