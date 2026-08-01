using UnityEngine;

namespace Code.Data.Animals
{
    [CreateAssetMenu(fileName = "Hedgehog Stats", menuName = "Stats/Hedgehog Stats")]
    public class HedgehogStats : AnimalStats
    {
        public const int DefaultOwnerCounterChance = 100;
        public const int DefaultInheritedCounterChance = 50;

        [SerializeField, Range(0, 100)] private int _ownerCounterChance = DefaultOwnerCounterChance;
        [SerializeField, Range(0, 100)] private int _inheritedCounterChance = DefaultInheritedCounterChance;

        public int OwnerCounterChance => _ownerCounterChance;
        public int InheritedCounterChance => _inheritedCounterChance;
    }
}
