#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Animals;
using Code.Animals.Facades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Editor.AnimalPortraits
{
    public static class AnimalPortraitBaker
    {
        private const string PortraitFolder = "Assets/UI_Sprites/AnimalPortraits";
        private const string PortraitPrefix = "portrait_";
        private const int PortraitSize = 256;
        private const float FieldOfView = 32f;
        private const float MaxHeadRadiusFactor = 0.24f;
        private const float MinHeadRadiusFactor = 0.15f;
        private const float HeadHeightRadiusFactor = 0.45f;
        private const float BodyRadiusFactor = 0.62f;
        private const float YawDegrees = 20f;
        private const float PitchDegrees = 8f;

        private static readonly string[] PrefabFolders =
        {
            "Assets/Resources/Prefabs/Animals",
            "Assets/Resources/Prefabs/Enemies"
        };

        private static readonly Color BackgroundColor = new Color(0.176f, 0.192f, 0.243f, 1f);

        [MenuItem("Tools/Animals/Bake Turn Order Portraits")]
        public static void BakeAll()
        {
            EnsurePortraitFolder();

            var prefabsByType = ResolvePrefabs();
            var previewScene = EditorSceneManager.NewPreviewScene();
            var bakedPaths = new List<string>();

            try
            {
                var rig = CreateRig(previewScene);

                foreach (AnimalType type in System.Enum.GetValues(typeof(AnimalType)))
                {
                    if (prefabsByType.TryGetValue(type, out var prefab) == false)
                    {
                        Debug.LogWarning($"[AnimalPortraitBaker] No prefab found for {type}");
                        continue;
                    }

                    bakedPaths.Add(BakeSingle(type, prefab, previewScene, rig));
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            AssetDatabase.Refresh();

            foreach (var path in bakedPaths.Where(p => string.IsNullOrEmpty(p) == false))
                ApplyImportSettings(path);

            AssetDatabase.Refresh();

            Debug.Log($"[AnimalPortraitBaker] Baked {bakedPaths.Count(p => string.IsNullOrEmpty(p) == false)} portraits into {PortraitFolder}");
        }

        private static string BakeSingle(AnimalType type, GameObject prefab, Scene previewScene, Camera rig)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, previewScene);

            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                foreach (var skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    skinned.updateWhenOffscreen = true;

                foreach (var canvas in instance.GetComponentsInChildren<Canvas>(true))
                    canvas.gameObject.SetActive(false);

                if (TryResolveFraming(instance, out var center, out var radius) == false)
                {
                    Debug.LogWarning($"[AnimalPortraitBaker] {type} has no renderers to frame");
                    return null;
                }

                PlaceCamera(rig, instance.transform, center, radius);

                var texture = Capture(rig);
                var assetPath = $"{PortraitFolder}/{PortraitPrefix}{type.ToString().ToLowerInvariant()}.png";

                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                return assetPath;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Camera CreateRig(Scene previewScene)
        {
            var cameraObject = new GameObject("PortraitCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.fieldOfView = FieldOfView;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 500f;
            camera.cullingMask = ~0;
            camera.scene = previewScene;

            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);

            CreateLight(previewScene, new Vector3(30f, -140f, 0f), 1.35f, new Color(1f, 0.97f, 0.9f));
            CreateLight(previewScene, new Vector3(15f, 40f, 0f), 0.6f, new Color(0.75f, 0.82f, 1f));

            return camera;
        }

        private static void CreateLight(Scene previewScene, Vector3 euler, float intensity, Color color)
        {
            var lightObject = new GameObject("PortraitLight");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(euler);

            SceneManager.MoveGameObjectToScene(lightObject, previewScene);
        }

        private static bool TryResolveFraming(GameObject instance, out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = 0f;

            var bounds = new Bounds();
            var hasBounds = false;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                if (hasBounds == false)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (hasBounds == false)
                return false;

            var longestSide = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var head = FindHeadBone(instance.transform);

            if (head == null)
            {
                center = bounds.center;
                radius = longestSide * BodyRadiusFactor;
                return true;
            }

            center = head.position;
            radius = Mathf.Clamp(
                bounds.size.y * HeadHeightRadiusFactor,
                longestSide * MinHeadRadiusFactor,
                longestSide * MaxHeadRadiusFactor);
            return true;
        }

        private static Transform FindHeadBone(Transform root)
        {
            var bones = root.GetComponentsInChildren<Transform>(true);
            var head = bones.FirstOrDefault(bone => IsBoneNamed(bone, "head"));

            return head != null ? head : bones.LastOrDefault(bone => IsBoneNamed(bone, "neck"));
        }

        private static bool IsBoneNamed(Transform bone, string keyword)
        {
            var name = bone.name.ToLowerInvariant();

            if (name.Contains(keyword) == false)
                return false;

            return name.Contains("end") == false && name.Contains("top") == false && name.Contains("nub") == false;
        }

        private static void PlaceCamera(Camera camera, Transform subject, Vector3 center, float radius)
        {
            var distance = radius / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad);
            var facing = Quaternion.AngleAxis(YawDegrees, Vector3.up) * subject.forward;
            var direction = Quaternion.AngleAxis(-PitchDegrees, Vector3.Cross(Vector3.up, facing)) * facing;

            camera.transform.position = center + direction.normalized * distance;
            camera.transform.LookAt(center, Vector3.up);
        }

        private static Texture2D Capture(Camera camera)
        {
            var renderTexture = new RenderTexture(PortraitSize, PortraitSize, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };

            var previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;

            try
            {
                camera.Render();

                RenderTexture.active = renderTexture;

                var texture = new Texture2D(PortraitSize, PortraitSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, PortraitSize, PortraitSize), 0, 0);

                var pixels = texture.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                    pixels[i].a = 255;

                texture.SetPixels32(pixels);
                texture.Apply();

                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }

        private static Dictionary<AnimalType, GameObject> ResolvePrefabs()
        {
            var result = new Dictionary<AnimalType, GameObject>();

            foreach (var folder in PrefabFolders)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab == null)
                        continue;

                    var facade = prefab.GetComponent<AnimalFacade>();

                    if (facade == null || result.ContainsKey(facade.Type))
                        continue;

                    result.Add(facade.Type, prefab);
                }
            }

            return result;
        }

        private static void EnsurePortraitFolder()
        {
            if (AssetDatabase.IsValidFolder(PortraitFolder))
                return;

            AssetDatabase.CreateFolder("Assets/UI_Sprites", "AnimalPortraits");
        }

        private static void ApplyImportSettings(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = PortraitSize;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
#endif
