#if UNITY_EDITOR
using UnityEditor;

namespace Code.Editor.LevelSets
{
    public static class LevelSetFolders
    {
        public static void Ensure(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];

            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);

                current = next;
            }
        }

        public static void EnsureSetFolders(string setName)
        {
            Ensure(LevelSetPaths.SetFolder(setName));
            Ensure(LevelSetPaths.LevelsFolder(setName));
            Ensure(LevelSetPaths.StagesFolder(setName));
        }
    }
}
#endif
