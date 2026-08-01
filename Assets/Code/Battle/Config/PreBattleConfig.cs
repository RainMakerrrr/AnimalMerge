using System.Collections.Generic;
using Code.Animals;
using UnityEngine;

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "PreBattleConfig", menuName = "Game/Pre Battle Config")]
    public class PreBattleConfig : ScriptableObject
    {
        [SerializeField] private AnimalType[] _startingPool =
        {
            AnimalType.Cheetah,
            AnimalType.Fox,
            AnimalType.Elephant
        };

        [SerializeField] private AnimalType[] _randomPool =
        {
            AnimalType.Elephant,
            AnimalType.Cheetah,
            AnimalType.Deer,
            AnimalType.Fox,
            AnimalType.Hedgehog,
            AnimalType.Chicken
        };

        [SerializeField, Min(1)] private int _minAlliesToStart = 2;

        [SerializeField, Min(0)] private int _reinforcementsPerLevel = 1;

        public IReadOnlyList<AnimalType> StartingPool => _startingPool;
        public IReadOnlyList<AnimalType> RandomPool => _randomPool;
        public int MinAlliesToStart => _minAlliesToStart;
        public int ReinforcementsPerLevel => _reinforcementsPerLevel;
    }
}
