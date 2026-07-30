using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Code.GridPathfinding;
using Code.GridPathfinding.Config;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Code.Tests.PlayModeTests.GridPathfinding
{
    /// <summary>
    /// Play Mode coverage for the config → grid path. Awake never runs for a component added
    /// in Edit Mode, so InitializeGrid() — the code that actually consumes GridConfig, sizes
    /// the cell array and scales the spawned cells — can only be exercised at runtime.
    /// </summary>
    [TestFixture]
    public class GridConfigRuntimeTests
    {
        private const int TestWidth = 6;
        private const int TestHeight = 3;
        private const float TestCellSize = 2f;

        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in _createdObjects)
            {
                if (createdObject != null)
                    Object.Destroy(createdObject);
            }

            _createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator GridManager_AwakenWithConfig_BuildsGridMatchingConfigDimensions()
        {
            var gridManager = CreateInactiveGridManager(TestWidth, TestHeight, TestCellSize);

            gridManager.gameObject.SetActive(true);
            yield return null;

            gridManager.Width.Should().Be(TestWidth);
            gridManager.Height.Should().Be(TestHeight);
            gridManager.CellSize.Should().Be(TestCellSize);
        }

        [UnityTest]
        public IEnumerator GridManager_AwakenWithConfig_SpawnsCellForEveryConfiguredCoordinate()
        {
            var gridManager = CreateInactiveGridManager(TestWidth, TestHeight, TestCellSize);

            gridManager.gameObject.SetActive(true);
            yield return null;

            gridManager.GetCell(0, 0).Should().NotBeNull("the origin cell is inside the configured grid");
            gridManager.GetCell(TestWidth - 1, TestHeight - 1).Should()
                .NotBeNull("the far corner is inside the configured grid");
            gridManager.GetCell(TestWidth, TestHeight).Should()
                .BeNull("coordinates past the configured size are out of bounds");
        }

        [UnityTest]
        public IEnumerator GridManager_AwakenWithConfig_ScalesSpawnedCellsToConfiguredCellSize()
        {
            var gridManager = CreateInactiveGridManager(TestWidth, TestHeight, TestCellSize);

            gridManager.gameObject.SetActive(true);
            yield return null;

            var cell = (GridCell)gridManager.GetCell(0, 0);
            cell.transform.localScale.x.Should().BeApproximately(TestCellSize, 0.0001f);
            cell.transform.localScale.z.Should().BeApproximately(TestCellSize, 0.0001f);
        }

        /// <summary>
        /// Regression cover for the bootstrap ordering crash: GameBootstrapper.Awake() runs the
        /// whole bootstrap — enemy spawning included — synchronously, and Unity gives no ordering
        /// guarantee between Awake calls on different GameObjects, so EnemySpawnService could
        /// reach GetCell() while the grid was still unbuilt and hit a NullReferenceException.
        /// An unawakened GridManager stands in for that window here.
        /// </summary>
        [UnityTest]
        public IEnumerator GridManager_QueriedBeforeAwake_BuildsGridInsteadOfThrowing()
        {
            var gridManager = CreateInactiveGridManager(TestWidth, TestHeight, TestCellSize);

            // Deliberately NOT activated: Awake has not run, so _cells is still null.
            gridManager.GetCell(0, 0).Should()
                .NotBeNull("the grid must build itself on first access, whatever the Awake order");
            gridManager.GetCell(TestWidth - 1, TestHeight - 1).Should().NotBeNull();
            gridManager.GetCell(TestWidth, TestHeight).Should().BeNull();

            yield return null;
        }

        /// <summary>
        /// Activating a grid that already built itself lazily must not spawn a second set of cells.
        /// </summary>
        [UnityTest]
        public IEnumerator GridManager_AwakenAfterLazyBuild_DoesNotDuplicateCells()
        {
            var gridManager = CreateInactiveGridManager(TestWidth, TestHeight, TestCellSize);

            var lazilyBuiltCell = gridManager.GetCell(0, 0);

            gridManager.gameObject.SetActive(true);
            yield return null;

            gridManager.GetCell(0, 0).Should()
                .BeSameAs(lazilyBuiltCell, "Awake must reuse the grid the lazy path already built");
        }

        /// <summary>
        /// Builds a GridManager on an inactive GameObject so the config can be injected before
        /// Awake fires; activating the object is what triggers the real initialization path.
        /// </summary>
        private GridManager CreateInactiveGridManager(int width, int height, float cellSize)
        {
            var gridObject = new GameObject(nameof(GridManager));
            gridObject.SetActive(false);
            _createdObjects.Add(gridObject);

            var gridManager = gridObject.AddComponent<GridManager>();
            SetPrivateField(gridManager, "_config", CreateConfig(width, height, cellSize));
            SetPrivateField(gridManager, "_cellPrefab", CreateCellTemplate());

            return gridManager;
        }

        private GridConfig CreateConfig(int width, int height, float cellSize)
        {
            var config = ScriptableObject.CreateInstance<GridConfig>();
            _createdObjects.Add(config);

            SetPrivateField(config, "_width", width);
            SetPrivateField(config, "_height", height);
            SetPrivateField(config, "_cellSize", cellSize);

            return config;
        }

        /// <summary>
        /// Stands in for the GridCell prefab: Instantiate() accepts a scene object just as well,
        /// and keeping it inactive keeps its colliders out of the running physics scene.
        /// </summary>
        private GridCell CreateCellTemplate()
        {
            var cellObject = new GameObject("CellTemplate");
            cellObject.SetActive(false);
            _createdObjects.Add(cellObject);

            cellObject.AddComponent<BoxCollider>();

            return cellObject.AddComponent<GridCell>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            field.Should().NotBeNull($"{target.GetType().Name} must declare the field '{fieldName}'");
            field.SetValue(target, value);
        }
    }
}
