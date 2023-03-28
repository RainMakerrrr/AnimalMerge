using System;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    public class AnimalHealth : MonoBehaviour, IDamageable
    {
        public event Action TakenDamage;
        public event Action Died;

        [SerializeField] private float _max;
        [SerializeField] private AnimalAnimator _animator;

        private IAbility _ability;
        
        public float Current { get; private set; }
        public float Max { get; private set; }

        public bool IsDead => Current <= 0;

        private Animal _animal;

        public AnimalAttack LastAttack { get; private set; }


        private void Start()
        {
            Max = _max;
            Current = Max;
            _animal = GetComponent<Animal>();
            //_ability = new CounterAttack(this);
            _ability = new Dodge(GetComponent<ITransformable>());
        }

        public void TakeDamage(AnimalAttack attacker)
        {
            if (_animal.Type == AnimalType.Fox)
            {
                if (_ability.CanUse)
                {
                    _ability.Apply();
                    return;
                }
            }

            LastAttack = attacker;

            Current -= attacker.Damage;
            TakenDamage?.Invoke();
            
            _animator.TakeDamageAnimation();
            // if (_animal.Type == AnimalType.Hedgehog)
            // {
            //     if (_ability.CanUse)
            //     {
            //         _animator.CounterAttackAnimation();
            //     }
            //     else
            //     {
            //         _animator.TakeDamageAnimation();
            //     }
            // }
            // else
            // {
            //     _animator.TakeDamageAnimation();
            // }
            
            if (IsDead)
            {
                Die();
            }
        }


        public void CounterAttackAnimationHandler()
        {
            _ability.Apply();

        }
        
        private void Die()
        {
            Debug.Log("Die");
            Died?.Invoke();
            
            _animator.DeathAnimation();
            
            Destroy(gameObject, 3f);
        }
    }
}