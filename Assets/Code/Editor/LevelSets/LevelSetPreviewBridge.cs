#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Battle.Config;
using UnityEditor;

namespace Code.Editor.LevelSets
{
    internal static class LevelSetPreviewBridge
    {
        public static bool IsAvailable => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static IReadOnlyList<string> PreviewStage(
            LevelStageConfig stage, PreBattleConfig preBattleConfig, int level)
        {
            if (stage == null)
                return new[] {"[Error] No stage selected"};

            if (!BattleSceneBuilder.BattleSceneReferenceResolver.TryResolveGameGrid(out var gameGrid))
                return new[] {"[Error] No GridManager in the open scene, open the battle scene first"};

            var request = new BattleSceneBuilder.BattleSceneBuildRequest
            {
                StageConfig = stage,
                PreBattleConfig = preBattleConfig != null
                    ? preBattleConfig
                    : BattleSceneBuilder.BattleSceneReferenceResolver.LoadPreBattleConfig(),
                GameGrid = gameGrid,
                Level = level,
                BuildEnemies = true,
                BuildAllies = false
            };

            return BattleSceneBuilder.BattleSceneBuilder.Build(request).Messages;
        }

        public static void ClearPreview()
        {
            BattleSceneBuilder.BattleSceneBuilder.Clear();
        }
    }
}
#endif
