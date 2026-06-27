using System.Reflection;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Tests.EditorTests.UI
{
    [TestFixture]
    public class HealthBarViewTests
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

        private (HealthBarView view, AnimalHealth health, Image fillImage) CreateHealthBarView(
            float maxHealth = 100f)
        {
            // Animal root with AnimalMovement (UnitSize.Small by default)
            var animalGo = new GameObject("Animal");
            animalGo.AddComponent<AnimalMovement>();

            // HealthBar root (BillboardRotator's parent — not touched)
            var healthBarRoot = new GameObject("HealthBar");
            healthBarRoot.transform.SetParent(animalGo.transform);

            // HealthBarCanvas — HealthBarView.Awake fires when component is added
            var canvasGo = new GameObject("HealthBarCanvas");
            canvasGo.transform.SetParent(healthBarRoot.transform);
            canvasGo.AddComponent<RectTransform>();

            // Fill image child
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(canvasGo.transform);
            var fillImage = fillGo.AddComponent<Image>();

            // AnimalHealth (separate GO — as in production)
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth);

            // Add HealthBarView — Awake fires immediately, _health is still null so OnEnable returns early
            var view = canvasGo.AddComponent<HealthBarView>();

            // Wire serialized fields
            SetPrivateField(view, "_health", health);
            SetPrivateField(view, "_fillImage", fillImage);

            // Re-trigger OnEnable so it subscribes with the now-set _health
            view.gameObject.SetActive(false);
            view.gameObject.SetActive(true);

            return (view, health, fillImage);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            typeof(HealthBarView)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(target, value);
        }

        // ─── Tests ─────────────────────────────────────────────────────────────

        /// <summary>UT-HB-001: fillAmount is updated to current/max on HealthChanged event</summary>
        [Test]
        public void HealthChanged_UpdatesFillAmount_ToCurrentOverMax()
        {
            // Arrange
            var (view, health, fillImage) = CreateHealthBarView(maxHealth: 100f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 40f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert — 60/100 = 0.6
            fillImage.fillAmount.Should().BeApproximately(0.6f, 0.001f);

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
            Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>UT-HB-002: When Max drops to 0, Refresh guard prevents divide-by-zero and leaves fillAmount unchanged</summary>
        [Test]
        public void HealthChanged_MaxIsZero_DoesNotChangeFillAmount()
        {
            // Arrange — start at 50 HP then reduce to half
            var (view, health, fillImage) = CreateHealthBarView(maxHealth: 100f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 50f);
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult(); // fillAmount = 0.5f

            // Act — SetMaxHealth(0) fires HealthChanged; Refresh guard (Max <= 0) should return early
            health.SetMaxHealth(0f);

            // Assert — fillAmount unchanged at 0.5f, no NaN or exception
            fillImage.fillAmount.Should().BeApproximately(0.5f, 0.001f);

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
            Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>UT-HB-003: Died event disables the health bar GameObject</summary>
        [Test]
        public void Died_DisablesHealthBarGameObject()
        {
            // Arrange
            var (view, health, _) = CreateHealthBarView(maxHealth: 30f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 30f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            view.gameObject.activeSelf.Should().BeFalse("Died event should call SetActive(false)");

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
            Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>UT-HB-004: OnDisable unsubscribes from events — damage taken after disable does not update fillAmount</summary>
        [Test]
        public void OnDisable_Unsubscribes_FillAmountDoesNotUpdate()
        {
            // Arrange
            var (view, health, fillImage) = CreateHealthBarView(maxHealth: 100f);
            fillImage.fillAmount.Should().BeApproximately(1f, 0.001f, "initial state");
            var attacker = AttackTestHelper.CreateMockAttack(damage: 50f);

            // Act — disable first, then apply damage
            view.gameObject.SetActive(false);
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert — fillAmount unchanged (unsubscribed)
            fillImage.fillAmount.Should().BeApproximately(1f, 0.001f, "unsubscribed: no update expected");

            // Cleanup
            Object.DestroyImmediate(view.transform.root.gameObject);
            Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>UT-HB-005: Awake scales sizeDelta.x by UnitSize.Width — Large (2×2) gets twice the base width</summary>
        [Test]
        public void Awake_LargeUnit_SizesDeltaXByUnitWidth()
        {
            // Arrange — animal with Large UnitSize (width = 2)
            var animalGo = new GameObject("Animal");
            var movement = animalGo.AddComponent<AnimalMovement>();
            typeof(AnimalMovement)
                .GetField("_unitSize", BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(movement, UnitSize.Large);

            var healthBarRoot = new GameObject("HealthBar");
            healthBarRoot.transform.SetParent(animalGo.transform);

            var canvasGo = new GameObject("HealthBarCanvas");
            canvasGo.transform.SetParent(healthBarRoot.transform);
            canvasGo.AddComponent<RectTransform>();

            const float defaultWidthPerCell = 100f; // matches HealthBarView serialized default

            // Act — Awake fires on AddComponent, reads UnitSize.Width from parent
            var view = canvasGo.AddComponent<HealthBarView>();
            var rt = view.GetComponent<RectTransform>();

            // Assert — Large.Width = 2, so sizeDelta.x = 100 × 2 = 200
            rt.sizeDelta.x.Should().BeApproximately(defaultWidthPerCell * UnitSize.Large.Width, 0.01f);

            // Cleanup
            Object.DestroyImmediate(animalGo);
        }
    }
}
