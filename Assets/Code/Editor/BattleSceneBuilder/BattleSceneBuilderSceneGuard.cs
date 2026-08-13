#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Editor.BattleSceneBuilder
{
    [InitializeOnLoad]
    internal static class BattleSceneBuilderSceneGuard
    {
        static BattleSceneBuilderSceneGuard()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange != PlayModeStateChange.ExitingEditMode)
                return;

            if (BattleSceneRoot.Find() == null)
                return;

            BattleSceneBuilder.Clear();
            Debug.Log("[SceneBuilder] Removed the edit mode preview before entering play mode");
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            var root = BattleSceneRoot.Find();

            if (root == null || root.scene != scene)
                return;

            BattleSceneBuilder.Clear();
            Debug.Log("[SceneBuilder] Removed the edit mode preview before saving the scene");
        }
    }
}
#endif
