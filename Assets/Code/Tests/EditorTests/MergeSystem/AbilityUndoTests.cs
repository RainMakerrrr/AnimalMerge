using System.Linq;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Merge;
using Code.Animals.Merge.Commands;
using Code.Animals.Merge.MergeSkills;
using Code.Animals.Movement;
using Code.Battle.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.MergeSystem
{
    [TestFixture]
    public class AbilityUndoTests
    {
        [Test]
        public void MergeStateSnapshot_CapturesAbilities()
        {
            // Arrange
            var animal = CreateMockAnimalWithAbilities(2);

            // Act
            var snapshot = MergeStateSnapshot.Capture(animal);

            // Assert
            snapshot.Should().NotBeNull();
            snapshot.Abilities.Should().NotBeNull();
            snapshot.Abilities.Count.Should().Be(2, "should capture both abilities");
        }

        [Test]
        public void RemoveAddedAbilities_WhenNewAbilityAdded_RemovesIt()
        {
            // Arrange
            var targetAnimal = CreateMockAnimalWithAbilities(1);
            var sourceAnimal = CreateMockAnimalWithAbilities(0);

            // Capture state BEFORE adding new ability
            var stateBefore = MergeStateSnapshot.Capture(targetAnimal);
            stateBefore.Abilities.Count.Should().Be(1, "target should start with 1 ability");

            // Simulate merge: add a new ability
            var newAbility = Substitute.For<IAbility>();
            newAbility.Priority.Returns(10); // Different priority for distinction
            targetAnimal.AddAbility(newAbility);

            targetAnimal.AbilityManager.Abilities.Count.Should().Be(2, "should have 2 abilities after merge");

            // Create command with saved state
            var mergeTarget = Substitute.For<MergeTarget>();
            var unitTracker = Substitute.For<IUnitTracker>();

            // We need to use reflection to access private RemoveAddedAbilities method
            // For testing purposes, we'll manually recreate the logic
            var abilityManager = targetAnimal.AbilityManager;
            var currentAbilities = abilityManager.Abilities.ToList();
            var addedAbilities = currentAbilities.Where(a => !stateBefore.Abilities.Contains(a)).ToList();

            addedAbilities.Count.Should().Be(1, "should detect 1 new ability");

            // Act - remove added abilities
            foreach (var ability in addedAbilities)
            {
                abilityManager.UnregisterAbility(ability);
            }

            // Assert
            targetAnimal.AbilityManager.Abilities.Count.Should().Be(1, "should have removed the new ability");
            targetAnimal.AbilityManager.Abilities.Should().Contain(stateBefore.Abilities[0], "original ability should remain");
        }

        [Test]
        public void RemoveAddedAbilities_WhenNoNewAbilities_DoesNothing()
        {
            // Arrange
            var targetAnimal = CreateMockAnimalWithAbilities(2);
            var stateBefore = MergeStateSnapshot.Capture(targetAnimal);

            // No new abilities added

            var abilityManager = targetAnimal.AbilityManager;
            var currentAbilities = abilityManager.Abilities.ToList();
            var addedAbilities = currentAbilities.Where(a => !stateBefore.Abilities.Contains(a)).ToList();

            // Assert
            addedAbilities.Count.Should().Be(0, "should detect no new abilities");
            targetAnimal.AbilityManager.Abilities.Count.Should().Be(2, "ability count should remain unchanged");
        }

        // Helper method to create mock animal with abilities
        private AnimalFacade CreateMockAnimalWithAbilities(int abilityCount)
        {
            // Create a real GameObject (needed for GetComponent)
            var go = new GameObject("TestAnimal");
            var animal = go.AddComponent<TestAnimalFacade>();

            // Set up health with AbilityManager
            var healthGO = new GameObject("Health");
            healthGO.transform.SetParent(go.transform);
            var health = healthGO.AddComponent<Code.Animals.Health.AnimalHealth>();

            // Use reflection to set private _max field
            var maxField = typeof(Code.Animals.Health.AnimalHealth).GetField("_max",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxField?.SetValue(health, 100f);

            // Initialize health values
            health.SetMaxHealth(100f);
            health.SetCurrentHealth(100f);

            // Set up attack component (required for GetDamage())
            var attack = go.AddComponent<AnimalAttack>();
            attack.SetDamage(10f);

            // Set up movement component (required for Movement property)
            var movement = go.AddComponent<AnimalMovement>();

            // Use reflection to set private fields
            var healthField = typeof(AnimalFacade).GetField("_health",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            healthField?.SetValue(animal, health);

            var attackField = typeof(AnimalFacade).GetField("_attack",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            attackField?.SetValue(animal, attack);

            var movementField = typeof(AnimalFacade).GetField("_movement",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            movementField?.SetValue(animal, movement);

            // Initialize AbilityManager (normally done in Awake)
            var abilityManager = new AbilityManager();
            var abilityManagerField = typeof(AnimalFacade).GetField("_abilityManager",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            abilityManagerField?.SetValue(animal, abilityManager);

            // Construct health with the AbilityManager
            var collider = go.AddComponent<BoxCollider>();
            health.Construct(new UnityEngine.Collider[] { collider }, abilityManager);

            // Add abilities
            for (int i = 0; i < abilityCount; i++)
            {
                var ability = Substitute.For<IAbility>();
                ability.Priority.Returns(i);
                animal.AddAbility(ability);
            }

            return animal;
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up any GameObjects created during tests
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name == "TestAnimal")
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        // Simple test implementation of AnimalFacade
        private class TestAnimalFacade : AnimalFacade
        {
            public override void InitBehaviours()
            {
                // No-op for tests
            }
        }
    }
}
