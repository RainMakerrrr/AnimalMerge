using System;
using Code.Animals.Merge.Commands;

namespace Code.Animals.Merge.Services
{
    /// <summary>
    /// Service interface for managing merge undo operations
    /// </summary>
    public interface IMergeUndoService
    {
        /// <summary>
        /// Fired when the command stack count changes
        /// </summary>
        event Action<int> OnStackCountChanged;

        /// <summary>
        /// Fired when the enabled state changes
        /// </summary>
        event Action<bool> OnEnabledStateChanged;

        /// <summary>
        /// Indicates whether undo tracking is currently enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Current number of commands in the undo stack
        /// </summary>
        int StackCount { get; }

        /// <summary>
        /// Executes a merge command and adds it to the undo stack if enabled
        /// </summary>
        bool ExecuteMerge(IMergeCommand command);

        /// <summary>
        /// Undoes the last merge operation
        /// </summary>
        bool UndoLastMerge();

        /// <summary>
        /// Enables undo tracking
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables undo tracking and clears the stack
        /// </summary>
        void Disable();
    }
}
