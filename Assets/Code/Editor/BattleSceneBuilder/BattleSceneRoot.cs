#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class BattleSceneRoot
    {
        public static GameObject Find()
        {
            var scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                if (rootObject.GetComponent<BattleSceneBuildRoot>() != null)
                    return rootObject;
            }

            foreach (var rootObject in rootObjects)
            {
                if (rootObject.name == BattleSceneBuilderPaths.RootObjectName)
                    return rootObject;
            }

            return null;
        }

        public static GameObject Create()
        {
            var root = new GameObject(BattleSceneBuilderPaths.RootObjectName);
            root.AddComponent<BattleSceneBuildRoot>();
            Undo.RegisterCreatedObjectUndo(root, "Create Battle Scene Root");

            return root;
        }

        public static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");

            return child.transform;
        }

        public static void Destroy()
        {
            var root = Find();

            if (root == null)
                return;

            Undo.DestroyObjectImmediate(root);
        }
    }
}
#endif
