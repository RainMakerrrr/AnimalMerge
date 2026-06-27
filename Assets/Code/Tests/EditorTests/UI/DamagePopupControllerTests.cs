using System.Reflection;
using Code.Animals.Health;
using Code.Animals.UI;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.UI
{
    [TestFixture]
    public class DamagePopupControllerTests
    {
        [SetUp]
        public void SetUp()
        {
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                try
                {
                    if (obj == null) continue;
                    if (obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad") continue;
                    Object.DestroyImmediate(obj);
                }
                catch { }
            }
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        /// Creates a DamagePopupController subscribed to the given health.
        /// Start() is never called in EditorTests, so _canvas/_camera stay null by default.
        private DamagePopupController CreateController(AnimalHealth health)
        {
            var go = new GameObject("Animal");
            var controller = go.AddComponent<DamagePopupController>();

            SetPrivateField(controller, "_health", health);

            // Re-trigger OnEnable to subscribe with the now-wired health
            controller.gameObject.SetActive(false);
            controller.gameObject.SetActive(true);

            return controller;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
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

        // ─── Tests ─────────────────────────────────────────────────────────────

        /// <summary>UT-DPC-001: No canvas (Start never ran) → SpawnPopup guard returns early → no popup instantiated</summary>
        [Test]
        public void TakenDamage_CanvasNull_DoesNotSpawnPopup()
        {
            // Arrange — Start() not called, so _canvas = null
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);
            var controller = CreateController(health);

            // Act
            FireTakenDamage(health, 25f);

            // Assert
            Object.FindObjectsOfType<DamagePopupView>().Should().BeEmpty(
                "SpawnPopup guards against null _canvas");

            // Cleanup
            Object.DestroyImmediate(controller.gameObject);
            Object.DestroyImmediate(health.gameObject);
        }

        /// <summary>UT-DPC-002: Camera set but canvas still null → guard returns early → no popup</summary>
        [Test]
        public void TakenDamage_CameraSetCanvasNull_DoesNotSpawnPopup()
        {
            // Arrange — set _camera manually, leave _canvas null
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);
            var controller = CreateController(health);

            var cameraGo = new GameObject("Camera");
            var camera = cameraGo.AddComponent<Camera>();
            SetPrivateField(controller, "_camera", camera);

            // Act
            FireTakenDamage(health, 25f);

            // Assert
            Object.FindObjectsOfType<DamagePopupView>().Should().BeEmpty(
                "SpawnPopup guards against null _canvas even when camera is set");

            // Cleanup
            Object.DestroyImmediate(controller.gameObject);
            Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(cameraGo);
        }

        /// <summary>UT-DPC-003: OnDisable unsubscribes — events fired after disable produce no popup</summary>
        [Test]
        public void OnDisable_Unsubscribes_EventsNoLongerTriggerSpawn()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);
            var controller = CreateController(health);

            // Act — disable then fire both event types
            controller.gameObject.SetActive(false);
            FireTakenDamage(health, 25f);
            FireDamageBlocked(health);

            // Assert
            Object.FindObjectsOfType<DamagePopupView>().Should().BeEmpty(
                "after OnDisable, controller should not react to health events");

            // Cleanup
            Object.DestroyImmediate(controller.gameObject);
            Object.DestroyImmediate(health.gameObject);
        }
    }
}
