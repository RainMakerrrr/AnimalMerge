using System.Collections.Generic;
using Code.GridPathfinding;
using Code.GridPathfinding.Config;
using FluentAssertions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Code.Tests.EditorTests.GridPathfinding
{
    /// <summary>
    /// Tests for the configurable grid size delivered through GridConfig assets.
    /// </summary>
    [TestFixture]
    public class GridConfigTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in _createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void GridConfig_CreatedInstance_ExposesDefaultDimensions()
        {
            var config = CreateConfig(GridConfig.DefaultWidth, GridConfig.DefaultHeight, GridConfig.DefaultCellSize);

            config.Width.Should().Be(GridConfig.DefaultWidth);
            config.Height.Should().Be(GridConfig.DefaultHeight);
            config.CellSize.Should().Be(GridConfig.DefaultCellSize);
        }

        [Test]
        public void GridManager_WithAssignedConfig_ReportsConfiguredDimensions()
        {
            var config = CreateConfig(8, 2, 0.5f);
            var gridManager = CreateGridManager();

            AssignConfig(gridManager, config);

            gridManager.Width.Should().Be(8);
            gridManager.Height.Should().Be(2);
            gridManager.CellSize.Should().Be(0.5f);
        }

        /// <summary>
        /// Edit-mode behaviour only: outside play mode GridManager re-reads the config on every
        /// access so that gizmos follow the asset while it is being tweaked. At runtime the
        /// dimensions are latched in Awake and editing the asset does NOT resize a live grid.
        /// </summary>
        [Test]
        public void GridManager_InEditMode_ConfigChangedAfterAssignment_ReportsUpdatedDimensions()
        {
            var config = CreateConfig(8, 10, 1f);
            var gridManager = CreateGridManager();
            AssignConfig(gridManager, config);

            SetConfigDimensions(config, 12, 4, 2f);

            gridManager.Width.Should().Be(12);
            gridManager.Height.Should().Be(4);
            gridManager.CellSize.Should().Be(2f);
        }

        /// <summary>
        /// Rebuild() supplies dimensions for the current session only, and they must survive the
        /// edit-mode re-read of the config that every Width/Height/CellSize access triggers.
        /// </summary>
        [Test]
        public void GridManager_InEditMode_RebuildWithExplicitSize_OverridesConfigDimensions()
        {
            var config = CreateConfig(8, 10, 1f);
            var gridManager = CreateGridManager();
            AssignConfig(gridManager, config);

            gridManager.Rebuild(12, 4, 2f);

            gridManager.Width.Should().Be(12);
            gridManager.Height.Should().Be(4);
            gridManager.CellSize.Should().Be(2f);
        }

        [Test]
        public void GridManager_WithoutConfig_FallsBackToDefaultDimensions()
        {
            var gridManager = CreateGridManager();

            gridManager.Width.Should().Be(GridConfig.DefaultWidth);
            gridManager.Height.Should().Be(GridConfig.DefaultHeight);
            gridManager.CellSize.Should().Be(GridConfig.DefaultCellSize);
        }

        private GridConfig CreateConfig(int width, int height, float cellSize)
        {
            var config = ScriptableObject.CreateInstance<GridConfig>();
            _createdObjects.Add(config);

            SetConfigDimensions(config, width, height, cellSize);

            return config;
        }

        private GridManager CreateGridManager()
        {
            var gridObject = new GameObject(nameof(GridManager));
            _createdObjects.Add(gridObject);

            return gridObject.AddComponent<GridManager>();
        }

        private static void SetConfigDimensions(GridConfig config, int width, int height, float cellSize)
        {
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("_width").intValue = width;
            serializedConfig.FindProperty("_height").intValue = height;
            serializedConfig.FindProperty("_cellSize").floatValue = cellSize;
            serializedConfig.ApplyModifiedProperties();
        }

        private static void AssignConfig(GridManager gridManager, GridConfig config)
        {
            var serializedGridManager = new SerializedObject(gridManager);
            serializedGridManager.FindProperty("_config").objectReferenceValue = config;
            serializedGridManager.ApplyModifiedProperties();
        }
    }
}
