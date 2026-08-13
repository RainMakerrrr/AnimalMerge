#if UNITY_EDITOR
using Code.Battle.Config;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class EditorEnemyPlacer
    {
        private const string EnemyLayerName = "Enemy";

        public static void PlaceStageEnemies(
            LevelStageConfig stageConfig,
            GridManager gameGrid,
            Transform parent,
            BattleSceneBuildReport report)
        {
            if (stageConfig == null)
            {
                report.AddError("Level stage config is not assigned, enemies were skipped");
                return;
            }

            if (gameGrid == null)
            {
                report.AddError("Game grid reference is missing, enemies were skipped");
                return;
            }

            if (stageConfig.Enemies == null || stageConfig.Enemies.Length == 0)
            {
                report.AddWarning($"Stage '{stageConfig.name}' has no enemies");
                return;
            }

            var enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

            for (int index = 0; index < stageConfig.Enemies.Length; index++)
            {
                var enemyConfig = stageConfig.Enemies[index];

                if (enemyConfig == null || enemyConfig.Prefab == null)
                {
                    report.AddWarning($"Enemy #{index} has no prefab, skipped");
                    continue;
                }

                var targetCell = gameGrid.GetCell(enemyConfig.GridPosition) as GridCell;

                if (targetCell == null)
                {
                    report.AddError($"No grid cell at {enemyConfig.GridPosition} for enemy #{index}");
                    continue;
                }

                var enemy = EditorUnitSpawner.Instantiate(enemyConfig.Prefab, parent);

                if (enemy == null || enemy.Movement == null)
                {
                    report.AddError($"Failed to instantiate enemy #{index} ({enemyConfig.Prefab.name})");
                    continue;
                }

                enemy.name += $"_{index}";

                if (enemyLayer >= 0)
                    enemy.gameObject.layer = enemyLayer;

                EditorUnitSpawner.PlaceAt(enemy, targetCell);
                gameGrid.SetOccupied(targetCell.GridPosition, enemy.Movement.UnitSize, enemy.Movement.Direction, null);
                report.Enemies++;
            }
        }
    }
}
#endif
