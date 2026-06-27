using System.Collections;
using System.Reflection;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.Animals.UI;
using FluentAssertions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Code.Tests.PlayModeTests.UI
{
    public class DamagePopupControllerIntegrationTests
    {
        private GameObject _canvasGo;
        private GameObject _cameraGo;

        [SetUp]
        public void SetUp()
        {
            // Screen Space Overlay canvas — required for DamagePopupController.Start()
            _canvasGo = new GameObject("OverlayCanvas");
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Camera tagged MainCamera — required for Camera.main
            _cameraGo = new GameObject("MainCamera");
            _cameraGo.tag = "MainCamera";
            _cameraGo.AddComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvasGo);
            Object.DestroyImmediate(_cameraGo);

            foreach (var popup in Object.FindObjectsOfType<DamagePopupView>())
                Object.DestroyImmediate(popup.gameObject);
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        private static AnimalHealth CreateAnimalHealth(float maxHealth = 100f)
        {
            var go = new GameObject("TestAnimalHealth");
            var health = go.AddComponent<AnimalHealth>();

            var animatorGO = new GameObject("MockAnimator");
            animatorGO.transform.SetParent(go.transform);
            var animator = animatorGO.AddComponent<AnimalAnimator>();

            var collider = go.AddComponent<BoxCollider>();
            health.Construct(new Collider[] { collider }, new AbilityManager());

            typeof(AnimalHealth)
                .GetField("_max", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(health, maxHealth);
            typeof(AnimalHealth)
                .GetField("_animator", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(health, animator);

            health.SetMaxHealth(maxHealth);
            return health;
        }

        private DamagePopupView CreatePopupTemplate()
        {
            var templateGo = new GameObject("PopupTemplate");
            templateGo.AddComponent<RectTransform>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(templateGo.transform);
            textGo.AddComponent<RectTransform>();
            textGo.AddComponent<TextMeshProUGUI>();

            var view = templateGo.AddComponent<DamagePopupView>();

            var textComp = textGo.GetComponent<TextMeshProUGUI>();
            typeof(DamagePopupView)
                .GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(view, textComp);

            templateGo.SetActive(false);
            return view;
        }

        private (DamagePopupController controller, AnimalHealth health) CreateControllerWithHealth()
        {
            var template = CreatePopupTemplate();

            var animalGo = new GameObject("Animal");
            animalGo.transform.position = new Vector3(0f, 0f, 5f);

            var health = CreateAnimalHealth(maxHealth: 100f);

            var controller = animalGo.AddComponent<DamagePopupController>();
            SetField(controller, "_health", health);
            SetField(controller, "_popupPrefab", template);

            // Re-trigger OnEnable so it subscribes with the now-wired _health
            // (AddComponent runs OnEnable before we set _health, so subscription never happened)
            controller.gameObject.SetActive(false);
            controller.gameObject.SetActive(true);

            return (controller, health);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(target, value);
        }

        private static void FireTakenDamage(AnimalHealth health, float damage)
        {
            var field = typeof(AnimalHealth)
                .GetField("TakenDamage", BindingFlags.NonPublic | BindingFlags.Instance);
            (field?.GetValue(health) as System.Action<float>)?.Invoke(damage);
        }

        private static void FireDamageBlocked(AnimalHealth health)
        {
            var field = typeof(AnimalHealth)
                .GetField("DamageBlocked", BindingFlags.NonPublic | BindingFlags.Instance);
            (field?.GetValue(health) as System.Action)?.Invoke();
        }

        private static string GetPopupText(DamagePopupView popup)
        {
            var textField = typeof(DamagePopupView)
                .GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance);
            return (textField?.GetValue(popup) as TextMeshProUGUI)?.text;
        }

        // ─── Tests ─────────────────────────────────────────────────────────────

        /// <summary>IT-DPC-001: TakenDamage event spawns a popup with the formatted damage text</summary>
        [UnityTest]
        public IEnumerator TakenDamage_SpawnsPopupWithDamageText()
        {
            // Arrange
            var (controller, health) = CreateControllerWithHealth();
            yield return null; // let Start() run — finds canvas and camera

            // Act
            FireTakenDamage(health, 25f);
            yield return null;

            // Assert
            var popup = Object.FindObjectOfType<DamagePopupView>();
            popup.Should().NotBeNull("TakenDamage should spawn a popup");
            GetPopupText(popup).Should().Be("-25");

            // Cleanup
            Object.DestroyImmediate(controller.gameObject);
            Object.DestroyImmediate(health.gameObject);
        }

        /// <summary>IT-DPC-002: DamageBlocked event spawns a popup with "Miss!" text</summary>
        [UnityTest]
        public IEnumerator DamageBlocked_SpawnsPopupWithMissText()
        {
            // Arrange
            var (controller, health) = CreateControllerWithHealth();
            yield return null; // let Start() run

            // Act
            FireDamageBlocked(health);
            yield return null;

            // Assert
            var popup = Object.FindObjectOfType<DamagePopupView>();
            popup.Should().NotBeNull("DamageBlocked should spawn a Miss! popup");
            GetPopupText(popup).Should().Be("Miss!");

            // Cleanup
            Object.DestroyImmediate(controller.gameObject);
            Object.DestroyImmediate(health.gameObject);
        }
    }
}
