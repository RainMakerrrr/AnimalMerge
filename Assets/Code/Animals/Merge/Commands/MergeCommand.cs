using System;
using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.Battle.Services;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Merge.Commands
{
    /// <summary>
    /// Command for executing and undoing merge operations
    /// Captures state before merge, executes merge, and can restore state on undo
    /// </summary>
    public class MergeCommand : IMergeCommand
    {
        private readonly MergeTarget _mergeTarget;
        private readonly AnimalFacade _targetAnimal;
        private readonly AnimalFacade _sourceAnimal;
        private readonly IUnitTracker _unitTracker;

        // State snapshots
        private MergeStateSnapshot _targetStateBefore;
        private MergeStateSnapshot _sourceStateBefore;
        private VisualStateSnapshot _visualState;

        // Chicken clone tracking
        private AnimalFacade _chickenClone;

        // Execution metadata
        private DateTime _executedAt;
        private string _description;

        public DateTime ExecutedAt => _executedAt;
        public string Description => _description;

        public MergeCommand(
            MergeTarget mergeTarget,
            AnimalFacade targetAnimal,
            AnimalFacade sourceAnimal,
            IUnitTracker unitTracker)
        {
            _mergeTarget = mergeTarget ?? throw new ArgumentNullException(nameof(mergeTarget));
            _targetAnimal = targetAnimal ?? throw new ArgumentNullException(nameof(targetAnimal));
            _sourceAnimal = sourceAnimal ?? throw new ArgumentNullException(nameof(sourceAnimal));
            _unitTracker = unitTracker ?? throw new ArgumentNullException(nameof(unitTracker));

            _description = $"Merge {sourceAnimal.Type} into {targetAnimal.Type}";
        }

        public bool Execute()
        {
            try
            {
                Debug.Log($"[MergeCommand] Executing: {_description}");

                // Step 1: Capture state BEFORE merge
                _targetStateBefore = MergeStateSnapshot.Capture(_targetAnimal);
                _sourceStateBefore = MergeStateSnapshot.Capture(_sourceAnimal);
                _visualState = new VisualStateSnapshot
                {
                    ScaleBeforeMerge = _targetAnimal.transform.localScale
                };

                if (_targetStateBefore == null || _sourceStateBefore == null)
                {
                    Debug.LogError("[MergeCommand] Failed to capture state before merge");
                    return false;
                }

                // Step 2: Track units before merge (for chicken clone detection)
                var unitsBeforeMerge = _unitTracker.GetAlivePlayerUnits().ToList();

                // Step 3: Execute the actual merge using MergeTarget's original logic
                // We'll call the private ExecuteMergeDirectly method that we'll add to MergeTarget
                // For now, we'll use the public Accept method
                var success = ExecuteMergeDirectly();

                if (!success)
                {
                    Debug.LogWarning("[MergeCommand] Merge execution failed");
                    return false;
                }

                _executedAt = DateTime.Now;

                // Step 4: Detect if a chicken clone was created
                DetectChickenClone(unitsBeforeMerge);

                Debug.Log($"[MergeCommand] Successfully executed merge. Chicken clone: {(_chickenClone != null ? _chickenClone.name : "none")}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MergeCommand] Exception during execution: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public bool Undo()
        {
            try
            {
                Debug.Log($"[MergeCommand] Undoing: {_description}");

                // Step 1: Restore target animal stats
                RestoreTargetStats();

                // Step 2: Remove abilities that were added during merge
                RemoveAddedAbilities();

                // Step 3: Remove merge skills that were added
                RemoveAddedMergeSkills();

                // Step 4: Restore visual state (scale, visual effects)
                RestoreVisualState();

                // Step 5: Reactivate source animal
                ReactivateSourceAnimal();

                // Step 6: Delete chicken clone if it was created
                if (_chickenClone != null)
                {
                    DeleteChickenClone();
                }

                Debug.Log($"[MergeCommand] Successfully undone merge");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MergeCommand] Exception during undo: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Executes the merge directly using MergeTarget's ExecuteMergeDirectly method
        /// </summary>
        private bool ExecuteMergeDirectly()
        {
            Debug.Log($"[MergeCommand] Executing merge through MergeTarget.ExecuteMergeDirectly()");

            // Collect types for visual effects tracking (for undo)
            var types = _sourceAnimal.MergeSkills.Select(skill => skill.AnimalType).ToList();
            types.Add(_sourceAnimal.Type);

            // Store visual types for undo
            _visualState.AppliedVisualTypes = new List<AnimalType>(types);
            Debug.Log($"[MergeCommand] Stored visual types for undo: {string.Join(", ", _visualState.AppliedVisualTypes)}");

            // Execute merge using MergeTarget's method
            // Note: ExecuteMergeDirectly will fire the Merge event for visual effects
            return _mergeTarget.ExecuteMergeDirectly(_sourceAnimal);
        }

        private void RestoreTargetStats()
        {
            Debug.Log($"[MergeCommand] Restoring target stats: HP {_targetStateBefore.MaxHealth}, Damage {_targetStateBefore.Damage}");

            _targetAnimal.SetHealth(_targetStateBefore.MaxHealth);
            _targetAnimal.Health.SetCurrentHealth(_targetStateBefore.CurrentHealth);
            _targetAnimal.SetDamage(_targetStateBefore.Damage);
            _targetAnimal.SetTilesPerMove(_targetStateBefore.TilesPerMove);

            // Restore transform scale
            _targetAnimal.transform.localScale = _targetStateBefore.LocalScale;
        }

        private void RemoveAddedAbilities()
        {
            var abilityManager = _targetAnimal.AbilityManager;
            if (abilityManager == null)
            {
                Debug.LogWarning("[MergeCommand] AbilityManager is null on target animal");
                return;
            }

            if (_targetStateBefore.Abilities == null)
            {
                Debug.LogWarning("[MergeCommand] No abilities saved before merge");
                return;
            }

            // Get current abilities
            var currentAbilities = abilityManager.Abilities.ToList();

            // Find abilities that were added during merge (present now, but not before)
            var addedAbilities = currentAbilities.Where(a => !_targetStateBefore.Abilities.Contains(a)).ToList();

            if (addedAbilities.Count == 0)
            {
                Debug.Log("[MergeCommand] No new abilities to remove");
                return;
            }

            // Remove each added ability
            foreach (var ability in addedAbilities)
            {
                var removed = abilityManager.UnregisterAbility(ability);
                if (removed)
                {
                    Debug.Log($"[MergeCommand] Removed ability: {ability.GetType().Name}");
                }
                else
                {
                    Debug.LogWarning($"[MergeCommand] Failed to remove ability: {ability.GetType().Name}");
                }
            }

            Debug.Log($"[MergeCommand] Removed {addedAbilities.Count} abilities added during merge");
        }

        private void RemoveAddedMergeSkills()
        {
            // Restore merge skills to the state before merge
            if (_targetStateBefore.MergeSkills != null)
            {
                _targetAnimal.MergeSkills.Clear();
                _targetAnimal.MergeSkills.AddRange(_targetStateBefore.MergeSkills);
                Debug.Log($"[MergeCommand] Restored {_targetAnimal.MergeSkills.Count} merge skills to target");
            }
        }

        private void RestoreVisualState()
        {
            // Restore scale
            _targetAnimal.transform.localScale = _visualState.ScaleBeforeMerge;
            Debug.Log($"[MergeCommand] Restored scale to {_visualState.ScaleBeforeMerge}");

            // Debug: Check state
            Debug.Log($"[MergeCommand] RestoreVisualState - MergeView: {(_targetAnimal.MergeView != null ? "Present" : "NULL")}, " +
                      $"AppliedVisualTypes: {(_visualState.AppliedVisualTypes != null ? _visualState.AppliedVisualTypes.Count.ToString() : "NULL")}");

            // Undo visual merge attributes (fox tail, elephant size, etc.)
            if (_targetAnimal.MergeView != null && _visualState.AppliedVisualTypes != null && _visualState.AppliedVisualTypes.Count > 0)
            {
                Debug.Log($"[MergeCommand] Calling UndoVisuals for types: {string.Join(", ", _visualState.AppliedVisualTypes)}");
                _targetAnimal.MergeView.UndoVisuals(_visualState.AppliedVisualTypes);
                Debug.Log($"[MergeCommand] Undone {_visualState.AppliedVisualTypes.Count} visual effects");
            }
            else
            {
                if (_targetAnimal.MergeView == null)
                    Debug.LogError("[MergeCommand] MergeView is NULL on target animal!");
                if (_visualState.AppliedVisualTypes == null)
                    Debug.LogError("[MergeCommand] AppliedVisualTypes is NULL!");
                if (_visualState.AppliedVisualTypes != null && _visualState.AppliedVisualTypes.Count == 0)
                    Debug.LogError("[MergeCommand] AppliedVisualTypes is empty!");
            }
        }

        private void ReactivateSourceAnimal()
        {
            Debug.Log($"[MergeCommand] Reactivating source animal {_sourceAnimal.name}");

            // Reactivate the GameObject
            _sourceAnimal.gameObject.SetActive(true);

            // Restore grid position using the saved GridCell (preserves correct grid - MergeGrid vs GameGrid)
            if (_sourceStateBefore.GridCell != null)
            {
                _sourceAnimal.Movement.SetNewNode(_sourceStateBefore.GridCell, true);
                Debug.Log($"[MergeCommand] Restored source position to {_sourceStateBefore.GridCell.GridPosition} on its original grid");
            }
            else
            {
                Debug.LogWarning($"[MergeCommand] No grid cell saved for source animal - cannot restore position");
            }
        }

        private void DetectChickenClone(List<AnimalFacade> unitsBeforeMerge)
        {
            // Chicken clones are only created when merging with Chicken
            if (_sourceAnimal.Type != AnimalType.Chicken && !_sourceAnimal.MergeSkills.Any(s => s.AnimalType == AnimalType.Chicken))
            {
                return;
            }

            // Get units after merge
            var unitsAfterMerge = _unitTracker.GetAlivePlayerUnits();

            // Find new units (units that weren't in the before list)
            var newUnits = unitsAfterMerge.Where(u => !unitsBeforeMerge.Contains(u)).ToList();

            // Look for a clone of the same type as the target
            foreach (var newUnit in newUnits)
            {
                if (newUnit.Type == _targetAnimal.Type && newUnit != _targetAnimal)
                {
                    _chickenClone = newUnit;
                    Debug.Log($"[MergeCommand] Detected chicken clone: {_chickenClone.name}");
                    break;
                }
            }

            if (_chickenClone == null && newUnits.Count > 0)
            {
                Debug.LogWarning($"[MergeCommand] Found {newUnits.Count} new units but none matched target type {_targetAnimal.Type}");
            }
        }

        private void DeleteChickenClone()
        {
            if (_chickenClone == null)
            {
                return;
            }

            Debug.Log($"[MergeCommand] Deleting chicken clone {_chickenClone.name}");

            // Clear grid cells
            _chickenClone.NotifyRemoved();

            // Destroy the GameObject
            UnityEngine.Object.Destroy(_chickenClone.gameObject);

            _chickenClone = null;
        }
    }
}
