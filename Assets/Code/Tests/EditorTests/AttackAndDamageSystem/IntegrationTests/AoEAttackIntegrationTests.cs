using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Services.Random;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.IntegrationTests
{
    [TestFixture]
    public class AoEAttackIntegrationTests
    {
        /// <summary>
        /// SetUp: Clean scene before each test to ensure test isolation
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Find and destroy all GameObjects in the scene
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                try
                {
                    // Unity overloads == operator, so check with == instead of != null
                    if (obj == null) continue;
                    if (obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad") continue;

                    UnityEngine.Object.DestroyImmediate(obj);
                }
                catch (System.Exception)
                {
                    // Object was already destroyed or is invalid, skip
                }
            }
        }

        /// <summary>
        /// INT-AOE-001: AoE_Attack_BypassesDodge_DamageApplied
        /// Verifies that AoE attacks bypass Dodge ability and damage is applied
        /// </summary>
        [Test]
        public void AoE_Attack_BypassesDodge_DamageApplied()
        {
            // Arrange - Fox with Dodge ability (first use = 100% normally)
            var dodge = AbilityTestMocks.CreateDodge(successChance: 50, initialCounter: 0);
            var foxHealth = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: dodge);

            var initialHealth = foxHealth.Current;

            // Arrange - AoE attacker
            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetDamage(25f);
            aoeAttack.SetIsAoE(true);

            // Act - Take damage from AoE attack
            foxHealth.TakeDamageAsync(aoeAttack).GetAwaiter().GetResult();

            // Assert
            foxHealth.Current.Should().Be(75f,
                "Fox should take damage because AoE bypasses Dodge");
            foxHealth.Current.Should().BeLessThan(initialHealth,
                "Health should decrease");

            // Cleanup
            Object.DestroyImmediate(foxHealth.gameObject);
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// INT-AOE-002: AoE_Attack_BypassesCounterAttack_NoCounter
        /// Verifies that AoE attacks bypass CounterAttack ability
        /// </summary>
        [Test]
        public void AoE_Attack_BypassesCounterAttack_NoCounter()
        {
            // Arrange - Hedgehog with CounterAttack
            var hedgehogGO = new GameObject("Hedgehog");
            var hedgehogHealth = hedgehogGO.AddComponent<AnimalHealth>();
            var hedgehogAnimator = hedgehogGO.AddComponent<AnimalAnimator>();
            var hedgehogAttack = hedgehogGO.AddComponent<AnimalAttack>();

            var abilityManager = new AbilityManager();
            hedgehogHealth.Construct(new Collider[] { hedgehogGO.AddComponent<BoxCollider>() }, abilityManager);
            hedgehogHealth.SetMaxHealth(50f);
            hedgehogAttack.SetDamage(15f);

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(hedgehogHealth, hedgehogAnimator);

            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var counterAttack = new CounterAttack(
                hedgehogHealth,
                hedgehogAnimator,
                hedgehogAttack,
                isOwner: true,
                randomProvider);

            hedgehogHealth.SetAbility(counterAttack);

            // Arrange - AoE attacker with health
            var attackerGO = new GameObject("AoEAttacker");
            var attackerHealth = AttackTestHelper.CreateMockDamageable(100f);
            attackerHealth.transform.SetParent(attackerGO.transform);
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetDamage(20f);
            aoeAttack.SetIsAoE(true);


            // Act
            hedgehogHealth.TakeDamageAsync(aoeAttack).GetAwaiter().GetResult();

            // Assert - Hedgehog takes damage
            hedgehogHealth.Current.Should().Be(30f,
                "Hedgehog should take damage from AoE attack");

            // Assert - Attacker does NOT take counter damage
            attackerHealth.TakeDamageCallCount.Should().Be(0,
                "CounterAttack should NOT trigger against AoE");
            attackerHealth.Current.Should().Be(100f,
                "Attacker health should remain at 100 (no counter-attack)");

            // Cleanup
            Object.DestroyImmediate(hedgehogGO);
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// INT-AOE-003: NonAoE_Attack_AllowsDodge_DamageBlocked
        /// Regression test: Verifies that non-AoE attacks still allow Dodge to work
        /// </summary>
        [Test]
        public void NonAoE_Attack_AllowsDodge_DamageBlocked()
        {
            // Arrange - Fox with Dodge (first use = 100%)
            var dodge = AbilityTestMocks.CreateDodge(successChance: 50, initialCounter: 0);
            var foxHealth = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: dodge);

            var initialHealth = foxHealth.Current;

            // Arrange - Normal (non-AoE) attacker
            var attackerGO = new GameObject("NormalAttacker");
            var normalAttack = attackerGO.AddComponent<AnimalAttack>();
            normalAttack.SetDamage(25f);
            normalAttack.SetIsAoE(false); // Explicitly non-AoE

            // Act
            foxHealth.TakeDamageAsync(normalAttack).GetAwaiter().GetResult();

            // Assert
            foxHealth.Current.Should().Be(100f,
                "Fox should NOT take damage because first Dodge use is 100%");
            foxHealth.Current.Should().Be(initialHealth,
                "Health should remain unchanged");

            // Cleanup
            Object.DestroyImmediate(foxHealth.gameObject);
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// INT-AOE-004: NonAoE_Attack_AllowsCounterAttack_CounterTriggers
        /// Regression test: Verifies that non-AoE attacks still allow CounterAttack
        /// </summary>
        [Test]
        public void NonAoE_Attack_AllowsCounterAttack_CounterTriggers()
        {
            // Arrange - Hedgehog with CounterAttack
            var hedgehogGO = new GameObject("Hedgehog");
            var hedgehogHealth = hedgehogGO.AddComponent<AnimalHealth>();
            var hedgehogAnimator = hedgehogGO.AddComponent<AnimalAnimator>();
            var hedgehogAttack = hedgehogGO.AddComponent<AnimalAttack>();

            var abilityManager = new AbilityManager();
            hedgehogHealth.Construct(new Collider[] { hedgehogGO.AddComponent<BoxCollider>() }, abilityManager);
            hedgehogHealth.SetMaxHealth(50f);
            hedgehogAttack.SetDamage(15f);

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(hedgehogHealth, hedgehogAnimator);

            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var counterAttack = new CounterAttack(
                hedgehogHealth,
                hedgehogAnimator,
                hedgehogAttack,
                isOwner: true,
                randomProvider);

            hedgehogHealth.SetAbility(counterAttack);

            // Arrange - Normal attacker with health
            var attackerGO = new GameObject("NormalAttacker");
            var attackerHealth = AttackTestHelper.CreateMockDamageable(100f);
            attackerHealth.transform.SetParent(attackerGO.transform);
            var normalAttack = attackerGO.AddComponent<AnimalAttack>();
            normalAttack.SetDamage(20f);
            normalAttack.SetIsAoE(false); // Explicitly non-AoE


            // Act
            hedgehogHealth.TakeDamageAsync(normalAttack).GetAwaiter().GetResult();

            // Assert - Hedgehog takes damage
            hedgehogHealth.Current.Should().Be(30f,
                "Hedgehog should take 20 damage");

            // Assert - Attacker DOES take counter damage
            attackerHealth.TakeDamageCallCount.Should().Be(1,
                "CounterAttack should trigger against non-AoE attack");
            attackerHealth.Current.Should().Be(85f,
                "Attacker should take 15 counter damage (100 - 15 = 85)");

            // Cleanup
            Object.DestroyImmediate(hedgehogGO);
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// INT-AOE-005: FoxHealth_AoE_DodgeFails_DamageApplied
        /// Verifies FoxHealth-specific logic: AoE bypasses own Dodge
        /// </summary>
        [Test]
        public void FoxHealth_AoE_DodgeFails_DamageApplied()
        {
            // Arrange - FoxHealth with own Dodge (100% normally)
            var foxGO = new GameObject("Fox");
            var foxHealth = foxGO.AddComponent<FoxHealth>();
            var foxAnimator = foxGO.AddComponent<AnimalAnimator>();

            var abilityManager = new AbilityManager();
            foxHealth.Construct(new Collider[] { foxGO.AddComponent<BoxCollider>() }, abilityManager);
            foxHealth.SetMaxHealth(100f);

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(foxHealth, foxAnimator);

            var transformable = new GameObject("Transformable").AddComponent<AnimalMovement>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var dodge = new Dodge(transformable, new Collider[0], successChance: 50, randomProvider);
            foxHealth.SetAbility(dodge);

            // Arrange - AoE attacker
            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetDamage(30f);
            aoeAttack.SetIsAoE(true);

            // Act
            foxHealth.TakeDamageAsync(aoeAttack).GetAwaiter().GetResult();

            // Assert
            foxHealth.Current.Should().Be(70f,
                "FoxHealth should take damage from AoE attack despite having Dodge");

            // Cleanup
            Object.DestroyImmediate(foxGO);
            Object.DestroyImmediate(attackerGO);
            Object.DestroyImmediate(transformable.gameObject);
        }

        /// <summary>
        /// INT-AOE-006: HedgehogHealth_AoE_NoCounter
        /// Verifies HedgehogHealth-specific logic: AoE bypasses own CounterAttack
        /// </summary>
        [Test]
        public void HedgehogHealth_AoE_NoCounter()
        {
            // Arrange - HedgehogHealth with own CounterAttack
            var hedgehogGO = new GameObject("Hedgehog");
            var hedgehogHealth = hedgehogGO.AddComponent<HedgehogHealth>();
            var hedgehogAnimator = hedgehogGO.AddComponent<AnimalAnimator>();
            var hedgehogAttack = hedgehogGO.AddComponent<AnimalAttack>();

            var abilityManager = new AbilityManager();
            hedgehogHealth.Construct(new Collider[] { hedgehogGO.AddComponent<BoxCollider>() }, abilityManager);
            hedgehogHealth.SetMaxHealth(50f);
            hedgehogAttack.SetDamage(15f);

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(hedgehogHealth, hedgehogAnimator);

            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var counterAttack = new CounterAttack(
                hedgehogHealth,
                hedgehogAnimator,
                hedgehogAttack,
                isOwner: true,
                randomProvider);

            hedgehogHealth.SetAbility(counterAttack);

            // Arrange - AoE attacker with health
            var attackerGO = new GameObject("AoEAttacker");
            var attackerHealth = AttackTestHelper.CreateMockDamageable(100f);
            attackerHealth.transform.SetParent(attackerGO.transform);
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetDamage(20f);
            aoeAttack.SetIsAoE(true);


            // Act
            hedgehogHealth.TakeDamageAsync(aoeAttack).GetAwaiter().GetResult();

            // Assert
            hedgehogHealth.Current.Should().Be(30f,
                "HedgehogHealth should take damage");
            attackerHealth.TakeDamageCallCount.Should().Be(0,
                "HedgehogHealth should NOT counter-attack against AoE");

            // Cleanup
            Object.DestroyImmediate(hedgehogGO);
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// INT-AOE-007: MultipleAbilities_AoE_AllBypass
        /// Verifies that when unit has multiple abilities, AoE bypasses all of them
        /// </summary>
        [Test]
        public void MultipleAbilities_AoE_AllBypass()
        {
            // Arrange - Unit with both Dodge (merged) and CounterAttack (merged)
            var dodge = AbilityTestMocks.CreateDodge(successChance: 30, initialCounter: 0);
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: false);

            var unitHealth = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: null,
                mergedAbilities: new IAbility[] { dodge, counterAttack });

            // Arrange - AoE attacker
            var attackerGO = new GameObject("AoEAttacker");
            var attackerHealth = AttackTestHelper.CreateMockDamageable(100f);
            attackerHealth.transform.SetParent(attackerGO.transform);
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetDamage(40f);
            aoeAttack.SetIsAoE(true);


            // Act
            unitHealth.TakeDamageAsync(aoeAttack).GetAwaiter().GetResult();

            // Assert
            unitHealth.Current.Should().Be(60f,
                "Unit should take damage because AoE bypasses all abilities");
            attackerHealth.TakeDamageCallCount.Should().Be(0,
                "No counter-attack should occur");

            // Cleanup
            Object.DestroyImmediate(unitHealth.gameObject);
            Object.DestroyImmediate(attackerGO);
        }
    }
}
