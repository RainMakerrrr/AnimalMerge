#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalPrefabTool
    {
        private const string LogPrefix = "[AnimalPrefabTool]";

        [MenuItem("Tools/Animals/Build Ally Prefab From Recipe")]
        public static void BuildAllyPrefab() => BuildSelected<AllyAnimalRecipe>("ally");

        [MenuItem("Tools/Animals/Build Enemy Prefab From Recipe")]
        public static void BuildEnemyPrefab() => BuildSelected<EnemyAnimalRecipe>("enemy");

        [MenuItem("Tools/Animals/Build All Animal Recipes")]
        public static void BuildAllRecipes()
        {
            var recipes = AssetDatabase.FindAssets($"t:{nameof(AnimalRecipe)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AnimalRecipe>)
                .Where(recipe => recipe != null)
                .OrderBy(recipe => recipe.name)
                .ToList();

            if (recipes.Count == 0)
            {
                Debug.LogError($"{LogPrefix} No AnimalRecipe assets found in the project");
                return;
            }

            var proceed = EditorUtility.DisplayDialog(
                "Rebuild every animal prefab?",
                $"{recipes.Count} recipes will be rebuilt. Every generated prefab is replaced and any hand " +
                "tuning made directly on those prefabs is lost.",
                "Rebuild all",
                "Cancel");

            if (!proceed) return;

            var built = recipes.Count(recipe => AnimalPrefabBuildRunner.Build(recipe, false));
            Debug.Log($"{LogPrefix} Built {built} of {recipes.Count} recipes.");
        }

        [MenuItem("Tools/Animals/Add Attack Animation Event")]
        public static void AddAttackAnimationEvent()
        {
            var clips = Selection.GetFiltered<AnimationClip>(SelectionMode.Assets);
            if (clips.Length == 0)
            {
                Debug.LogError($"{LogPrefix} Select one or more standalone .anim clips in the Project window");
                return;
            }

            var changed = 0;

            foreach (var clip in clips)
            {
                var path = AssetDatabase.GetAssetPath(clip);
                if (!path.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError(
                        $"{LogPrefix} {clip.name} lives inside {path} and is not a standalone clip. " +
                        "Duplicate it into its own .anim first",
                        clip);
                    continue;
                }

                var events = AnimationUtility.GetAnimationEvents(clip).ToList();
                if (events.Any(e => e.functionName == AnimalPrefabConstants.AttackEventFunctionName))
                {
                    Debug.Log($"{LogPrefix} {clip.name} already has the event, skipped", clip);
                    continue;
                }

                events.Add(new AnimationEvent
                {
                    functionName = AnimalPrefabConstants.AttackEventFunctionName,
                    time = clip.length * 0.6f
                });

                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                EditorUtility.SetDirty(clip);
                changed++;

                Debug.Log(
                    $"{LogPrefix} Added {AnimalPrefabConstants.AttackEventFunctionName} to {clip.name} at " +
                    $"t={clip.length * 0.6f:0.###}s. Check that the outgoing transition exit time is later",
                    clip);
            }

            if (changed > 0) AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Added the attack event to {changed} of {clips.Length} clips.");
        }

        private static void BuildSelected<T>(string sideName) where T : AnimalRecipe
        {
            var recipes = Selection.GetFiltered<T>(SelectionMode.Assets);

            if (recipes.Length == 0)
            {
                var wrongSide = Selection.GetFiltered<AnimalRecipe>(SelectionMode.Assets);
                if (wrongSide.Length > 0)
                {
                    Debug.LogError(
                        $"{LogPrefix} The selection holds {wrongSide[0].GetType().Name} assets, but this menu " +
                        $"item builds {sideName} prefabs only. Use the other Build item");
                    return;
                }

                Debug.LogError(
                    $"{LogPrefix} Select one or more {typeof(T).Name} assets in the Project window first");
                return;
            }

            var built = recipes.Count(recipe => AnimalPrefabBuildRunner.Build(recipe, true));
            Debug.Log($"{LogPrefix} Built {built} of {recipes.Length} {sideName} prefabs.");
        }

        [MenuItem("Tools/Animals/Validate Animal Prefabs")]
        public static void ValidateAnimalPrefabs()
        {
            var searchFolders = new[] { AnimalPrefabPaths.AnimalsFolder, AnimalPrefabPaths.EnemiesFolder };

            var paths = AssetDatabase.FindAssets("t:Prefab", searchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => path)
                .ToList();

            if (paths.Count == 0)
            {
                Debug.LogWarning($"{LogPrefix} No prefabs found in {string.Join(" or ", searchFolders)}");
                return;
            }

            var context = AnimalPrefabAuditContext.Build(paths);
            if (context.Database == null)
            {
                Debug.LogWarning(
                    $"{LogPrefix} AnimalDatabase not found at {AnimalPrefabPaths.AnimalDatabaseAsset} - " +
                    "database rules were skipped");
            }

            var errors = 0;
            var warnings = 0;
            var audited = 0;

            foreach (var path in paths)
            {
                var side = AnimalSideProfile.FromAssetPath(path);
                if (side == null) continue;

                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                {
                    Debug.LogError($"{LogPrefix} Could not load {path}");
                    continue;
                }

                audited++;

                var findings = AnimalPrefabValidator.Validate(root, side, context);
                foreach (var finding in findings)
                {
                    if (finding.Severity == FindingSeverity.Error)
                    {
                        errors++;
                        Debug.LogError($"{LogPrefix} {finding}", finding.Context);
                    }
                    else
                    {
                        warnings++;
                        Debug.LogWarning($"{LogPrefix} {finding}", finding.Context);
                    }
                }
            }

            Debug.Log($"{LogPrefix} Audited {audited} prefabs: {errors} errors, {warnings} warnings.");
        }

        [MenuItem("Tools/Animals/Validate Selected Animal Prefab")]
        public static void ValidateSelectedAnimalPrefab()
        {
            var selected = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selected.Length == 0)
            {
                Debug.LogError($"{LogPrefix} Select one or more prefabs in the Project window first");
                return;
            }

            var paths = AssetDatabase.FindAssets("t:Prefab",
                    new[] { AnimalPrefabPaths.AnimalsFolder, AnimalPrefabPaths.EnemiesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToList();

            var context = AnimalPrefabAuditContext.Build(paths);
            var findings = new List<ValidationFinding>();

            foreach (var root in selected)
            {
                var path = AssetDatabase.GetAssetPath(root);
                var side = AnimalSideProfile.FromAssetPath(path);

                if (side == null)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} {root.name} is not inside {AnimalPrefabPaths.AnimalsFolder} or " +
                        $"{AnimalPrefabPaths.EnemiesFolder} - the side cannot be inferred, skipped",
                        root);
                    continue;
                }

                findings.AddRange(AnimalPrefabValidator.Validate(root, side, context));
            }

            foreach (var finding in findings)
            {
                if (finding.Severity == FindingSeverity.Error)
                    Debug.LogError($"{LogPrefix} {finding}", finding.Context);
                else
                    Debug.LogWarning($"{LogPrefix} {finding}", finding.Context);
            }

            var errors = findings.Count(f => f.Severity == FindingSeverity.Error);
            Debug.Log(
                $"{LogPrefix} Validated {selected.Length} prefabs: {errors} errors, " +
                $"{findings.Count - errors} warnings.");
        }
    }
}
#endif
