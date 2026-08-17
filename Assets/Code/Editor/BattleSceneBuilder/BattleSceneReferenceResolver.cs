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
        private const string InstallerGameGridFieldName = "_gameGridManager";
        private const string SpawnerGameGridFieldName = "_gameGrid";

        public static bool TryResolveGameGrid(out GridManager gameGrid)
        {
            var installer = UnityEngine.Object.FindObjectOfType<PathfindingInstaller>();

            if (TryReadGrid(installer, InstallerGameGridFieldName, out gameGrid))
                return true;

            var spawner = UnityEngine.Object.FindObjectOfType<AnimalSpawner>();

            if (TryReadGrid(spawner, SpawnerGameGridFieldName, out gameGrid))
                return true;

            return TryResolveFromScene(out gameGrid);
        }

        public static PreBattleConfig LoadPreBattleConfig()
        {
            return AssetDatabase.LoadAssetAtPath<PreBattleConfig>(BattleSceneBuilderPaths.PreBattleConfigAsset);
        }

        private static bool TryReadGrid(UnityEngine.Object source, string gameGridFieldName, out GridManager gameGrid)
        {
            gameGrid = null;

            if (source == null)
                return false;

            var serializedSource = new SerializedObject(source);
            var gameGridProperty = serializedSource.FindProperty(gameGridFieldName);

            if (gameGridProperty == null)
                return false;

            gameGrid = gameGridProperty.objectReferenceValue as GridManager;

            return gameGrid != null;
        }

        private static bool TryResolveFromScene(out GridManager gameGrid)
        {
            var grids = UnityEngine.Object.FindObjectsOfType<GridManager>();

            gameGrid = grids.Length == 1 ? grids[0] : null;

            return gameGrid != null;
        }
    }
}
#endif
