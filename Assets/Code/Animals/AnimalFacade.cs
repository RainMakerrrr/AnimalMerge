using Code.Animals.Health;
using UnityEngine;

namespace Code.Animals
{
    public class AnimalFacade : MonoBehaviour
    {
        [SerializeField] private AnimalType _type;
        [SerializeField] private AnimalMovement _movement;
        [SerializeField] private AnimalAttack _attack;

        private IDamageable _target;
        
        public AnimalType Type => _type;
        
        public void ClearNodes() => _movement.ClearNodes();
        
        public void Attack() => _attack.Attack();
    }
}