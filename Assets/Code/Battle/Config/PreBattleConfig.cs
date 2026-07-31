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

        [SerializeField, Min(1)] private int _minAlliesToStart = 2;

        public IReadOnlyList<AnimalType> StartingPool => _startingPool;
        public int MinAlliesToStart => _minAlliesToStart;
    }
}
