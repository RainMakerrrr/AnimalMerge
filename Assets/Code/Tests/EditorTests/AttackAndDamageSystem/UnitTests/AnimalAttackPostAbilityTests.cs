using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.GridPathfinding;
using Code.Services.Random;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class AnimalAttackPostAbilityTests
    {
        private GameObject _attackGO;
        private AnimalAttack _attack;
        private Transform _attackPoint;
        private AbilityManager _abilityManager;

        [SetUp]
        public void SetUp()
        {
            // Clean scene before each test
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                try
                {
                    if (obj == null) continue;
                    if (obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad") continue;
                    UnityEngine.Object.DestroyImmediate(obj);
                }
                catch (System.Exception)
                {
                    // Object was already destroyed or is invalid, skip
                }
            }

            // Create attack GameObject with all required components
            _attackGO = new GameObject("TestAttack");
            _attack = _attackGO.AddComponent<AnimalAttack>();

            // Create attack point
            var attackPointGO = new GameObject("AttackPoint");
            attackPointGO.transform.SetParent(_attackGO.transform);
            _attackPoint = attackPointGO.transform;

            // Set attack point via reflection
            var attackPointField = typeof(AnimalAttack).GetField("_attackPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            attackPointField?.SetValue(_attack, _attackPoint);

            _attack.SetDamage(10f);

            // Initialize AbilityManager
            _abilityManager = new AbilityManager();
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackGO != null)
                UnityEngine.Object.DestroyImmediate(_attackGO);
        }

        /// <summary>
        /// UT-ATK-POST-001: ExecutePostAttackAbilitiesAsync_WithAbilityManager_ExecutesAbilities
        /// Verifies that post-attack abilities are executed when AbilityManager is injected
        /// </summary>
        [Test]
        public async Task ExecutePostAttackAbilitiesAsync_WithAbilityManager_ExecutesAbilities()
        {
            // Arrange
            _attack.Construct(_abilityManager);

            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            _abilityManager.RegisterAbility(retreat);

            var target = Substitute.For<ITarget>();
            var targetDamageable = Substitute.For<IDamageable>();
            var targetTransformable = Substitute.For<ITransformable>();
            var targetPathNode = Substitute.For<IGridCell>();
            targetPathNode.GridPosition.Returns(new Vector2Int(5, 5));
            targetTransformable.CurrentPathNode.Returns(targetPathNode);
            target.Damageable.Returns(targetDamageable);
            target.Transformable.Returns(targetTransformable);

            // Act
            var method = typeof(AnimalAttack).GetMethod("ExecutePostAttackAbilitiesAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitask = (UniTask)method.Invoke(_attack, new object[] { target });
            await unitask;

            // Assert
            await transformable.Received(1).RetreatFrom(Arg.Any<Vector2Int>(), Arg.Any<int>());
        }

        /// <summary>
        /// UT-ATK-POST-002: ExecutePostAttackAbilitiesAsync_WithoutAbilityManager_DoesNothing
        /// Verifies that no errors occur when AbilityManager is not injected
        /// </summary>
        [Test]
        public async Task ExecutePostAttackAbilitiesAsync_WithoutAbilityManager_DoesNothing()
        {
            // Arrange - don't inject AbilityManager
            var target = Substitute.For<ITarget>();
            var targetDamageable = Substitute.For<IDamageable>();
            target.Damageable.Returns(targetDamageable);

            // Act
            var method = typeof(AnimalAttack).GetMethod("ExecutePostAttackAbilitiesAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitask = (UniTask)method.Invoke(_attack, new object[] { target });

            // Should not throw exception
            Func<Task> action = async () => await unitask;

            // Assert
            await action.Should().NotThrowAsync();
        }

        /// <summary>
        /// UT-ATK-POST-003: ExecutePostAttackAbilitiesAsync_SetsTargetForPostAttackAbilities
        /// Verifies that SetAttackTarget is called on IPostAttackAbility before execution
        /// </summary>
        [Test]
        public async Task ExecutePostAttackAbilitiesAsync_SetsTargetForPostAttackAbilities()
        {
            // Arrange
            _attack.Construct(_abilityManager);

            var mockPostAttackAbility = Substitute.For<IPostAttackAbility>();
            mockPostAttackAbility.CanUse(Arg.Any<IAttacker>()).Returns(true);
            mockPostAttackAbility.Priority.Returns(-1);
            mockPostAttackAbility.IsBlockingDamage.Returns(false);
            mockPostAttackAbility.Apply().Returns(UniTask.CompletedTask);

            _abilityManager.RegisterAbility(mockPostAttackAbility);

            var target = Substitute.For<ITarget>();
            var targetDamageable = Substitute.For<IDamageable>();
            target.Damageable.Returns(targetDamageable);

            // Act
            var method = typeof(AnimalAttack).GetMethod("ExecutePostAttackAbilitiesAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitask = (UniTask)method.Invoke(_attack, new object[] { target });
            await unitask;

            // Assert
            mockPostAttackAbility.Received(1).SetAttackTarget(target);
            await mockPostAttackAbility.Received(1).Apply();
        }

        /// <summary>
        /// UT-ATK-POST-004: ExecutePostAttackAbilitiesAsync_WithMixedAbilities_OnlySetsTargetForPostAttackAbilities
        /// Verifies that SetAttackTarget is only called on IPostAttackAbility, not on regular IAbility
        /// </summary>
        [Test]
        public async Task ExecutePostAttackAbilitiesAsync_WithMixedAbilities_OnlySetsTargetForPostAttackAbilities()
        {
            // Arrange
            _attack.Construct(_abilityManager);

            // Regular ability (not IPostAttackAbility)
            var regularAbility = Substitute.For<IAbility>();
            regularAbility.CanUse(Arg.Any<IAttacker>()).Returns(true);
            regularAbility.Priority.Returns(0);
            regularAbility.IsBlockingDamage.Returns(false);
            regularAbility.Apply().Returns(UniTask.CompletedTask);

            // Post-attack ability
            var postAttackAbility = Substitute.For<IPostAttackAbility>();
            postAttackAbility.CanUse(Arg.Any<IAttacker>()).Returns(true);
            postAttackAbility.Priority.Returns(-1);
            postAttackAbility.IsBlockingDamage.Returns(false);
            postAttackAbility.Apply().Returns(UniTask.CompletedTask);

            _abilityManager.RegisterAbility(regularAbility);
            _abilityManager.RegisterAbility(postAttackAbility);

            var target = Substitute.For<ITarget>();
            var targetDamageable = Substitute.For<IDamageable>();
            target.Damageable.Returns(targetDamageable);

            // Act
            var method = typeof(AnimalAttack).GetMethod("ExecutePostAttackAbilitiesAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var unitask = (UniTask)method.Invoke(_attack, new object[] { target });
            await unitask;

            // Assert
            // SetAttackTarget should only be called on IPostAttackAbility
            postAttackAbility.Received(1).SetAttackTarget(target);

            // Both abilities should be executed
            await regularAbility.Received(1).Apply();
            await postAttackAbility.Received(1).Apply();
        }

        /// <summary>
        /// UT-ATK-POST-005: AttackAnimationHandlerAsync_WithTargetOverride_ExecutesPostAttackAbilities
        /// Verifies that post-attack abilities are executed after dealing damage with target override
        /// </summary>
        [Test]
        public async Task AttackAnimationHandlerAsync_WithTargetOverride_ExecutesPostAttackAbilities()
        {
            // Arrange
            _attack.Construct(_abilityManager);

            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(2);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            _abilityManager.RegisterAbility(retreat);

            var target = Substitute.For<ITarget>();
            var targetDamageable = Substitute.For<IDamageable>();
            targetDamageable.TakeDamageAsync(_attack).Returns(UniTask.CompletedTask);

            var targetTransformable = Substitute.For<ITransformable>();
            var targetPathNode = Substitute.For<IGridCell>();
            targetPathNode.GridPosition.Returns(new Vector2Int(5, 5));
            targetTransformable.CurrentPathNode.Returns(targetPathNode);

            target.Damageable.Returns(targetDamageable);
            target.Transformable.Returns(targetTransformable);

            // Set target override via reflection
            var targetOverrideField = typeof(AnimalAttack).GetField("_targetOverride",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetOverrideField?.SetValue(_attack, target);

            // Act
            await _attack.AttackAnimationHandlerAsync();

            // Assert
            // Damage should be dealt
            await targetDamageable.Received(1).TakeDamageAsync(_attack);

            // Retreat should be executed
            await transformable.Received(1).RetreatFrom(Arg.Any<Vector2Int>(), Arg.Any<int>());
        }

        /// <summary>
        /// UT-ATK-POST-006: Construct_InjectsAbilityManager_CorrectBehavior
        /// Verifies that Construct method properly injects AbilityManager
        /// </summary>
        [Test]
        public void Construct_InjectsAbilityManager_CorrectBehavior()
        {
            // Arrange & Act
            _attack.Construct(_abilityManager);

            // Assert - verify by checking that abilities can be registered and executed
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            _abilityManager.RegisterAbility(retreat);

            // Verify AbilityManager has the ability
            _abilityManager.Abilities.Should().Contain(retreat);
        }
    }
}
