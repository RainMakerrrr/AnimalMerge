#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Code.Battle.Config;
using Code.Infrastructure;
using Code.Levels;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.LevelSets
{
    public static class LevelSetFactory
    {
        private const string DisplayNameField = "_displayName";
        private const string DescriptionField = "_description";
        private const string PreBattleConfigField = "_preBattleConfig";
        private const string TutorialLevelsField = "_tutorialLevels";
        private const string LevelsField = "_levels";
        private const string StageNumberField = "_stageNumber";

        public static LevelSet CreateSet(string setName)
        {
            LevelSetFolders.EnsureSetFolders(setName);

            var path = AssetDatabase.GenerateUniqueAssetPath(LevelSetPaths.SetAsset(setName));
            var set = ScriptableObject.CreateInstance<LevelSet>();

            AssetDatabase.CreateAsset(set, path);

            var serialized = new SerializedObject(set);
            serialized.FindProperty(DisplayNameField).stringValue = setName;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            LevelSetLibraryAccess.Register(set);

            return set;
        }

        public static LevelStageConfig AddStage(LevelSet set, ExtendedLevel level)
        {
            if (set == null || level == null)
                return null;

            var folder = StagesFolderOf(set);
            LevelSetFolders.Ensure(folder);

            var stageNumber = level.Stages == null ? 1 : level.Stages.Length + 1;
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{level.name}_Stage_{stageNumber}.asset");

            var stage = ScriptableObject.CreateInstance<LevelStageConfig>();
            AssetDatabase.CreateAsset(stage, path);

            var serialized = new SerializedObject(stage);
            serialized.FindProperty(StageNumberField).intValue = stageNumber;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            LevelPrefabWriter.AppendStage(level, stage);
            AssetDatabase.SaveAssets();

            return stage;
        }

        public static LevelSet DuplicateSet(LevelSet source, string newName, List<string> report)
        {
            if (source == null)
            {
                report.Add("[Error] No source set to duplicate");
                return null;
            }

            LevelSetFolders.EnsureSetFolders(newName);

            var stageCopies = new Dictionary<LevelStageConfig, LevelStageConfig>();
            var tutorialCopies = CopyLevels(source.TutorialLevels, newName, stageCopies, report);
            var levelCopies = CopyLevels(source.Levels, newName, stageCopies, report);

            var setPath = AssetDatabase.GenerateUniqueAssetPath(LevelSetPaths.SetAsset(newName));
            var set = ScriptableObject.CreateInstance<LevelSet>();

            AssetDatabase.CreateAsset(set, setPath);

            var serialized = new SerializedObject(set);
            serialized.FindProperty(DisplayNameField).stringValue = newName;
            serialized.FindProperty(DescriptionField).stringValue = source.Description;
            serialized.FindProperty(PreBattleConfigField).objectReferenceValue = source.PreBattleConfig;

            WriteLevels(serialized.FindProperty(TutorialLevelsField), tutorialCopies);
            WriteLevels(serialized.FindProperty(LevelsField), levelCopies);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            LevelSetLibraryAccess.Register(set);

            report.Add(
                $"[Info] {newName}: copied {levelCopies.Count} of {source.Levels.Count} levels and " +
                $"{tutorialCopies.Count} of {source.TutorialLevels.Count} tutorial levels");

            return set;
        }

        public static bool DeleteSet(LevelSet set, List<string> report)
        {
            if (set == null)
            {
                report.Add("[Error] No set selected to delete");
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(set);

            if (string.IsNullOrEmpty(assetPath))
            {
                report.Add("[Error] The selected set is not saved as an asset");
                return false;
            }

            var folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var ownsFolder = folder.StartsWith($"{LevelSetPaths.SetsRoot}/") && CountSetsIn(folder) == 1;

            if (ownsFolder && !CanDeleteFolder(set, folder, report))
                return false;

            LevelSetLibraryAccess.Unregister(set);

            var deletedPath = ownsFolder ? folder : assetPath;

            if (!AssetDatabase.DeleteAsset(deletedPath))
            {
                report.Add($"[Error] Unity refused to delete {deletedPath}");
                return false;
            }

            AssetDatabase.SaveAssets();
            report.Add($"[Info] Deleted {deletedPath}");

            if (!ownsFolder)
                report.Add($"[Info] Kept {folder} — it holds other sets or lives outside {LevelSetPaths.SetsRoot}");

            return true;
        }

        public static string LevelsFolderOf(LevelSet set) =>
            $"{SetFolderOf(set)}/{LevelSetPaths.LevelsSubfolder}";

        public static string StagesFolderOf(LevelSet set) =>
            $"{SetFolderOf(set)}/{LevelSetPaths.StagesSubfolder}";

        private static int CountSetsIn(string folder) =>
            AssetDatabase.FindAssets("t:LevelSet", new[] { folder }).Length;

        private static bool CanDeleteFolder(LevelSet set, string folder, List<string> report)
        {
            var prefix = $"{folder}/";
            var conflicts = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:LevelSet"))
            {
                var other = AssetDatabase.LoadAssetAtPath<LevelSet>(AssetDatabase.GUIDToAssetPath(guid));

                if (other == null || other == set)
                    continue;

                conflicts += ReportSharedLevels(other, other.Levels, prefix, report);
                conflicts += ReportSharedLevels(other, other.TutorialLevels, prefix, report);
            }

            if (conflicts == 0)
                return true;

            report.Add("[Error] Nothing deleted — detach the shared levels first, or delete those sets too");
            return false;
        }

        private static int ReportSharedLevels(
            LevelSet other,
            IReadOnlyList<ExtendedLevel> levels,
            string prefix,
            List<string> report)
        {
            var found = 0;

            for (var index = 0; index < levels.Count; index++)
            {
                var level = levels[index];

                if (level == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(level);

                if (!path.StartsWith(prefix))
                    continue;

                report.Add($"[Error] {other.DisplayName} still uses {path}");
                found++;
            }

            return found;
        }

        private static string SetFolderOf(LevelSet set)
        {
            var path = AssetDatabase.GetAssetPath(set);

            if (string.IsNullOrEmpty(path))
                return LevelSetPaths.SetFolder(set == null ? LevelSetPaths.StandardSetName : set.name);

            return Path.GetDirectoryName(path).Replace('\\', '/');
        }

        private static List<ExtendedLevel> CopyLevels(
            IReadOnlyList<ExtendedLevel> levels,
            string setName,
            Dictionary<LevelStageConfig, LevelStageConfig> stageCopies,
            List<string> report)
        {
            var copies = new List<ExtendedLevel>();

            for (var index = 0; index < levels.Count; index++)
            {
                var copy = CopyLevel(levels[index], setName, stageCopies, report);

                if (copy == null)
                    continue;

                copies.Add(copy);
            }

            return copies;
        }

        private static ExtendedLevel CopyLevel(
            ExtendedLevel level,
            string setName,
            Dictionary<LevelStageConfig, LevelStageConfig> stageCopies,
            List<string> report)
        {
            if (level == null)
            {
                report.Add("[Error] An empty entry in the source set was skipped");
                return null;
            }

            var sourcePath = AssetDatabase.GetAssetPath(level);

            if (string.IsNullOrEmpty(sourcePath))
            {
                report.Add($"[Error] '{level.name}' is not a prefab asset and was skipped");
                return null;
            }

            var targetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{LevelSetPaths.LevelsFolder(setName)}/{Path.GetFileName(sourcePath)}");

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                report.Add($"[Error] Failed to copy {sourcePath} to {targetPath}, the level was skipped");
                return null;
            }

            var copyRoot = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            var copy = copyRoot == null ? null : copyRoot.GetComponent<ExtendedLevel>();

            if (copy == null)
            {
                report.Add($"[Error] {targetPath} has no ExtendedLevel on its root, the level was skipped");
                return null;
            }

            RebindStages(copy, setName, stageCopies, report);
            LevelPrefabWriter.SetId(copy, copyRoot.name);

            return copy;
        }

        private static void RebindStages(
            ExtendedLevel copy,
            string setName,
            Dictionary<LevelStageConfig, LevelStageConfig> stageCopies,
            List<string> report)
        {
            var serialized = new SerializedObject(copy);
            var stages = serialized.FindProperty(LevelPrefabWriter.StagesField);

            for (var index = 0; index < stages.arraySize; index++)
            {
                var element = stages.GetArrayElementAtIndex(index);
                var sourceStage = element.objectReferenceValue as LevelStageConfig;

                if (sourceStage == null)
                    continue;

                element.objectReferenceValue = CopyStage(sourceStage, setName, stageCopies, report);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            LevelPrefabWriter.Save(copy);
        }

        private static LevelStageConfig CopyStage(
            LevelStageConfig sourceStage,
            string setName,
            Dictionary<LevelStageConfig, LevelStageConfig> stageCopies,
            List<string> report)
        {
            if (stageCopies.TryGetValue(sourceStage, out var alreadyCopied))
                return alreadyCopied;

            var sourcePath = AssetDatabase.GetAssetPath(sourceStage);
            var targetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{LevelSetPaths.StagesFolder(setName)}/{Path.GetFileName(sourcePath)}");

            LevelStageConfig copy = null;

            if (AssetDatabase.CopyAsset(sourcePath, targetPath))
                copy = AssetDatabase.LoadAssetAtPath<LevelStageConfig>(targetPath);
            else
                report.Add($"[Error] Failed to copy {sourcePath} to {targetPath}, the stage is missing in the copy");

            stageCopies[sourceStage] = copy;

            return copy;
        }

        private static void WriteLevels(SerializedProperty property, List<ExtendedLevel> levels)
        {
            property.arraySize = levels.Count;

            for (var index = 0; index < levels.Count; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = levels[index];
        }
    }
}
#endif
