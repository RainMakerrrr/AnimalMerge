#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Facades;
using Framework.Code;
using UnityEngine;

namespace Code.Editor.BattleSceneBuilder
{
    internal static class AllyPrefabCatalog
    {
        public static IReadOnlyDictionary<AnimalType, AnimalFacade> Load()
        {
            var prefabsByType = new Dictionary<AnimalType, AnimalFacade>();

            foreach (var prefab in Resources.LoadAll<AnimalFacade>(AssetPath.Animals))
            {
                if (prefab == null || prefabsByType.ContainsKey(prefab.Type))
                    continue;

                prefabsByType.Add(prefab.Type, prefab);
            }

            return prefabsByType;
        }
    }
}
#endif
