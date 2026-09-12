using System.Collections.Generic;
using Code.Battle.Config;
using Code.Infrastructure;
using UnityEngine;

namespace Code.Levels
{
    [CreateAssetMenu(fileName = "LevelSet", menuName = "Game/Level Set")]
    public class LevelSet : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ExtendedLevel[] _tutorialLevels = new ExtendedLevel[0];
        [SerializeField] private ExtendedLevel[] _levels = new ExtendedLevel[0];
        [SerializeField] private PreBattleConfig _preBattleConfig;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public PreBattleConfig PreBattleConfig => _preBattleConfig;

        public IReadOnlyList<ExtendedLevel> TutorialLevels =>
            _tutorialLevels ?? (IReadOnlyList<ExtendedLevel>)new ExtendedLevel[0];

        public IReadOnlyList<ExtendedLevel> Levels =>
            _levels ?? (IReadOnlyList<ExtendedLevel>)new ExtendedLevel[0];

        private void OnValidate()
        {
            WarnAboutMissingEntries(_tutorialLevels, nameof(_tutorialLevels));
            WarnAboutMissingEntries(_levels, nameof(_levels));
            WarnAboutDuplicates(_levels, nameof(_levels));
        }

        private void WarnAboutMissingEntries(ExtendedLevel[] levels, string fieldName)
        {
            if (levels == null)
                return;

            for (var index = 0; index < levels.Length; index++)
            {
                if (levels[index] == null)
                    Debug.LogWarning($"[LevelSet] {name}: {fieldName}[{index}] is empty", this);
            }
        }

        private void WarnAboutDuplicates(ExtendedLevel[] levels, string fieldName)
        {
            if (levels == null)
                return;

            var alreadyListed = new HashSet<ExtendedLevel>();

            for (var index = 0; index < levels.Length; index++)
            {
                if (levels[index] == null)
                    continue;

                if (!alreadyListed.Add(levels[index]))
                    Debug.LogWarning(
                        $"[LevelSet] {name}: {fieldName}[{index}] repeats {levels[index].name}", this);
            }
        }
    }
}
