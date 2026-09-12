using System.Collections.Generic;
using Code.Animals;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using Code.Battle.PreBattle;
using Code.GridPathfinding;
using Code.GridPathfinding.Config;
using UnityEditor;
#endif

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "PreBattleConfig", menuName = "Game/Pre Battle Config")]
    public class PreBattleConfig : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("_startingPool")] private AnimalType[] _baseRoster =
        {
            AnimalType.Cheetah,
            AnimalType.Fox,
            AnimalType.Elephant
        };

        [SerializeField] private AnimalUnlockEntry[] _unlocks =
        {
            new AnimalUnlockEntry(1, AnimalType.Deer),
            new AnimalUnlockEntry(2, AnimalType.Hedgehog),
            new AnimalUnlockEntry(3, AnimalType.Chicken)
        };

        [SerializeField] private bool _guaranteeNewlyUnlockedReinforcement = true;

        [SerializeField, Min(1)] private int _minAlliesToStart = 2;

        [SerializeField, Min(0)] private int _reinforcementsPerLevel = 1;

        public IReadOnlyList<AnimalType> BaseRoster => _baseRoster;
        public IReadOnlyList<AnimalUnlockEntry> Unlocks => _unlocks;
        public bool GuaranteeNewlyUnlockedReinforcement => _guaranteeNewlyUnlockedReinforcement;
        public int MinAlliesToStart => _minAlliesToStart;
        public int ReinforcementsPerLevel => _reinforcementsPerLevel;

        private void OnValidate()
        {
            ValidateUniqueAnimals();

#if UNITY_EDITOR
            EditorApplication.delayCall -= ValidateRosterFitsDeploymentZone;
            EditorApplication.delayCall += ValidateRosterFitsDeploymentZone;
#endif
        }

        private void ValidateUniqueAnimals()
        {
            var alreadyListed = new HashSet<AnimalType>();

            if (_baseRoster != null)
            {
                foreach (var animal in _baseRoster)
                {
                    if (!alreadyListed.Add(animal))
                        Debug.LogWarning($"[PreBattleConfig] {animal} is listed more than once in the base roster - only the first entry is used", this);
                }
            }

            if (_unlocks == null)
                return;

            foreach (var entry in _unlocks)
            {
                if (entry.CompletedLevel < 1)
                    Debug.LogWarning($"[PreBattleConfig] Unlock of {entry.Animal} points at completed level {entry.CompletedLevel} - levels start at 1", this);

                if (!alreadyListed.Add(entry.Animal))
                    Debug.LogWarning($"[PreBattleConfig] {entry.Animal} is already available before its unlock at completed level {entry.CompletedLevel} - only the first entry is used", this);
            }
        }

#if UNITY_EDITOR
        private const int FirstLevel = 1;

        private void ValidateRosterFitsDeploymentZone()
        {
            if (this == null)
                return;

            var gridConfig = FindGridConfig();
            if (gridConfig == null)
                return;

            int capacity = gridConfig.Width * DeploymentZone.Depth;

            for (int level = FirstLevel; level <= LastReachableLevel(); level++)
            {
                int required = RequiredDeploymentCells(level);

                if (required <= capacity)
                    continue;

                Debug.LogWarning(
                    $"[PreBattleConfig] The level {level} roster needs {required} deployment cells but " +
                    $"'{gridConfig.name}' offers {capacity} ({gridConfig.Width} columns x {DeploymentZone.Depth} rows) - " +
                    "the last animals of that roster will have nowhere to spawn",
                    this);

                return;
            }
        }

        private int RequiredDeploymentCells(int level)
        {
            int required = 0;

            foreach (var animal in AnimalRosterResolver.Resolve(this, level))
            {
                required += AnimalFootprints.CellCount(animal);
            }

            return required;
        }

        private int LastReachableLevel()
        {
            int lastUnlockLevel = 0;

            if (_unlocks != null)
            {
                foreach (var entry in _unlocks)
                {
                    if (entry.CompletedLevel > lastUnlockLevel)
                        lastUnlockLevel = entry.CompletedLevel;
                }
            }

            return lastUnlockLevel + 1;
        }

        private static GridConfig FindGridConfig()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(GridConfig)}");

            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<GridConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
#endif
    }
}
