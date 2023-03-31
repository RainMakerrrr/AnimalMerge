using Code.Animals.Health;
using Code.Animals.Movement;
using UnityEngine;

namespace Code.Animals.Upgrade
{
    public class AnimalUpgrade : MonoBehaviour
    {
        [SerializeField] private AnimalHealth _health;
        [SerializeField] private AnimalAttack _attack;
        [SerializeField] private AnimalMovement _movement;

        public void UpgradeHealth(float multiplier)
        {
            _health.Upgrade(multiplier);
        }

        public void UpgradeDamage(float multiplier)
        {
            _attack.Upgrade(multiplier);
        }

        public void UpgradeSpeed(int multiplier)
        {
            _movement.Upgrade(multiplier);
        }
    }
}