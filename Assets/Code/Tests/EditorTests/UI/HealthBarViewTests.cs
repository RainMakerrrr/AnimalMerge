using System.Linq;
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
        private const float DefaultWidthPerGridCell = 100f;

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

        private static HealthBarFixture CreateHealthBarView(float maxHealth = 100f, UnitSize? unitSize = null)
        {
            var animalGo = new GameObject("Animal");
            var movement = animalGo.AddComponent<AnimalMovement>();
            if (unitSize.HasValue)
                SetPrivateField(movement, "_unitSize", unitSize.Value);

            var healthBarRoot = new GameObject("HealthBar");
            healthBarRoot.transform.SetParent(animalGo.transform);

            var canvasGo = new GameObject("HealthBarCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(healthBarRoot.transform);

            var barGo = new GameObject("Bar", typeof(RectTransform));
            barGo.transform.SetParent(canvasGo.transform);
            var bar = (RectTransform)barGo.transform;

            var templateGo = new GameObject("SegmentTemplate", typeof(RectTransform));
            templateGo.transform.SetParent(bar);
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(templateGo.transform);
            var templateFill = fillGo.AddComponent<Image>();
            templateFill.type = Image.Type.Filled;
            var template = templateGo.AddComponent<HealthBarSegment>();
            SetPrivateField(template, "_fill", templateFill);

            var segments = canvasGo.AddComponent<SegmentedHealthBar>();
            SetPrivateField(segments, "_container", bar);
            SetPrivateField(segments, "_segmentTemplate", template);

            var layout = canvasGo.AddComponent<HorizontalHealthBarLayout>();
            SetPrivateField(layout, "_bar", bar);

            var health = HealthTestHelper.CreateAnimalHealth(maxHealth);

            var view = canvasGo.AddComponent<HealthBarView>();
            SetPrivateField(view, "_health", health);
            SetPrivateField(view, "_layout", (HealthBarLayout)layout);
            SetPrivateField(view, "_segments", segments);

            InvokeLifecycle(view, "Awake");
            InvokeLifecycle(view, "OnEnable");

            return new HealthBarFixture(animalGo, view, health, bar);
        }

        private static void InvokeLifecycle(HealthBarView view, string methodName)
        {
            typeof(HealthBarView)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                !.Invoke(view, null);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                !.SetValue(target, value);
        }

        [Test]
        public void HealthChanged_UpdatesFillAmount_ToCurrentOverMax()
        {
            var fixture = CreateHealthBarView(maxHealth: 100f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 40f);

            fixture.Health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            fixture.DisplayedFill().Should().BeApproximately(0.6f, 0.001f);

            fixture.Destroy();
            Object.DestroyImmediate(attacker.gameObject);
        }

        [Test]
        public void HealthChanged_MaxIsZero_DoesNotChangeFillAmount()
        {
            var fixture = CreateHealthBarView(maxHealth: 100f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 50f);
            fixture.Health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            fixture.Health.SetMaxHealth(0f);

            fixture.DisplayedFill().Should().BeApproximately(0.5f, 0.001f);

            fixture.Destroy();
            Object.DestroyImmediate(attacker.gameObject);
        }

        [Test]
        public void Died_DisablesHealthBarGameObject()
        {
            var fixture = CreateHealthBarView(maxHealth: 30f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 30f);

            fixture.Health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            fixture.View.gameObject.activeSelf.Should().BeFalse("Died event should hide the bar");

            fixture.Destroy();
            Object.DestroyImmediate(attacker.gameObject);
        }

        [Test]
        public void OnDisable_Unsubscribes_FillAmountDoesNotUpdate()
        {
            var fixture = CreateHealthBarView(maxHealth: 100f);
            fixture.DisplayedFill().Should().BeApproximately(1f, 0.001f, "initial state");
            var attacker = AttackTestHelper.CreateMockAttack(damage: 50f);

            fixture.View.gameObject.SetActive(false);
            InvokeLifecycle(fixture.View, "OnDisable");
            fixture.Health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            fixture.DisplayedFill().Should().BeApproximately(1f, 0.001f, "unsubscribed: no update expected");

            fixture.Destroy();
            Object.DestroyImmediate(attacker.gameObject);
        }

        [Test]
        public void Awake_LargeUnit_SizesDeltaXByUnitWidth()
        {
            var fixture = CreateHealthBarView(unitSize: UnitSize.Large);

            fixture.Bar.sizeDelta.x.Should().BeApproximately(DefaultWidthPerGridCell * UnitSize.Large.Width, 0.01f);

            fixture.Destroy();
        }

        private sealed class HealthBarFixture
        {
            private readonly GameObject _animal;

            public HealthBarFixture(GameObject animal, HealthBarView view, AnimalHealth health, RectTransform bar)
            {
                _animal = animal;
                View = view;
                Health = health;
                Bar = bar;
            }

            public HealthBarView View { get; }
            public AnimalHealth Health { get; }
            public RectTransform Bar { get; }

            public float DisplayedFill()
            {
                var segments = Bar.GetComponentsInChildren<HealthBarSegment>(true)
                    .Where(segment => segment.gameObject.activeSelf)
                    .ToArray();

                segments.Should().NotBeEmpty("the bar should be built from the segment template");
                return segments.Sum(segment => segment.Fill.fillAmount) / segments.Length;
            }

            public void Destroy()
            {
                Object.DestroyImmediate(_animal);
                Object.DestroyImmediate(Health.gameObject);
            }
        }
    }
}
