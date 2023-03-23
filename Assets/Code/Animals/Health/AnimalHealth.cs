using System;
using UnityEngine;

namespace Code.Animals.Health
{
    public class AnimalHealth : MonoBehaviour, IDamageable
    {
        public event Action TakenDamage;
        public event Action Died;

        [SerializeField] private float _max;

        public float Current { get; private set; }
        public float Max { get; private set; }

        private bool IsDead => Current <= 0;

        private void Start()
        {
            Max = _max;
            Current = Max;
        }

        public void TakeDamage(float damage)
        {
            Current -= damage;
            TakenDamage?.Invoke();

            if (IsDead)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Die");
            Died?.Invoke();
            Destroy(gameObject);
        }
    }
}