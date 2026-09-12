#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Levels;
using UnityEditor;

namespace Code.Editor.LevelSets
{
    public static class LevelSetCatalog
    {
        public static List<LevelSet> FindAll()
        {
            var sets = new List<LevelSet>();

            foreach (var guid in AssetDatabase.FindAssets("t:LevelSet"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var set = AssetDatabase.LoadAssetAtPath<LevelSet>(path);

                if (set != null)
                    sets.Add(set);
            }

            sets.Sort((left, right) => string.Compare(left.name, right.name, System.StringComparison.Ordinal));

            return sets;
        }
    }
}
#endif
