using UnityEngine;

namespace Code.Data.Animals
{
    [CreateAssetMenu(fileName = "Velociraptor Stats", menuName = "Stats/Velociraptor Stats")]
    public class VelociraptorStats : AnimalStats
    {
        public const int DefaultOwnerRetreatChance = 100;
        public const int DefaultInheritedRetreatChance = 50;

        [SerializeField, Range(0, 100)] private int _ownerRetreatChance = DefaultOwnerRetreatChance;
        [SerializeField, Range(0, 100)] private int _inheritedRetreatChance = DefaultInheritedRetreatChance;

        public int OwnerRetreatChance => _ownerRetreatChance;
        public int InheritedRetreatChance => _inheritedRetreatChance;
    }
}
