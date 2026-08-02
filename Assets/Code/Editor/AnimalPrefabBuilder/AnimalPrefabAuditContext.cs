#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Merge.MergeAttributes;
using Code.Data.Animals;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public sealed class AnimalPrefabAuditContext
    {
        private readonly Dictionary<AnimalType, List<string>> _allyPathsByType =
            new Dictionary<AnimalType, List<string>>();

        private readonly HashSet<AnimalType> _slotMergeAllyTypes = new HashSet<AnimalType>();

        public AnimalDatabase Database { get; private set; }

        public IReadOnlyDictionary<AnimalType, List<string>> AllyPathsByType => _allyPathsByType;

        public IReadOnlyCollection<AnimalType> SlotMergeAllyTypes => _slotMergeAllyTypes;

        public static AnimalPrefabAuditContext Build(IEnumerable<string> prefabPaths)
        {
            var context = new AnimalPrefabAuditContext
            {
                Database = AssetDatabase.LoadAssetAtPath<AnimalDatabase>(AnimalPrefabPaths.AnimalDatabaseAsset)
            };

            foreach (var path in prefabPaths)
            {
                var side = AnimalSideProfile.FromAssetPath(path);
                if (side != AnimalSideProfile.Ally) continue;

                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) continue;

                var facade = root.GetComponent<AnimalFacade>();
                if (facade == null) continue;

                if (!context._allyPathsByType.TryGetValue(facade.Type, out var paths))
                {
                    paths = new List<string>();
                    context._allyPathsByType[facade.Type] = paths;
                }

                paths.Add(path);

                foreach (var attribute in root.GetComponentsInChildren<SlotMergeAttribute>(true))
                    context._slotMergeAllyTypes.Add(attribute.Type);
            }

            return context;
        }
    }
}
#endif
