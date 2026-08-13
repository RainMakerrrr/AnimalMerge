#if UNITY_EDITOR
using Code.Animals;
using Code.Battle.Config;
using Code.GridPathfinding;
using Code.Infrastructure.Installers;
using UnityEditor;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class BattleSceneReferenceResolver
    {
        private const string InstallerMergeGridFieldName = "_mergeGridManager";
        private const string InstallerGameGridFieldName = "_gameGridManager";
        private const string SpawnerMergeGridFieldName = "_mergeGrid";
        private const string SpawnerGameGridFieldName = "_gameGrid";
        private const int MaxMergeGridHeight = 2;

        public static bool TryResolveGrids(out GridManager mergeGrid, out GridManager gameGrid)
        {
            var installer = UnityEngine.Object.FindObjectOfType<PathfindingInstaller>();

            if (TryReadGrids(installer, InstallerMergeGridFieldName, InstallerGameGridFieldName, out mergeGrid, out gameGrid))
                return true;

            var spawner = UnityEngine.Object.FindObjectOfType<AnimalSpawner>();

            if (TryReadGrids(spawner, SpawnerMergeGridFieldName, SpawnerGameGridFieldName, out mergeGrid, out gameGrid))
                return true;

            return TryResolveByHeight(out mergeGrid, out gameGrid);
        }

        public static PreBattleConfig LoadPreBattleConfig()
        {
            return AssetDatabase.LoadAssetAtPath<PreBattleConfig>(BattleSceneBuilderPaths.PreBattleConfigAsset);
        }

        private static bool TryReadGrids(
            UnityEngine.Object source,
            string mergeGridFieldName,
            string gameGridFieldName,
            out GridManager mergeGrid,
            out GridManager gameGrid)
        {
            mergeGrid = null;
            gameGrid = null;

            if (source == null)
                return false;

            var serializedSource = new SerializedObject(source);
            var mergeGridProperty = serializedSource.FindProperty(mergeGridFieldName);
            var gameGridProperty = serializedSource.FindProperty(gameGridFieldName);

            if (mergeGridProperty == null || gameGridProperty == null)
                return false;

            mergeGrid = mergeGridProperty.objectReferenceValue as GridManager;
            gameGrid = gameGridProperty.objectReferenceValue as GridManager;

            return mergeGrid != null && gameGrid != null;
        }

        private static bool TryResolveByHeight(out GridManager mergeGrid, out GridManager gameGrid)
        {
            mergeGrid = null;
            gameGrid = null;

            foreach (var grid in UnityEngine.Object.FindObjectsOfType<GridManager>())
            {
                if (grid.Height <= MaxMergeGridHeight)
                    mergeGrid = grid;
                else
                    gameGrid = grid;
            }

            return mergeGrid != null && gameGrid != null;
        }
    }
}
#endif
