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
        [SerializeField] private float _forwardReach = 0.25f;
        [SerializeField] private float _damage;
        [SerializeField] private int _maxTargets;
        [SerializeField] private LayerMask _mask;

        private Collider[] _colliders;
        public float Damage => _damage;

        private void Start() => _colliders = new Collider[_maxTargets];

        public void Upgrade(float multiplier) => _damage *= multiplier;

        public void SetDamage(float newDamage)
        {
            _damage = newDamage;
        }

        public async Task Attack()
        {
            if (GetComponent<Animal>().Type == AnimalType.Hedgehog) return;

            await _animator.WaitForAttackAnimation();
        }

        private void OnDrawGizmos()
        {
            if (_attackPoint == null) return;
            Gizmos.color = Color.red;
            Vector3 center = _attackPoint.position + transform.forward * _forwardReach;
            Gizmos.DrawWireSphere(center, _radius);
        }

        public void AttackAnimationHandler()
        {
            var animal = GetComponent<Animal>();
            if (animal != null && animal.Type == AnimalType.Hedgehog) return;
            if (_attackPoint == null) return;

            if (_colliders == null || _colliders.Length != _maxTargets)
                _colliders = new Collider[_maxTargets];

            Vector3 a = _attackPoint.position;
            Vector3 b = _attackPoint.position + transform.forward * (_forwardReach + _radius);
            int count = Physics.OverlapCapsuleNonAlloc(a, b, _radius, _colliders, _mask);
            if (count <= 0)
            {
                // Fallback to a simple sphere centered slightly forward
                Vector3 center = _attackPoint.position + transform.forward * _forwardReach;
                count = Physics.OverlapSphereNonAlloc(center, _radius, _colliders, _mask);
                if (count <= 0) return;
            }

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