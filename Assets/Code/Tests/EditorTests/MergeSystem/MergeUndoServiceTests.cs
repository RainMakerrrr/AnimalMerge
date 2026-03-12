using System;
using Code.Animals.Merge.Commands;
using Code.Animals.Merge.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Code.Tests.EditorTests.MergeSystem
{
    [TestFixture]
    public class MergeUndoServiceTests
    {
        private MergeUndoService _service;
        private IMergeCommand _mockCommand;

        [SetUp]
        public void SetUp()
        {
            _service = new MergeUndoService();
            _mockCommand = Substitute.For<IMergeCommand>();
            _mockCommand.Execute().Returns(true);
            _mockCommand.Undo().Returns(true);
            _mockCommand.Description.Returns("Test Merge");
            _mockCommand.ExecutedAt.Returns(DateTime.Now);
        }

        [Test]
        public void Enable_SetsIsEnabledToTrue()
        {
            // Arrange
            _service.IsEnabled.Should().BeFalse("service should start disabled");

            // Act
            _service.Enable();

            // Assert
            _service.IsEnabled.Should().BeTrue("Enable() should set IsEnabled to true");
        }

        [Test]
        public void Disable_ClearsStackAndSetsIsEnabledToFalse()
        {
            // Arrange
            _service.Enable();
            _service.ExecuteMerge(_mockCommand);
            _service.StackCount.Should().Be(1, "command should be added to stack");

            // Act
            _service.Disable();

            // Assert
            _service.IsEnabled.Should().BeFalse("Disable() should set IsEnabled to false");
            _service.StackCount.Should().Be(0, "Disable() should clear the stack");
        }

        [Test]
        public void ExecuteMerge_WhenEnabled_AddsCommandToStack()
        {
            // Arrange
            _service.Enable();

            // Act
            var result = _service.ExecuteMerge(_mockCommand);

            // Assert
            result.Should().BeTrue("ExecuteMerge should return true when command executes successfully");
            _service.StackCount.Should().Be(1, "command should be added to stack");
            _mockCommand.Received(1).Execute();
        }

        [Test]
        public void ExecuteMerge_WhenDisabled_ExecutesButDoesNotAddToStack()
        {
            // Arrange
            _service.IsEnabled.Should().BeFalse();

            // Act
            var result = _service.ExecuteMerge(_mockCommand);

            // Assert
            result.Should().BeTrue("ExecuteMerge should still execute the command");
            _service.StackCount.Should().Be(0, "command should not be added to stack when disabled");
            _mockCommand.Received(1).Execute();
        }

        [Test]
        public void ExecuteMerge_WhenCommandFails_ReturnsFalse()
        {
            // Arrange
            _service.Enable();
            _mockCommand.Execute().Returns(false);

            // Act
            var result = _service.ExecuteMerge(_mockCommand);

            // Assert
            result.Should().BeFalse("ExecuteMerge should return false when command execution fails");
            _service.StackCount.Should().Be(0, "failed command should not be added to stack");
        }

        [Test]
        public void ExecuteMerge_WithNullCommand_ReturnsFalse()
        {
            // Arrange
            _service.Enable();
            LogAssert.Expect(LogType.Error, "[MergeUndoService] Cannot execute null command");

            // Act
            var result = _service.ExecuteMerge(null);

            // Assert
            result.Should().BeFalse("ExecuteMerge should return false for null command");
            _service.StackCount.Should().Be(0);
        }

        [Test]
        public void UndoLastMerge_WhenStackNotEmpty_UndoesAndRemovesCommand()
        {
            // Arrange
            _service.Enable();
            _service.ExecuteMerge(_mockCommand);

            // Act
            var result = _service.UndoLastMerge();

            // Assert
            result.Should().BeTrue("UndoLastMerge should return true when undo succeeds");
            _service.StackCount.Should().Be(0, "command should be removed from stack after undo");
            _mockCommand.Received(1).Undo();
        }

        [Test]
        public void UndoLastMerge_WhenStackEmpty_ReturnsFalse()
        {
            // Arrange
            _service.Enable();
            _service.StackCount.Should().Be(0);

            // Act
            var result = _service.UndoLastMerge();

            // Assert
            result.Should().BeFalse("UndoLastMerge should return false when stack is empty");
        }

        [Test]
        public void UndoLastMerge_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            _service.Enable();
            _service.ExecuteMerge(_mockCommand);
            _service.Disable();

            // Act
            var result = _service.UndoLastMerge();

            // Assert
            result.Should().BeFalse("UndoLastMerge should return false when service is disabled");
            _mockCommand.DidNotReceive().Undo();
        }

        [Test]
        public void UndoLastMerge_WhenUndoFails_KeepsCommandOnStack()
        {
            // Arrange
            _service.Enable();
            _service.ExecuteMerge(_mockCommand);
            _mockCommand.Undo().Returns(false);
            LogAssert.Expect(LogType.Error, "[MergeUndoService] Failed to undo command: Test Merge");

            // Act
            var result = _service.UndoLastMerge();

            // Assert
            result.Should().BeFalse("UndoLastMerge should return false when undo fails");
            _service.StackCount.Should().Be(1, "command should remain on stack when undo fails");
        }

        [Test]
        public void ExecuteMerge_WhenStackFull_RemovesOldestCommand()
        {
            // Arrange
            _service.Enable();

            // Add 50 commands (max stack size)
            for (int i = 0; i < 50; i++)
            {
                var command = Substitute.For<IMergeCommand>();
                command.Execute().Returns(true);
                command.Description.Returns($"Command {i}");
                _service.ExecuteMerge(command);
            }

            _service.StackCount.Should().Be(50, "stack should be at max capacity");

            // Act - add one more command
            var newCommand = Substitute.For<IMergeCommand>();
            newCommand.Execute().Returns(true);
            newCommand.Description.Returns("New Command");
            _service.ExecuteMerge(newCommand);

            // Assert
            _service.StackCount.Should().Be(50, "stack should remain at max capacity");
            // The oldest command should have been removed
        }

        [Test]
        public void OnStackCountChanged_FiresWhenCommandAdded()
        {
            // Arrange
            _service.Enable();
            int eventFiredCount = 0;
            int lastStackCount = -1;

            _service.OnStackCountChanged += (count) =>
            {
                eventFiredCount++;
                lastStackCount = count;
            };

            // Act
            _service.ExecuteMerge(_mockCommand);

            // Assert
            eventFiredCount.Should().Be(1, "event should fire once");
            lastStackCount.Should().Be(1, "event should pass correct stack count");
        }

        [Test]
        public void OnStackCountChanged_FiresWhenStackCleared()
        {
            // Arrange
            _service.Enable();
            _service.ExecuteMerge(_mockCommand);

            int eventFiredCount = 0;
            int lastStackCount = -1;

            _service.OnStackCountChanged += (count) =>
            {
                eventFiredCount++;
                lastStackCount = count;
            };

            // Act
            _service.Disable();

            // Assert
            eventFiredCount.Should().Be(1, "event should fire when stack is cleared");
            lastStackCount.Should().Be(0, "stack count should be 0 after disable");
        }

        [Test]
        public void OnEnabledStateChanged_FiresWhenEnabled()
        {
            // Arrange
            bool eventFired = false;
            bool lastState = false;

            _service.OnEnabledStateChanged += (enabled) =>
            {
                eventFired = true;
                lastState = enabled;
            };

            // Act
            _service.Enable();

            // Assert
            eventFired.Should().BeTrue("event should fire when enabled");
            lastState.Should().BeTrue("event should pass true when enabled");
        }

        [Test]
        public void OnEnabledStateChanged_FiresWhenDisabled()
        {
            // Arrange
            _service.Enable();

            bool eventFired = false;
            bool lastState = true;

            _service.OnEnabledStateChanged += (enabled) =>
            {
                eventFired = true;
                lastState = enabled;
            };

            // Act
            _service.Disable();

            // Assert
            eventFired.Should().BeTrue("event should fire when disabled");
            lastState.Should().BeFalse("event should pass false when disabled");
        }

        [Test]
        public void MultipleUndos_WorkInReverseOrder()
        {
            // Arrange
            _service.Enable();

            var command1 = Substitute.For<IMergeCommand>();
            command1.Execute().Returns(true);
            command1.Undo().Returns(true);
            command1.Description.Returns("Command 1");

            var command2 = Substitute.For<IMergeCommand>();
            command2.Execute().Returns(true);
            command2.Undo().Returns(true);
            command2.Description.Returns("Command 2");

            var command3 = Substitute.For<IMergeCommand>();
            command3.Execute().Returns(true);
            command3.Undo().Returns(true);
            command3.Description.Returns("Command 3");

            _service.ExecuteMerge(command1);
            _service.ExecuteMerge(command2);
            _service.ExecuteMerge(command3);

            // Act & Assert - undo in reverse order
            _service.UndoLastMerge();
            command3.Received(1).Undo();
            command2.DidNotReceive().Undo();
            command1.DidNotReceive().Undo();

            _service.UndoLastMerge();
            command2.Received(1).Undo();
            command1.DidNotReceive().Undo();

            _service.UndoLastMerge();
            command1.Received(1).Undo();

            _service.StackCount.Should().Be(0, "all commands should be undone");
        }
    }
}
