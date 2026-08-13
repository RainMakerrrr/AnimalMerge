#if UNITY_EDITOR
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class BattleSceneBuilder
    {
        private const string GridsParentName = "Grids";
        private const string AlliesParentName = "Allies";
        private const string EnemiesParentName = "Enemies";

        public static BattleSceneBuildReport Build(BattleSceneBuildRequest request)
        {
            var report = new BattleSceneBuildReport();

            if (request == null)
            {
                report.AddError("Build request is missing");
                return report;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Battle Scene");
            var undoGroup = Undo.GetCurrentGroup();

            Clear();

            var root = BattleSceneRoot.Create();

            var gridsReady = TryBuildGrids(request, root.transform, report);

            if (gridsReady && request.BuildEnemies)
            {
                EditorEnemyPlacer.PlaceStageEnemies(
                    request.StageConfig,
                    request.GameGrid,
                    BattleSceneRoot.CreateChild(root.transform, EnemiesParentName),
                    report);
            }

            if (gridsReady && request.BuildAllies)
            {
                EditorAllyPlacer.PlaceStartingPool(
                    request.PreBattleConfig,
                    request.MergeGrid,
                    BattleSceneRoot.CreateChild(root.transform, AlliesParentName),
                    report);
            }

            if (!gridsReady)
                report.AddError("Grid is not ready, units were skipped");

            Undo.CollapseUndoOperations(undoGroup);

            report.AddInfo($"Cells: {report.GridCells}, enemies: {report.Enemies}, allies: {report.Allies}");

            return report;
        }

        public static void Clear()
        {
            BattleSceneRoot.Destroy();

            foreach (var grid in UnityEngine.Object.FindObjectsOfType<GridManager>())
                GridManagerEditorAccess.ResetCells(grid);
        }

        private static bool TryBuildGrids(BattleSceneBuildRequest request, Transform root, BattleSceneBuildReport report)
        {
            var gridsParent = BattleSceneRoot.CreateChild(root, GridsParentName);

            var gameGridBuilt = TryBuildGrid(request.GameGrid, gridsParent, report);
            var mergeGridBuilt = TryBuildGrid(request.MergeGrid, gridsParent, report);

            return gameGridBuilt && mergeGridBuilt;
        }

        private static bool TryBuildGrid(GridManager grid, Transform parent, BattleSceneBuildReport report)
        {
            if (grid == null)
            {
                report.AddError("Grid manager reference is missing");
                return false;
            }

            var cells = EditorGridSpawner.Spawn(grid, BattleSceneRoot.CreateChild(parent, grid.name), report);

            if (cells == null)
                return false;

            if (!GridManagerEditorAccess.TrySeedCells(grid, cells))
            {
                report.AddError($"Failed to hand the spawned cells over to '{grid.name}'");
                return false;
            }

            return true;
        }
    }
}
#endif
