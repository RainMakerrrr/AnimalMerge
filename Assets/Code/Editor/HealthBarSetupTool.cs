#if UNITY_EDITOR
using System.Collections.Generic;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Animals.UI;
using Code.Editor.AnimalPrefabBuilder;
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    public static class HealthBarSetupTool
    {
        private const string AllyHealthBarPrefabPath = "Assets/Prefabs/AllyHealthBar.prefab";
        private const string EnemyHealthBarPrefabPath = "Assets/Prefabs/EnemyHealthBar.prefab";
        private const string AnimalsFolder = "Assets/Resources/Prefabs/Animals";
        private const string EnemiesFolder = "Assets/Resources/Prefabs/Enemies";
        private const string HealthBarChildName = "HealthBar";
        private const string LegacyHealthBarChildName = "HealthBarRoot";

        private static readonly Dictionary<string, float> EnemyHeightOffsets = new Dictionary<string, float>
        {
            { "EnemyChicken_Temp",  1.0f },
            { "T-Rex",              2.8f },
            { "Velociraptor",       2.0f },
            { "Pteranodon",         1.3f },
        };

        private const float DefaultEnemyHeightOffset = 1.5f;

        [MenuItem("Tools/Animals/Setup HealthBar Prefabs")]
        public static void SetupHealthBars()
        {
            var allyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AllyHealthBarPrefabPath);
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyHealthBarPrefabPath);

            if (allyPrefab == null || enemyPrefab == null)
            {
                Debug.LogError(
                    $"[HealthBarSetupTool] Missing health bar prefab: {AllyHealthBarPrefabPath} or {EnemyHealthBarPrefabPath}");
                return;
            }

            var count = 0;
            count += ApplyToFolder(AnimalsFolder, allyPrefab, ResolveAllyPlacement);
            count += ApplyToFolder(EnemiesFolder, enemyPrefab, ResolveEnemyPlacement);

            AssetDatabase.SaveAssets();
            Debug.Log($"[HealthBarSetupTool] Done. Applied health bar prefabs to {count} prefabs.");
        }

        private static int ApplyToFolder(
            string folder,
            GameObject healthBarPrefab,
            System.Func<GameObject, BarPlacement> resolvePlacement)
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (ApplyHealthBarToPrefab(path, healthBarPrefab, resolvePlacement))
                    count++;
            }

            return count;
        }

        private static BarPlacement ResolveAllyPlacement(GameObject prefabRoot)
        {
            if (!ModelBoundsCalculator.TryCalculateRootLocalBounds(prefabRoot, out var bounds))
            {
                Debug.LogWarning($"[HealthBarSetupTool] No mesh bounds on '{prefabRoot.name}', lateral offset left at 0");
                return new BarPlacement(AnimalPrefabConstants.AllyHealthBarBaseOffset, 0f);
            }

            var movement = prefabRoot.GetComponent<AnimalMovement>();
            var footprint = movement != null ? movement.UnitSize : UnitSize.Small;
            var geometry = DerivedGeometry.Calculate(bounds, footprint, prefabRoot.transform.localScale);

            return new BarPlacement(AnimalPrefabConstants.AllyHealthBarBaseOffset, geometry.HealthBarLateralOffset);
        }

        private static BarPlacement ResolveEnemyPlacement(GameObject prefabRoot) =>
            new BarPlacement(
                EnemyHeightOffsets.TryGetValue(prefabRoot.name, out var height) ? height : DefaultEnemyHeightOffset,
                0f);

        private static bool ApplyHealthBarToPrefab(
            string path,
            GameObject healthBarPrefab,
            System.Func<GameObject, BarPlacement> resolvePlacement)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveExistingHealthBar(root.transform, HealthBarChildName);
                RemoveExistingHealthBar(root.transform, LegacyHealthBarChildName);

                var placement = resolvePlacement(root);

                var barInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, root.transform);
                barInstance.name = HealthBarChildName;

                WireHealth(barInstance, root);
                SetPlacement(barInstance, placement);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HealthBarSetupTool] Failed to process {path}: {e.Message}");
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveExistingHealthBar(Transform root, string childName)
        {
            var existing = root.Find(childName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }

        private static void WireHealth(GameObject barInstance, GameObject prefabRoot)
        {
            var health = prefabRoot.GetComponentInChildren<AnimalHealth>();
            var view = barInstance.GetComponentInChildren<HealthBarView>();
            if (health == null || view == null)
            {
                Debug.LogWarning($"[HealthBarSetupTool] Could not wire health on '{prefabRoot.name}': health={health != null}, view={view != null}");
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("_health").objectReferenceValue = health;
            so.ApplyModifiedProperties();
        }

        private static void SetPlacement(GameObject barInstance, BarPlacement placement)
        {
            var rotator = barInstance.GetComponent<BillboardRotator>();
            if (rotator == null) return;

            var so = new SerializedObject(rotator);
            so.FindProperty("_heightOffset").floatValue = placement.HeightOffset;
            so.FindProperty("_lateralOffset").floatValue = placement.LateralOffset;
            so.ApplyModifiedProperties();
        }

        private readonly struct BarPlacement
        {
            public BarPlacement(float heightOffset, float lateralOffset)
            {
                HeightOffset = heightOffset;
                LateralOffset = lateralOffset;
            }

            public float HeightOffset { get; }
            public float LateralOffset { get; }
        }
    }
}
#endif
