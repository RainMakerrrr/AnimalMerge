#if UNITY_EDITOR
using Code.Animals;
using Code.Animals.Facades;
using Code.Battle.Config;
using Code.Battle.PreBattle;
using Code.GridPathfinding;
using Framework.Code;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class EditorAllyPlacer
    {
        public static void PlaceStartingPool(
            PreBattleConfig config,
            int level,
            GridManager grid,
            Transform parent,
            BattleSceneBuildReport report)
        {
            if (config == null)
            {
                report.AddError("PreBattleConfig is not assigned, allies were skipped");
                return;
            }

            if (grid == null)
            {
                report.AddError("Grid reference is missing, allies were skipped");
                return;
            }

            var roster = AnimalRosterResolver.Resolve(config, level);

            if (roster.Count == 0)
            {
                report.AddWarning("Animal roster is empty, no allies to place");
                return;
            }

            var prefabsByType = AllyPrefabCatalog.Load();

            if (prefabsByType.Count == 0)
            {
                report.AddError($"No ally prefabs found in Resources/{AssetPath.Animals}");
                return;
            }

            foreach (var animalType in roster)
            {
                if (!prefabsByType.TryGetValue(animalType, out var prefab))
                {
                    report.AddError($"No prefab registered for {animalType}");
                    continue;
                }

                if (animalType == AnimalType.Chicken)
                {
                    PlaceChickenFlock(prefab, grid, parent, report);
                    continue;
                }

                PlaceSingle(prefab, animalType, grid, parent, report);
            }
        }

        private static void PlaceSingle(
            AnimalFacade prefab,
            AnimalType animalType,
            GridManager grid,
            Transform parent,
            BattleSceneBuildReport report)
        {
            if (!grid.HasCellFor(animalType))
            {
                report.AddWarning($"No free cell for {animalType}, skipped");
                return;
            }

            var unit = EditorUnitSpawner.Instantiate(prefab, parent);

            if (unit == null || unit.Movement == null)
            {
                report.AddError($"Failed to instantiate {animalType}");
                return;
            }

            var unitSize = unit.Movement.UnitSize;
            var direction = unit.Movement.Direction;

            if (!grid.TryFindFreePlacement(unitSize, direction, out var anchor))
            {
                report.AddWarning($"No free {unitSize} placement for {animalType}, skipped");
                Undo.DestroyObjectImmediate(unit.gameObject);
                return;
            }

            var anchorCell = grid.GetCell(anchor) as GridCell;

            if (anchorCell == null)
            {
                report.AddError($"No grid cell at {anchor} for {animalType}");
                Undo.DestroyObjectImmediate(unit.gameObject);
                return;
            }

            EditorUnitSpawner.PlaceAt(unit, anchorCell);
            grid.SetOccupied(anchorCell.GridPosition, unitSize, direction, null);
            report.Allies++;
        }

        private static void PlaceChickenFlock(
            AnimalFacade prefab,
            GridManager grid,
            Transform parent,
            BattleSceneBuildReport report)
        {
            if (!grid.TryFindFreePlacement(ChickenFlock.Footprint, Direction.North, out var anchor))
            {
                report.AddWarning($"No free {ChickenFlock.Footprint} block for {AnimalType.Chicken}, skipped");
                return;
            }

            var flockCells = grid.GetOccupiedCells(anchor, ChickenFlock.Footprint, Direction.North);

            if (flockCells.Count != ChickenFlock.Count)
            {
                report.AddWarning($"Chicken flock needs {ChickenFlock.Count} cells, found {flockCells.Count}, skipped");
                return;
            }

            foreach (var flockCell in flockCells)
            {
                var chicken = EditorUnitSpawner.Instantiate(prefab, parent);

                if (chicken == null)
                {
                    report.AddError($"Failed to instantiate {AnimalType.Chicken}");
                    continue;
                }

                EditorUnitSpawner.PlaceAt(chicken, flockCell as GridCell);
                report.Allies++;
            }

            grid.SetOccupied(anchor, ChickenFlock.Footprint, Direction.North, null);
        }
    }
}
#endif
