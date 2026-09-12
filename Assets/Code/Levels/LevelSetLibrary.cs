using System.Collections.Generic;
using UnityEngine;

namespace Code.Levels
{
    [CreateAssetMenu(fileName = "LevelSetLibrary", menuName = "Game/Level Set Library")]
    public class LevelSetLibrary : ScriptableObject
    {
        [SerializeField] private LevelSet[] _sets = new LevelSet[0];
        [SerializeField] private LevelSet _defaultSet;

        public IReadOnlyList<LevelSet> Sets => _sets ?? (IReadOnlyList<LevelSet>)new LevelSet[0];

        public LevelSet DefaultSet => _defaultSet != null ? _defaultSet : FirstRegisteredSet();

        private LevelSet FirstRegisteredSet()
        {
            if (_sets == null)
                return null;

            for (var index = 0; index < _sets.Length; index++)
            {
                if (_sets[index] != null)
                    return _sets[index];
            }

            return null;
        }
    }
}
