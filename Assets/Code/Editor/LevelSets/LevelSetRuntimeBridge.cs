#if UNITY_EDITOR
using Code.Levels;
using UnityEngine;
using Zenject;

namespace Code.Editor.LevelSets
{
    internal static class LevelSetRuntimeBridge
    {
        public static bool TryResolveSwitchService(out ILevelSetSwitchService switchService, out string problem)
        {
            switchService = null;
            problem = null;

            var contexts = Object.FindObjectsOfType<SceneContext>();

            if (contexts.Length == 0)
            {
                problem = "No SceneContext in the running scene";
                return false;
            }

            if (contexts.Length > 1)
            {
                problem = $"{contexts.Length} SceneContexts are loaded, cannot tell which one owns the game";
                return false;
            }

            var container = contexts[0].Container;

            if (container == null)
            {
                problem = "SceneContext has no container yet";
                return false;
            }

            switchService = container.TryResolve<ILevelSetSwitchService>();

            if (switchService == null)
            {
                problem = $"{nameof(ILevelSetSwitchService)} is not bound in the scene container";
                return false;
            }

            return true;
        }
    }
}
#endif
