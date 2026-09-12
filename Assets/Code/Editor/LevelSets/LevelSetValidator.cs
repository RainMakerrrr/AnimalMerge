#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Animals.Merge;
using Code.Animals.Movement;
using Code.Battle.Config;
using Code.Data.Animals;
using Code.Editor.AnimalPrefabBuilder;
using Code.GridPathfinding;
using Code.GridPathfinding.Config;
using Code.Infrastructure;
using Code.Levels;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.LevelSets
{
    public static class LevelSetValidator
    {
        private const string ConfigsField = "_configs";
        private const string TypeField = "Type";

        public static List<ValidationFinding> ValidateAll()
        {
            var findings = new List<ValidationFinding>();

            foreach (var set in LevelSetCatalog.FindAll())
                findings.AddRange(Validate(set));

            return findings;
        }

        public static List<ValidationFinding> Validate(LevelSet set)
        {
            var findings = new List<ValidationFinding>();

            if (set == null)
                return findings;

            var grid = ResolveGridConfig(set, findings);
            var databaseTypes = ResolveDatabaseTypes(set, findings);

            ValidateLibraryRegistration(set, findings);

            if (set.Levels.Count == 0)
                findings.Add(ValidationFinding.Error(
                    "L01", $"{set.name}: the set has no levels, the game cannot load anything from it", set));

            ValidateLevels(set, set.TutorialLevels, "_tutorialLevels", grid, databaseTypes, findings);
            ValidateLevels(set, set.Levels, "_levels", grid, databaseTypes, findings);

            return findings;
        }

        private static void ValidateLibraryRegistration(LevelSet set, List<ValidationFinding> findings)
        {
            if (LevelSetLibraryAccess.DefaultSet() != set)
                return;

            if (LevelSetLibraryAccess.IsRegistered(set))
                return;

            findings.Add(ValidationFinding.Error(
                "L16",
                $"{set.name} is the default set but is missing from {LevelSetPaths.LibraryAsset}, " +
                "so a player build will not contain it",
                set));
        }

        private static void ValidateLevels(
            LevelSet set,
            IReadOnlyList<ExtendedLevel> levels,
            string fieldName,
            GridConfig grid,
            HashSet<int> databaseTypes,
            List<ValidationFinding> findings)
        {
            var seenLevels = new HashSet<ExtendedLevel>();
            var seenIds = new HashSet<string>();

            for (var index = 0; index < levels.Count; index++)
            {
                var level = levels[index];

                if (level == null)
                {
                    findings.Add(ValidationFinding.Error(
                        "L02", $"{set.name}: {fieldName}[{index}] is empty", set));
                    continue;
                }

                if (!seenLevels.Add(level))
                    findings.Add(ValidationFinding.Warning(
                        "L06", $"{set.name}: {fieldName}[{index}] repeats {level.name}", level));

                ValidateLevelAsset(set, level, fieldName, index, findings);
                ValidateLevelId(set, level, seenIds, findings);
                ValidateStages(set, level, grid, databaseTypes, findings);
            }
        }

        private static void ValidateLevelAsset(
            LevelSet set, ExtendedLevel level, string fieldName, int index, List<ValidationFinding> findings)
        {
            var path = AssetDatabase.GetAssetPath(level);

            if (string.IsNullOrEmpty(path))
            {
                findings.Add(ValidationFinding.Error(
                    "L03",
                    $"{set.name}: {fieldName}[{index}] ({level.name}) is not a prefab asset, " +
                    "the level factory can only instantiate prefabs",
                    level));
                return;
            }

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (root != level.gameObject)
                findings.Add(ValidationFinding.Error(
                    "L03",
                    $"{set.name}: {fieldName}[{index}] ({level.name}) is not on the prefab root of {path}",
                    level));

            if (path.StartsWith(LevelSetPaths.ResourcesFolder))
                findings.Add(ValidationFinding.Warning(
                    "L17",
                    $"{set.name}: {level.name} lives in {path}. Level sets reference prefabs directly, " +
                    "so it no longer has to sit under Resources",
                    level));
        }

        private static void ValidateLevelId(
            LevelSet set, ExtendedLevel level, HashSet<string> seenIds, List<ValidationFinding> findings)
        {
            var id = LevelPrefabWriter.GetId(level);

            if (string.IsNullOrWhiteSpace(id))
            {
                findings.Add(ValidationFinding.Warning(
                    "L05", $"{set.name}: {level.name} has an empty id, analytics will report a blank level", level));
                return;
            }

            if (!seenIds.Add(id))
                findings.Add(ValidationFinding.Warning(
                    "L05", $"{set.name}: id '{id}' is used by more than one level", level));
        }

        private static void ValidateStages(
            LevelSet set,
            ExtendedLevel level,
            GridConfig grid,
            HashSet<int> databaseTypes,
            List<ValidationFinding> findings)
        {
            var stages = level.Stages;

            if (stages == null || stages.Length == 0)
            {
                findings.Add(ValidationFinding.Error(
                    "L04", $"{set.name}: {level.name} has no stages, the battle loop cannot start", level));
                return;
            }

            for (var index = 0; index < stages.Length; index++)
            {
                var stage = stages[index];

                if (stage == null)
                {
                    findings.Add(ValidationFinding.Error(
                        "L04", $"{set.name}: {level.name} stage #{index + 1} is empty", level));
                    continue;
                }

                if (stage.StageNumber != index + 1)
                    findings.Add(ValidationFinding.Warning(
                        "L14",
                        $"{set.name}: {level.name} stage #{index + 1} reports StageNumber {stage.StageNumber}, " +
                        "which only shows up in battle logs but makes them hard to read",
                        stage));

                ValidateEnemies(set, level, stage, grid, databaseTypes, findings);
                ValidateBossFlags(set, level, stage, findings);
            }
        }

        private static void ValidateBossFlags(
            LevelSet set, ExtendedLevel level, LevelStageConfig stage, List<ValidationFinding> findings)
        {
            var hasBossEnemy = false;

            if (stage.Enemies != null)
            {
                for (var index = 0; index < stage.Enemies.Length; index++)
                {
                    if (stage.Enemies[index] != null && stage.Enemies[index].IsBoss)
                        hasBossEnemy = true;
                }
            }

            if (stage.IsBossStage == hasBossEnemy)
                return;

            findings.Add(ValidationFinding.Warning(
                "L15",
                $"{set.name}: {level.name} / {stage.name} has IsBossStage={stage.IsBossStage} " +
                $"but {(hasBossEnemy ? "an enemy is" : "no enemy is")} flagged as a boss. " +
                "Only the per-enemy flag drives gameplay, the stage flag is log-only",
                stage));
        }

        private static void ValidateEnemies(
            LevelSet set,
            ExtendedLevel level,
            LevelStageConfig stage,
            GridConfig grid,
            HashSet<int> databaseTypes,
            List<ValidationFinding> findings)
        {
            if (stage.Enemies == null)
                return;

            var occupancy = new Dictionary<Vector2Int, int>();
            var stageLabel = $"{set.name}: {level.name} / {stage.name}";

            for (var index = 0; index < stage.Enemies.Length; index++)
            {
                var enemy = stage.Enemies[index];

                if (enemy == null || enemy.Prefab == null)
                {
                    findings.Add(ValidationFinding.Error(
                        "L07", $"{stageLabel}: enemy #{index} has no prefab", stage));
                    continue;
                }

                var movement = enemy.Prefab.Movement;

                if (movement == null)
                {
                    findings.Add(ValidationFinding.Error(
                        "L08",
                        $"{stageLabel}: enemy #{index} ({enemy.Prefab.name}) has no AnimalMovement, " +
                        "it cannot be placed on the grid",
                        enemy.Prefab));
                    continue;
                }

                ValidateEnemyType(enemy.Prefab, stageLabel, index, databaseTypes, findings);
                ValidateEnemyMergeability(enemy.Prefab, stageLabel, index, findings);

                if (grid == null)
                    continue;

                ValidateEnemyPlacement(enemy, movement, stage, stageLabel, index, grid, occupancy, findings);
            }
        }

        private static void ValidateEnemyType(
            AnimalFacade prefab,
            string stageLabel,
            int index,
            HashSet<int> databaseTypes,
            List<ValidationFinding> findings)
        {
            if (databaseTypes == null || databaseTypes.Contains((int)prefab.Type))
                return;

            findings.Add(ValidationFinding.Warning(
                "L13",
                $"{stageLabel}: enemy #{index} ({prefab.name}) is {prefab.Type}, which has no AnimalDatabase row, " +
                "so it will spawn without stats",
                prefab));
        }

        private static void ValidateEnemyMergeability(
            AnimalFacade prefab,
            string stageLabel,
            int index,
            List<ValidationFinding> findings)
        {
            var mergeTargets = prefab.GetComponentsInChildren<MergeTarget>(true).Length;

            if (mergeTargets == 0)
                return;

            findings.Add(ValidationFinding.Warning(
                "L18",
                $"{stageLabel}: enemy #{index} ({prefab.name}) carries {mergeTargets} MergeTarget " +
                $"component(s) because it is a {prefab.GetType().Name} reused as an enemy, so the " +
                "player can treat it as a merge target",
                prefab));
        }

        private static void ValidateEnemyPlacement(
            StageEnemyConfig enemy,
            AnimalMovement movement,
            LevelStageConfig stage,
            string stageLabel,
            int index,
            GridConfig grid,
            Dictionary<Vector2Int, int> occupancy,
            List<ValidationFinding> findings)
        {
            var anchor = enemy.GridPosition;
            var size = movement.UnitSize;
            var direction = movement.Direction;

            var isInBounds = anchor.x >= 0 && anchor.x < grid.Width && anchor.y >= 0 && anchor.y < grid.Height;

            if (!isInBounds)
            {
                findings.Add(ValidationFinding.Error(
                    "L09",
                    $"{stageLabel}: enemy #{index} sits at {anchor}, outside the " +
                    $"{grid.Width}x{grid.Height} grid",
                    stage));
                return;
            }

            var adjustedAnchor = UnitFootprint.AdjustAnchor(anchor, size, direction, grid.Width, grid.Height);

            if (adjustedAnchor != anchor)
                findings.Add(ValidationFinding.Error(
                    "L10",
                    $"{stageLabel}: enemy #{index} ({enemy.Prefab.name}, {size}, {direction}) does not fit " +
                    $"from {anchor}. The grid silently shifts its cells to {adjustedAnchor} while the model " +
                    "stays at the requested anchor",
                    stage));

            var cells = UnitFootprint.Cells(anchor, size, direction, grid.Width, grid.Height);

            for (var cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                var cell = cells[cellIndex];

                if (occupancy.TryGetValue(cell, out var otherIndex))
                    findings.Add(ValidationFinding.Error(
                        "L11",
                        $"{stageLabel}: enemy #{index} overlaps enemy #{otherIndex} at {cell}. " +
                        "Nothing checks this at spawn time, both units will be placed anyway",
                        stage));
                else
                    occupancy[cell] = index;

                if (DeploymentZone.Contains(cell.y))
                    findings.Add(ValidationFinding.Error(
                        "L12",
                        $"{stageLabel}: enemy #{index} occupies {cell}, inside the deployment zone " +
                        $"(rows 0-{DeploymentZone.Depth - 1}) reserved for allies",
                        stage));
            }
        }

        private static GridConfig ResolveGridConfig(LevelSet set, List<ValidationFinding> findings)
        {
            var guids = AssetDatabase.FindAssets("t:GridConfig");

            if (guids.Length == 0)
            {
                findings.Add(ValidationFinding.Warning(
                    "L00", "No GridConfig asset found, enemy positions were not checked against grid bounds", set));
                return null;
            }

            if (guids.Length > 1)
                findings.Add(ValidationFinding.Warning(
                    "L00",
                    $"{guids.Length} GridConfig assets exist. Bounds were checked against the first one, " +
                    "and PreBattleConfig validation picks the first one too",
                    set));

            return AssetDatabase.LoadAssetAtPath<GridConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static HashSet<int> ResolveDatabaseTypes(LevelSet set, List<ValidationFinding> findings)
        {
            var database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>(AnimalPrefabPaths.AnimalDatabaseAsset);

            if (database == null)
            {
                findings.Add(ValidationFinding.Warning(
                    "L13",
                    $"AnimalDatabase not found at {AnimalPrefabPaths.AnimalDatabaseAsset}, enemy stats were not checked",
                    set));
                return null;
            }

            var configs = new SerializedObject(database).FindProperty(ConfigsField);

            if (configs == null || !configs.isArray)
                return null;

            var types = new HashSet<int>();

            for (var index = 0; index < configs.arraySize; index++)
            {
                var type = configs.GetArrayElementAtIndex(index).FindPropertyRelative(TypeField);

                if (type != null)
                    types.Add(type.intValue);
            }

            return types;
        }
    }
}
#endif
