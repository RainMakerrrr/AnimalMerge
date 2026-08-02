#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Data.Animals;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalDatabaseWriter
    {
        public static AnimalStats ResolveOrCreateStats(AnimalRecipe recipe, List<ValidationFinding> findings)
        {
            if (recipe.Stats != null) return recipe.Stats;

            var scriptType = recipe.StatsScript == null ? null : recipe.StatsScript.GetClass();
            if (scriptType == null) return null;

            var path = $"{AnimalPrefabPaths.StatsFolder}/{recipe.Type}Stats.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AnimalStats>(path);

            if (existing != null)
            {
                if (!scriptType.IsInstanceOfType(existing))
                {
                    findings.Add(ValidationFinding.Error(
                        "R17",
                        $"{recipe.name}: {path} already exists as {existing.GetType().Name} but the recipe " +
                        $"asks for {scriptType.Name}. Convert it by hand rather than losing its balance values",
                        recipe));
                    return null;
                }

                return existing;
            }

            EnsureFolder(AnimalPrefabPaths.StatsFolder);

            var created = (AnimalStats)ScriptableObject.CreateInstance(scriptType);
            AssetDatabase.CreateAsset(created, path);
            Debug.Log($"[AnimalPrefabTool] Created {path}. Fill in its balance values from the concept doc.");

            return created;
        }

        public static void EnsureDatabaseRow(
            AnimalRecipe recipe,
            AnimalStats stats,
            List<ValidationFinding> findings)
        {
            var database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>(AnimalPrefabPaths.AnimalDatabaseAsset);
            if (database == null)
            {
                findings.Add(ValidationFinding.Error(
                    "R17",
                    $"{recipe.name}: AnimalDatabase not found at {AnimalPrefabPaths.AnimalDatabaseAsset}",
                    recipe));
                return;
            }

            var serialized = new SerializedObject(database);
            var configs = serialized.FindProperty("_configs");

            if (configs == null || !configs.isArray)
            {
                findings.Add(ValidationFinding.Error(
                    "R17", $"{recipe.name}: AnimalDatabase has no _configs array", database));
                return;
            }

            var row = FindRow(configs, (int)recipe.Type);

            if (row == null)
            {
                configs.arraySize++;
                row = configs.GetArrayElementAtIndex(configs.arraySize - 1);
                row.FindPropertyRelative("Type").intValue = (int)recipe.Type;
                Debug.Log($"[AnimalPrefabTool] Added an AnimalDatabase row for {recipe.Type}.");
            }

            FillIfEmpty(row, "Stats", stats);
            FillIfEmpty(row, "Icon", recipe.Icon);

            var ally = recipe as AllyAnimalRecipe;
            if (ally != null && !string.IsNullOrEmpty(ally.MergeInfo))
            {
                var mergeInfo = row.FindPropertyRelative("MergeInfo");
                if (mergeInfo != null && string.IsNullOrEmpty(mergeInfo.stringValue))
                    mergeInfo.stringValue = ally.MergeInfo;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindRow(SerializedProperty configs, int type)
        {
            for (var i = 0; i < configs.arraySize; i++)
            {
                var element = configs.GetArrayElementAtIndex(i);
                var typeProperty = element.FindPropertyRelative("Type");
                if (typeProperty != null && typeProperty.intValue == type) return element;
            }

            return null;
        }

        private static void FillIfEmpty(SerializedProperty row, string fieldName, Object value)
        {
            if (value == null) return;

            var property = row.FindPropertyRelative(fieldName);
            if (property == null || property.objectReferenceValue != null) return;

            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
