#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Editor.LevelSets
{
    internal static class PrefabComponentCatalog
    {
        public static List<Component> Scan(Type componentType)
        {
            var result = new List<Component>();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (root == null)
                    continue;

                var component = root.GetComponent(componentType);

                if (component != null)
                    result.Add(component);
            }

            result.Sort(CompareByPath);
            return result;
        }

        public static Component Resolve(Object[] candidates, Type componentType)
        {
            if (candidates == null)
                return null;

            foreach (var candidate in candidates)
            {
                var root = candidate as GameObject;

                if (root == null || !AssetDatabase.Contains(root))
                    continue;

                var component = root.GetComponent(componentType);

                if (component != null)
                    return component;
            }

            return null;
        }

        public static string MenuPathOf(Component component)
        {
            var assetPath = AssetDatabase.GetAssetPath(component);
            var folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(assetPath));

            return string.IsNullOrEmpty(folder)
                ? component.name
                : $"{folder}/{component.name}";
        }

        private static int CompareByPath(Component left, Component right)
        {
            return string.Compare(MenuPathOf(left), MenuPathOf(right), StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
