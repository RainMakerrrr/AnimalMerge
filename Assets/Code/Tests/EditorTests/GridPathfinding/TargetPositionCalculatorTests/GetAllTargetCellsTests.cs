using System.Collections.Generic;
using System.Linq;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Tests.EditorTests.Helpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.GridPathfinding.TargetPositionCalculatorTests
{
    /// <summary>
    /// Tests for GetAllTargetCells method - returns all cells occupied by target unit
    /// TC-2.1, TC-2.2, TC-2.3
    /// </summary>
    [TestFixture]
    public class GetAllTargetCellsTests
    {
        private IGridManager _gridManager;
        private TargetPositionCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _gridManager = GridTestHelper.CreateMockGrid();
            _calculator = new TargetPositionCalculator(_gridManager);
        }

        /// <summary>
        /// TC-2.1: Single Cell Target (1×1)
        /// </summary>
        [Test]
        public void GetAllTargetCells_Target1x1_ReturnsSingleCell()
        {
            // Arrange
            var targetTransformable = Substitute.For<ITransformable>();
            var targetCell = _gridManager.GetCell(3, 8);

            targetTransformable.CurrentPathNode.Returns(targetCell);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            // Act
            var result = _calculator.GetAllTargetCells(targetTransformable);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1, "1×1 unit occupies exactly 1 cell");
            result[0].Should().Be(targetCell);
            result[0].GridPosition.Should().Be(new Vector2Int(3, 8));
        }

        /// <summary>
        /// TC-2.2: Medium Target (1×2 vertical)
        /// </summary>
        [Test]
        public void GetAllTargetCells_Target1x2_ReturnsTwoCells()
        {
            // Arrange
            var targetTransformable = Substitute.For<ITransformable>();
            var currentNode = _gridManager.GetCell(3, 8);
            var node1 = _gridManager.GetCell(3, 9);

            var occupiedCells = new List<IGridCell> { currentNode, node1 };

            targetTransformable.CurrentPathNode.Returns(currentNode);
            targetTransformable.GetOccupiedCells().Returns(occupiedCells);

            // Act
            var result = _calculator.GetAllTargetCells(targetTransformable);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2, "1×2 unit occupies 2 cells");
            result.Should().Contain(currentNode);
            result.Should().Contain(node1);

            var positions = result.Select(c => c.GridPosition).ToList();
            positions.Should().Contain(new Vector2Int(3, 8));
            positions.Should().Contain(new Vector2Int(3, 9));
        }

        /// <summary>
        /// TC-2.3: Large Target (2×2)
        /// </summary>
        [Test]
        public void GetAllTargetCells_Target2x2_ReturnsFourCells()
        {
            // Arrange
            var targetTransformable = Substitute.For<ITransformable>();
            var currentNode = _gridManager.GetCell(3, 8);
            var node1 = _gridManager.GetCell(3, 9);
            var node2 = _gridManager.GetCell(4, 8);
            var node3 = _gridManager.GetCell(4, 9);

            var occupiedCells = new List<IGridCell> { currentNode, node1, node2, node3 };

            targetTransformable.CurrentPathNode.Returns(currentNode);
            targetTransformable.GetOccupiedCells().Returns(occupiedCells);

            // Act
            var result = _calculator.GetAllTargetCells(targetTransformable);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4, "2×2 unit occupies 4 cells");
            result.Should().Contain(currentNode);
            result.Should().Contain(node1);
            result.Should().Contain(node2);
            result.Should().Contain(node3);

            var positions = result.Select(c => c.GridPosition).ToList();
            positions.Should().Contain(new Vector2Int(3, 8));
            positions.Should().Contain(new Vector2Int(3, 9));
            positions.Should().Contain(new Vector2Int(4, 8));
            positions.Should().Contain(new Vector2Int(4, 9));
        }

        /// <summary>
        /// Edge case: Null CurrentPathNode
        /// </summary>
        [Test]
        public void GetAllTargetCells_NullCurrentPathNode_ReturnsEmptyList()
        {
            // Arrange
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.CurrentPathNode.Returns((IGridCell)null);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell>());

            // Act
            var result = _calculator.GetAllTargetCells(targetTransformable);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty("GetOccupiedCells returns empty list when no cells occupied");
        }

        /// <summary>
        /// Edge case: Non-AnimalMovement transformable (only CurrentPathNode, no additional Nodes)
        /// </summary>
        [Test]
        public void GetAllTargetCells_NonAnimalMovement_ReturnsOnlyCurrentNode()
        {
            // Arrange
            var targetTransformable = Substitute.For<ITransformable>();
            var targetCell = _gridManager.GetCell(5, 5);

            targetTransformable.CurrentPathNode.Returns(targetCell);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            // Act
            var result = _calculator.GetAllTargetCells(targetTransformable);

            // Assert
            result.Should().HaveCount(1, "simple transformable only has CurrentPathNode");
            result[0].Should().Be(targetCell);
        }

        /// <summary>
        /// Edge case: Simple transformable (validates the method works for basic case)
        /// </summary>
        [Test]
        public void GetAllTargetCells_SimpleTransformable_ReturnsOnlyCurrentNode()
        {
            // Arrange
            var targetTransformable = Substitute.For<ITransformable>();
            var currentNode = _gridManager.GetCell(2, 2);

            targetTransformable.CurrentPathNode.Returns(currentNode);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { currentNode });

            // Act
            var result = _calculator.GetAllTargetCells(targetTransformable);

            // Assert
            result.Should().HaveCount(1, "simple transformable has only CurrentPathNode");
            result[0].Should().Be(currentNode);
            result[0].GridPosition.Should().Be(new Vector2Int(2, 2));
        }
    }
}
