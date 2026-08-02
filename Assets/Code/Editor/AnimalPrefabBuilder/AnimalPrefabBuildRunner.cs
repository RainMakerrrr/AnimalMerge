#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalPrefabBuildRunner
    {
        private const string LogPrefix = "[AnimalPrefabTool]";

        public static bool Build(AnimalRecipe recipe, bool askBeforeOverwrite)
        {
            if (recipe == null) return false;

            if (recipe.FacadeScript == null
                && EnemyFacadeScriptGenerator.TryGenerate(recipe, out _))
            {
                return false;
            }

            var findings = RecipePreflight.Run(recipe);
            if (Report(recipe, findings, "preflight")) return false;

            var outputPath = recipe.OutputPath;
            if (askBeforeOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
            {
                var proceed = EditorUtility.DisplayDialog(
                    $"Rebuild {recipe.PrefabName}?",
                    $"{outputPath} already exists and will be replaced. Any hand tuning made directly on the " +
                    "prefab is lost. The stats asset and the AnimalDatabase row are preserved.",
                    "Rebuild",
                    "Cancel");

                if (!proceed) return false;
            }

            var buildFindings = new List<ValidationFinding>();
            var root = AnimalPrefabAssembler.Assemble(recipe, buildFindings);

            if (root == null)
            {
                Report(recipe, buildFindings, "assembly");
                return false;
            }

            EnsureFolder(recipe.Side.OutputFolder);

            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, outputPath, out var saved);
                if (!saved)
                {
                    Debug.LogError($"{LogPrefix} {recipe.PrefabName}: SaveAsPrefabAsset reported failure");
                    return false;
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            var stats = AnimalDatabaseWriter.ResolveOrCreateStats(recipe, buildFindings);
            if (stats != null) AnimalDatabaseWriter.EnsureDatabaseRow(recipe, stats, buildFindings);

            AssetDatabase.SaveAssets();

            var saved2 = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (saved2 == null)
            {
                Debug.LogError($"{LogPrefix} {recipe.PrefabName}: the saved prefab could not be read back");
                return false;
            }

            buildFindings.AddRange(AnimalPrefabValidator.Validate(saved2, recipe.Side, null));
            var blocked = Report(recipe, buildFindings, "post-save validation");

            Debug.Log($"{LogPrefix} Built {outputPath}", saved2);
            return !blocked;
        }

        private static bool Report(AnimalRecipe recipe, List<ValidationFinding> findings, string stage)
        {
            var hasError = false;

            foreach (var finding in findings)
            {
                if (finding.Severity == FindingSeverity.Error)
                {
                    hasError = true;
                    Debug.LogError($"{LogPrefix} {stage}: {finding}", finding.Context);
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} {stage}: {finding}", finding.Context);
                }
            }

            if (hasError)
                Debug.LogError($"{LogPrefix} {recipe.PrefabName}: {stage} failed, nothing was written");

            return hasError;
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
