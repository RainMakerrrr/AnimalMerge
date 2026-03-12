using System;

namespace Code.Animals.Merge.Commands
{
    /// <summary>
    /// Interface for merge command that supports Execute and Undo operations
    /// </summary>
    public interface IMergeCommand
    {
        /// <summary>
        /// Executes the merge operation
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        bool Execute();

        /// <summary>
        /// Undoes the merge operation, restoring previous state
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        bool Undo();

        /// <summary>
        /// Timestamp when the command was executed
        /// </summary>
        DateTime ExecutedAt { get; }

        /// <summary>
        /// Human-readable description of the merge operation
        /// </summary>
        string Description { get; }
    }
}
