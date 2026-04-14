#pragma warning disable CS4014 // Because this call is not awaited, execution continues before the call is completed

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Battle.Services;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using Code.Pathfinding;
using Code.Services.Physics;
using Code.Services.Random;
using NSubstitute;
using UnityEngine;

namespace Code.Tests.EditorTests.BattleSystem.Helpers
{
    public static class BattleTestHelper
    {
        /// <summary>
        /// Creates a mock UnitTracker for testing
        /// </summary>
        public static IUnitTracker CreateMockUnitTracker()
        {
            var tracker = Substitute.For<IUnitTracker>();
            tracker.AlivePlayerUnitsCount.Returns(0);
            tracker.AliveEnemyUnitsCount.Returns(0);
            tracker.HasAliveBoss.Returns(false);
            tracker.GetAlivePlayerUnits().Returns(new List<AnimalFacade>());
            tracker.GetAliveEnemyUnits().Returns(new List<AnimalFacade>());
            return tracker;
        }

        /// <summary>
        /// Creates a real UnitTracker instance for integration tests
        /// </summary>
        public static UnitTracker CreateRealUnitTracker()
        {
            return new UnitTracker();
        }

        /// <summary>
        /// Creates a mock VictoryConditionChecker
        /// </summary>
        public static IVictoryConditionChecker CreateMockVictoryChecker()
        {
            return Substitute.For<IVictoryConditionChecker>();
        }

        /// <summary>
        /// Creates a real VictoryConditionChecker with specified unit tracker
        /// </summary>
        public static VictoryConditionChecker CreateRealVictoryChecker(IUnitTracker unitTracker)
        {
            return new VictoryConditionChecker(unitTracker);
        }

        /// <summary>
        /// Creates a TurnExecutor for testing
        /// </summary>
        public static TurnExecutor CreateTurnExecutor(IUnitTracker tracker, TargetFinder finder)
        {
            return new TurnExecutor(tracker, finder);
        }

        /// <summary>
        /// Creates a mock TargetFinder
        /// </summary>
        public static TargetFinder CreateMockTargetFinder()
        {
            var go = new GameObject("MockTargetFinder");
            var finder = go.AddComponent<TargetFinder>();

            // Setup _colliders to empty array so Setup() doesn't crash
            // and FindClosestTarget returns null
            SetPrivateField(finder, "_colliders", new Collider[0]);

            return finder;
        }

        /// <summary>
        /// Creates a mock AnimalFacade for testing
        /// </summary>
        public static AnimalFacade CreateMockAnimalFacade(
            Vector2Int gridPosition,
            bool isPlayer,
            bool isDead = false,
            AnimalType type = AnimalType.Cheetah,
            bool isBoss = false,
            string customName = null)
        {
            var go = new GameObject(customName ?? $"TestAnimal_{type}");
            var facade = go.AddComponent<TestAnimalFacade>();

            // Create components - use Test versions to prevent async execution
            var health = go.AddComponent<AnimalHealth>();
            var movement = go.AddComponent<TestAnimalMovement>();
            var attack = go.AddComponent<TestAnimalAttack>();
            var animator = go.AddComponent<AnimalAnimator>();
            var collider = go.AddComponent<BoxCollider>();

            // Setup health
            var abilityManager = new AbilityManager();
            health.Construct(new Collider[] { collider }, abilityManager);
            SetPrivateField(health, "_max", 100f);
            SetPrivateField(health, "_animator", animator);
            health.SetMaxHealth(100f);

            if (isDead)
            {
                // Set health to 0 to mark as dead
                health.SetMaxHealth(0f);
            }

            // Setup movement
            var gridCell = CreateMockGridCell(gridPosition);
            SetPrivateField(movement, "_currentPathNode", gridCell);
            SetPrivateField(movement, "_nodes", new List<GridCell> { gridCell });

            // Setup movement Zenject dependencies (mocks with lenient configuration)
            var pathfinder = Substitute.For<IPathfindingService>();
            pathfinder.FindPath(default).ReturnsForAnyArgs(default(PathResult));

            var gridManager = Substitute.For<IGridManager>();
            // Configure properties
            gridManager.Width.Returns(10);
            gridManager.Height.Returns(10);
            gridManager.CellSize.Returns(1f);

            // Configure methods with ReturnsForAnyArgs for lenient behavior
            gridManager.GetCell(0, 0).ReturnsForAnyArgs((IGridCell)null);
            gridManager.IsInBounds(0, 0).ReturnsForAnyArgs(true);
            gridManager.WorldToGrid(default).ReturnsForAnyArgs(Vector2Int.zero);
            gridManager.GridToWorld(0, 0).ReturnsForAnyArgs(Vector3.zero);
            gridManager.GridToWorldCenter(0, 0).ReturnsForAnyArgs(Vector3.zero);
            gridManager.GetUnitWorldPosition(default, default, default).ReturnsForAnyArgs(Vector3.zero);
            gridManager.CanPlaceUnit(default, default, default, default).ReturnsForAnyArgs(false);
            gridManager.GetOccupiedCells(default, default, default).ReturnsForAnyArgs(new List<IGridCell>());
            gridManager.GetNeighborCells(default, default, default).ReturnsForAnyArgs(new List<IGridCell>());

            var animalFactory = Substitute.For<IAnimalFactory>();

            var targetDetector = Substitute.For<ITargetDetector>();
            targetDetector.GetPossibleAttackPositions(default, default, default, default)
                .ReturnsForAnyArgs(new Vector2Int[0]);
            targetDetector.IsCloseToTarget(default, default, default)
                .ReturnsForAnyArgs(false);

            var unitOccupancy = Substitute.For<IUnitOccupancy>();
            unitOccupancy.CurrentCell.Returns(gridCell);
            unitOccupancy.OccupiedCells.Returns(new List<GridCell> { gridCell });

            var movementAnimator = Substitute.For<IMovementAnimator>();

            SetPrivateField(movement, "_pathfinder", pathfinder);
            SetPrivateField(movement, "_gridManager", gridManager);
            SetPrivateField(movement, "_animalFactory", animalFactory);
            SetPrivateField(movement, "_targetDetector", targetDetector);
            SetPrivateField(movement, "_unitOccupancy", unitOccupancy);
            SetPrivateField(movement, "_movementAnimator", movementAnimator);

            // Setup attack
            SetPrivateField(attack, "_animator", animator);
            attack.SetDamage(10f);

            // Setup attack Zenject dependencies (mocks)
            var physicsService = Substitute.For<IPhysicsService>();
            physicsService.OverlapSphereNonAlloc(default, default, default, default)
                .ReturnsForAnyArgs(0);
            physicsService.OverlapCapsuleNonAlloc(default, default, default, default, default)
                .ReturnsForAnyArgs(0);
            SetPrivateField(attack, "_physicsService", physicsService);

            // Setup facade fields via reflection
            SetPrivateField(facade, "_type", type);
            SetPrivateField(facade, "_animator", animator);
            SetPrivateField(facade, "_attack", attack);
            SetPrivateField(facade, "_health", health);
            SetPrivateField(facade, "_movement", movement);
            SetPrivateField(facade, "_colliders", new Collider[] { collider });

            // Setup facade Zenject dependencies (mocks)
            var randomProvider = Substitute.For<IRandomProvider>();
            // Configure Range overloads - use different dummy values to help NSubstitute distinguish them
            randomProvider.Range(0f, 0f).ReturnsForAnyArgs(0.5f);
            randomProvider.Range(0, 0).ReturnsForAnyArgs(0);
            SetPrivateField(facade, "_randomProvider", randomProvider);

            facade.IsBoss = isBoss;

            // Set layer based on player/enemy
            go.layer = LayerMask.NameToLayer(isPlayer ? "Animal" : "Enemy");

            return facade;
        }

        /// <summary>
        /// Creates a mock GridCell at specified position
        /// </summary>
        public static GridCell CreateMockGridCell(Vector2Int gridPosition)
        {
            var go = new GameObject($"Cell_{gridPosition.x}_{gridPosition.y}");

            // GridCell requires Collider and MeshRenderer components
            go.AddComponent<BoxCollider>();
            go.AddComponent<MeshRenderer>();

            var cell = go.AddComponent<GridCell>();

            // Set world position (just convert grid to world for simplicity)
            var worldPos = new Vector3(gridPosition.x, 0, gridPosition.y);
            go.transform.position = worldPos;

            // Initialize the cell (gridManager can be null for tests)
            cell.Initialize(gridPosition.x, gridPosition.y, gridManager: null, isWalkable: true);

            return cell;
        }

        /// <summary>
        /// Cleans all GameObjects from the scene for test isolation
        /// </summary>
        public static void CleanScene()
        {
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                try
                {
                    if (obj == null) continue;
                    if (obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad") continue;

                    UnityEngine.Object.DestroyImmediate(obj);
                }
                catch (Exception)
                {
                    // Object was already destroyed or is invalid, skip
                }
            }
        }

        /// <summary>
        /// Asserts that units executed in the expected order based on grid positions
        /// </summary>
        public static void AssertTurnOrder(
            List<AnimalFacade> executedUnits,
            List<Vector2Int> expectedOrder)
        {
            if (executedUnits.Count != expectedOrder.Count)
            {
                throw new Exception($"Expected {expectedOrder.Count} units but got {executedUnits.Count}");
            }

            for (int i = 0; i < executedUnits.Count; i++)
            {
                var actualPos = executedUnits[i].Movement.CurrentPathNode.GridPosition;
                var expectedPos = expectedOrder[i];

                if (actualPos != expectedPos)
                {
                    throw new Exception(
                        $"Turn order mismatch at index {i}: " +
                        $"expected {expectedPos} but got {actualPos}");
                }
            }
        }

        /// <summary>
        /// Sets a private field on an object using reflection
        /// </summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                Debug.LogError($"Cannot set field '{fieldName}' on null target");
                return;
            }

            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);

                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Debug.LogWarning($"Field '{fieldName}' not found on type '{target.GetType().Name}'");
        }

        /// <summary>
        /// Test implementation of AnimalFacade for testing purposes
        /// </summary>
        private class TestAnimalFacade : AnimalFacade
        {
            public override void InitBehaviours()
            {
                // Empty implementation for testing
            }
        }

        /// <summary>
        /// Test implementation of AnimalMovement that overrides async methods to prevent execution
        /// </summary>
        private class TestAnimalMovement : AnimalMovement
        {
            // Override Move to prevent actual execution
            public override async Task<bool> Move(Vector3 target)
            {
                // Do nothing - just return completed task
                await Task.CompletedTask;

                return false;
            }
        }

        /// <summary>
        /// Test implementation of AnimalAttack that overrides async methods to prevent execution
        /// </summary>
        private class TestAnimalAttack : AnimalAttack
        {
            // Override Attack to prevent actual execution
            public override async Task Attack()
            {
                // Do nothing - just return completed task
                await Task.CompletedTask;
            }

            // Override Attack with target to prevent actual execution
            public override async Task Attack(ITarget target)
            {
                // Do nothing - just return completed task
                await Task.CompletedTask;
            }

            // Override AttackAnimationHandlerAsync to prevent actual execution
            public override async Task AttackAnimationHandlerAsync()
            {
                // Do nothing - just return completed task
                await Task.CompletedTask;
            }
        }
    }
}
