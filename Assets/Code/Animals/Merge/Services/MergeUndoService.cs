using System;
using System.Collections.Generic;
using Code.Animals.Merge.Commands;
using UnityEngine;

namespace Code.Animals.Merge.Services
{
    /// <summary>
    /// Service for managing merge undo operations with a command stack
    /// </summary>
    public class MergeUndoService : IMergeUndoService
    {
        private const int MaxStackSize = 50;

        private readonly Stack<IMergeCommand> _commandStack = new Stack<IMergeCommand>();
        private bool _isEnabled;

        public event Action<int> OnStackCountChanged;
        public event Action<bool> OnEnabledStateChanged;

        public bool IsEnabled => _isEnabled;
        public int StackCount => _commandStack.Count;

        public bool ExecuteMerge(IMergeCommand command)
        {
            if (command == null)
            {
                Debug.LogError("[MergeUndoService] Cannot execute null command");
                return false;
            }

            // Execute the command
            var success = command.Execute();

            if (!success)
            {
                Debug.LogWarning($"[MergeUndoService] Command execution failed: {command.Description}");
                return false;
            }

            // Only add to stack if undo is enabled
            if (_isEnabled)
            {
                // Enforce stack size limit
                if (_commandStack.Count >= MaxStackSize)
                {
                    var oldestCommands = new List<IMergeCommand>();
                    var tempStack = new Stack<IMergeCommand>();

                    // Remove oldest command
                    while (_commandStack.Count > 0)
                    {
                        tempStack.Push(_commandStack.Pop());
                    }

                    // Discard the oldest (bottom) command
                    var discarded = tempStack.Pop();
                    Debug.Log($"[MergeUndoService] Stack full, discarding oldest command: {discarded.Description}");

                    // Restore stack (minus the discarded command)
                    while (tempStack.Count > 0)
                    {
                        _commandStack.Push(tempStack.Pop());
                    }
                }

                _commandStack.Push(command);
                Debug.Log($"[MergeUndoService] Command added to stack: {command.Description} (Stack count: {_commandStack.Count})");

                OnStackCountChanged?.Invoke(_commandStack.Count);
            }

            return true;
        }

        public bool UndoLastMerge()
        {
            if (!_isEnabled)
            {
                Debug.LogWarning("[MergeUndoService] Cannot undo - service is disabled");
                return false;
            }

            if (_commandStack.Count == 0)
            {
                Debug.LogWarning("[MergeUndoService] Cannot undo - stack is empty");
                return false;
            }

            var command = _commandStack.Pop();
            Debug.Log($"[MergeUndoService] Undoing command: {command.Description}");

            var success = command.Undo();

            if (!success)
            {
                Debug.LogError($"[MergeUndoService] Failed to undo command: {command.Description}");
                // Put it back on the stack since undo failed
                _commandStack.Push(command);
                return false;
            }

            Debug.Log($"[MergeUndoService] Successfully undone (Stack count: {_commandStack.Count})");
            OnStackCountChanged?.Invoke(_commandStack.Count);

            return true;
        }

        public void Enable()
        {
            if (_isEnabled) return;

            _isEnabled = true;
            Debug.Log("[MergeUndoService] Undo tracking enabled");

            OnEnabledStateChanged?.Invoke(true);
        }

        public void Disable()
        {
            if (!_isEnabled) return;

            _isEnabled = false;
            _commandStack.Clear();

            Debug.Log("[MergeUndoService] Undo tracking disabled and stack cleared");

            OnEnabledStateChanged?.Invoke(false);
            OnStackCountChanged?.Invoke(0);
        }
    }
}
