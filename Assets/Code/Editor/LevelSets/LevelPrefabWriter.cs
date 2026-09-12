#if UNITY_EDITOR
using Code.Battle.Config;
using Code.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.LevelSets
{
    public static class LevelPrefabWriter
    {
        public const string IdField = "id";
        public const string StagesField = "_stages";

        public static ExtendedLevel CreatePrefab(string folder, string levelName)
        {
            LevelSetFolders.Ensure(folder);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{levelName}.prefab");
            var uniqueName = System.IO.Path.GetFileNameWithoutExtension(path);

            var template = new GameObject(uniqueName);
            template.AddComponent<ExtendedLevel>();

            var savedRoot = PrefabUtility.SaveAsPrefabAsset(template, path);
            Object.DestroyImmediate(template);

            if (savedRoot == null)
            {
                Debug.LogError($"[LevelSets] Failed to save a level prefab at {path}");
                return null;
            }

            var level = savedRoot.GetComponent<ExtendedLevel>();
            SetId(level, uniqueName);

            return level;
        }

        public static GameObject ResolveRoot(ExtendedLevel level)
        {
            if (level == null)
                return null;

            var path = AssetDatabase.GetAssetPath(level);

            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static void Save(ExtendedLevel level)
        {
            var root = ResolveRoot(level);

            if (root == null)
            {
                if (level != null)
                    EditorUtility.SetDirty(level);

                return;
            }

            PrefabUtility.SavePrefabAsset(root);
        }

        public static string GetId(ExtendedLevel level)
        {
            if (level == null)
                return string.Empty;

            var property = new SerializedObject(level).FindProperty(IdField);

            return property == null ? string.Empty : property.stringValue;
        }

        public static void SetId(ExtendedLevel level, string id)
        {
            if (level == null)
                return;

            var serialized = new SerializedObject(level);
            var property = serialized.FindProperty(IdField);

            if (property == null)
                return;

            property.stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Save(level);
        }

        public static void AppendStage(ExtendedLevel level, LevelStageConfig stage)
        {
            if (level == null)
                return;

            var serialized = new SerializedObject(level);
            var stages = serialized.FindProperty(StagesField);

            stages.arraySize++;
            stages.GetArrayElementAtIndex(stages.arraySize - 1).objectReferenceValue = stage;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            Save(level);
        }
    }
}
#endif
