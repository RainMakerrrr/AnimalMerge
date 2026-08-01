using UnityEngine;

namespace Code.Data.Animals
{
    [CreateAssetMenu(fileName = "Fox Stats", menuName = "Stats/Fox Stats")]
    public class FoxStats : AnimalStats
    {
        public const int DefaultOwnerDodgeChance = 80;
        public const int DefaultInheritedDodgeChance = 50;

        [SerializeField, Range(0, 100)] private int _ownerDodgeChance = DefaultOwnerDodgeChance;
        [SerializeField, Range(0, 100)] private int _inheritedDodgeChance = DefaultInheritedDodgeChance;

        public int OwnerDodgeChance => _ownerDodgeChance;
        public int InheritedDodgeChance => _inheritedDodgeChance;
    }
}
