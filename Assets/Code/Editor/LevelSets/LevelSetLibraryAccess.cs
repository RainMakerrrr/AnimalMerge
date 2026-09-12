#if UNITY_EDITOR
using Code.Levels;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.LevelSets
{
    public static class LevelSetLibraryAccess
    {
        private const string SetsField = "_sets";
        private const string DefaultSetField = "_defaultSet";

        public static LevelSetLibrary Load() =>
            AssetDatabase.LoadAssetAtPath<LevelSetLibrary>(LevelSetPaths.LibraryAsset);

        public static LevelSetLibrary LoadOrCreate()
        {
            var existing = Load();

            if (existing != null)
                return existing;

            LevelSetFolders.Ensure("Assets/Resources");

            var created = ScriptableObject.CreateInstance<LevelSetLibrary>();
            AssetDatabase.CreateAsset(created, LevelSetPaths.LibraryAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LevelSets] Created {LevelSetPaths.LibraryAsset}");

            return created;
        }

        public static bool IsRegistered(LevelSet set)
        {
            var library = Load();

            if (library == null || set == null)
                return false;

            for (var index = 0; index < library.Sets.Count; index++)
            {
                if (library.Sets[index] == set)
                    return true;
            }

            return false;
        }

        public static void Register(LevelSet set)
        {
            if (set == null)
                return;

            if (IsRegistered(set))
                return;

            var library = LoadOrCreate();
            var serialized = new SerializedObject(library);
            var sets = serialized.FindProperty(SetsField);

            sets.arraySize++;
            sets.GetArrayElementAtIndex(sets.arraySize - 1).objectReferenceValue = set;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static void Unregister(LevelSet set)
        {
            var library = Load();

            if (library == null || set == null)
                return;

            var serialized = new SerializedObject(library);
            var sets = serialized.FindProperty(SetsField);

            for (var index = sets.arraySize - 1; index >= 0; index--)
            {
                if (sets.GetArrayElementAtIndex(index).objectReferenceValue == set)
                    sets.DeleteArrayElementAtIndex(index);
            }

            var defaultSet = serialized.FindProperty(DefaultSetField);

            if (defaultSet.objectReferenceValue == set)
                defaultSet.objectReferenceValue = null;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static void SetDefault(LevelSet set)
        {
            if (set == null)
                return;

            Register(set);

            var library = LoadOrCreate();
            var serialized = new SerializedObject(library);

            serialized.FindProperty(DefaultSetField).objectReferenceValue = set;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
        }

        public static LevelSet DefaultSet()
        {
            var library = Load();

            return library == null ? null : library.DefaultSet;
        }
    }
}
#endif
