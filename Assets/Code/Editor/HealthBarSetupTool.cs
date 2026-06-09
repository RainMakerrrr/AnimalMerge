#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Code.Animals.Health;
using Code.Animals.UI;
using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    public static class HealthBarSetupTool
    {
        private const string HealthBarPrefabPath = "Assets/Prefabs/HealthBar.prefab";
        private const string AnimalsFolder = "Assets/Resources/Prefabs/Animals";
        private const string EnemiesFolder = "Assets/Resources/Prefabs/Enemies";
        private const string CheetahPrefabPath = "Assets/Resources/Prefabs/Animals/Cheetah.prefab";

        private static readonly Dictionary<string, float> HeightOffsets = new Dictionary<string, float>
        {
            { "Cheetah",            1.7f },
            { "Chicken",            1.0f },
            { "Hedgehog",           1.0f },
            { "Fox",                1.3f },
            { "Deer",               1.5f },
            { "Elephant",           2.5f },
            { "EnemyCheetah_Temp",  1.7f },
            { "EnemyChicken_Temp",  1.0f },
            { "EnemyElephant_Temp", 2.5f },
            { "T-Rex",              2.8f },
            { "Velociraptor",       2.5f },
        };

        private const float DefaultHeightOffset = 1.5f;

        [MenuItem("Tools/Animals/Setup HealthBar Prefabs")]
        public static void SetupHealthBars()
        {
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath) != null;
            if (prefabExists)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "HealthBar.prefab already exists",
                    "Re-running will overwrite HealthBar.prefab and re-wire all animal/enemy prefabs. Continue?",
                    "Continue", "Cancel");
                if (!proceed) return;
            }

            var healthBarPrefab = ExtractHealthBarPrefab();
            if (healthBarPrefab == null)
            {
                Debug.LogError("[HealthBarSetupTool] Failed to create HealthBar.prefab — aborting.");
                return;
            }

            int count = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { AnimalsFolder, EnemiesFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (ApplyHealthBarToPrefab(path, healthBarPrefab))
                    count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[HealthBarSetupTool] Done. Applied HealthBar prefab to {count} prefabs.");
        }

        private static GameObject ExtractHealthBarPrefab()
        {
            GameObject cheetahRoot = PrefabUtility.LoadPrefabContents(CheetahPrefabPath);
            try
            {
                Transform healthBarTransform = cheetahRoot.transform.Find("HealthBarRoot");
                if (healthBarTransform == null)
                {
                    Debug.LogError("[HealthBarSetupTool] HealthBarRoot not found in Cheetah.prefab");
                    return null;
                }

                // On rerun: HealthBarRoot is already a nested prefab instance — avoid saving an
                // instance back over its own source asset (would produce circular / stripped data).
                if (PrefabUtility.GetPrefabAssetType(healthBarTransform.gameObject) != PrefabAssetType.NotAPrefab)
                {
                    var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
                    if (existing != null) return existing;
                    // Prefab asset missing despite instance existing — fall through to re-extract.
                }

                // First run: inline HealthBarRoot — extract it as a standalone prefab.
                // Clear external _health ref so the standalone prefab has null as default.
                var view = healthBarTransform.GetComponentInChildren<HealthBarView>();
                if (view != null)
                {
                    var so = new SerializedObject(view);
                    so.FindProperty("_health").objectReferenceValue = null;
                    so.ApplyModifiedProperties();
                }

                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");

                PrefabUtility.SaveAsPrefabAsset(healthBarTransform.gameObject, HealthBarPrefabPath);
                AssetDatabase.ImportAsset(HealthBarPrefabPath);

                return AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            }
            finally
            {
                // Discard — Cheetah is updated in the main loop like all other prefabs
                PrefabUtility.UnloadPrefabContents(cheetahRoot);
            }
        }

        private static bool ApplyHealthBarToPrefab(string path, GameObject healthBarPrefab)
        {
            string prefabName = Path.GetFileNameWithoutExtension(path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform existing = root.transform.Find("HealthBarRoot");
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                var barInstance = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab, root.transform);

                WireHealth(barInstance, root);
                SetHeightOffset(barInstance, prefabName);

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

        private static void SetHeightOffset(GameObject barInstance, string prefabName)
        {
            var rotator = barInstance.GetComponent<BillboardRotator>();
            if (rotator == null) return;

            float offset = HeightOffsets.TryGetValue(prefabName, out float h) ? h : DefaultHeightOffset;
            var so = new SerializedObject(rotator);
            so.FindProperty("_heightOffset").floatValue = offset;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
