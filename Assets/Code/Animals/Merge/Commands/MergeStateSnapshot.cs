using System;
using System.Collections.Generic;
using System.Linq;
using Code.Abilities;
using Code.Animals.Facades;
using Code.Animals.Merge.MergeSkills;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Merge.Commands
{
    /// <summary>
    /// Snapshot of an animal's state before merge operation
    /// </summary>
    [Serializable]
    public class MergeStateSnapshot
    {
        // Stats
        public float MaxHealth { get; set; }
        public float CurrentHealth { get; set; }
        public float Damage { get; set; }
        public int TilesPerMove { get; set; }

        // Abilities (we store types to recreate them on undo)
        public List<Type> AbilityTypes { get; set; }

        // Merge skills (reference to the actual skill objects)
        public List<IMergeSkill> MergeSkills { get; set; }

        // Grid position - store the actual GridCell reference instead of just position
        // This preserves which grid (MergeGrid vs GameGrid) the animal was on
        public GridCell GridCell { get; set; }
        public UnitSize UnitSize { get; set; }
        public Direction Direction { get; set; }

        // GameObject state (for source animal)
        public bool WasActive { get; set; }

        // Visual state
        public Vector3 LocalScale { get; set; }

        public MergeStateSnapshot()
        {
            AbilityTypes = new List<Type>();
            MergeSkills = new List<IMergeSkill>();
        }

        /// <summary>
        /// Captures the current state of an animal
        /// </summary>
        public static MergeStateSnapshot Capture(AnimalFacade animal)
        {
            if (animal == null)
            {
                Debug.LogError("[MergeStateSnapshot] Cannot capture state - animal is null");
                return null;
            }

            var snapshot = new MergeStateSnapshot
            {
                MaxHealth = animal.GetMaxHealth(),
                CurrentHealth = animal.GetCurrentHealth(),
                Damage = animal.GetDamage(),
                WasActive = animal.gameObject.activeSelf,
                LocalScale = animal.transform.localScale
            };

            // Capture grid cell if available
            // Store the actual GridCell reference to preserve which grid (MergeGrid/GameGrid) it was on
            if (animal.Movement?.CurrentPathNode != null)
            {
                snapshot.GridCell = animal.Movement.CurrentPathNode;
                snapshot.UnitSize = animal.Movement.UnitSize;
                snapshot.Direction = animal.Movement.Direction;
                snapshot.TilesPerMove = animal.Movement.TilesPerMove;
            }

            // Capture abilities (store types so we can recreate them on undo)
            // We can't easily serialize ability instances, so we store their types
            // This assumes abilities can be recreated from their type
            var abilityManager = animal.Health?.AbilityManager;
            if (abilityManager != null)
            {
                // Get all registered abilities
                // Note: We'll need to implement a way to get ability types from AbilityManager
                // For now, we'll capture what we can
                Debug.Log($"[MergeStateSnapshot] Captured state for {animal.name} - HP: {snapshot.CurrentHealth}/{snapshot.MaxHealth}, Damage: {snapshot.Damage}");
            }

            // Capture merge skills (we can store references since these are persistent objects)
            if (animal.MergeSkills != null)
            {
                snapshot.MergeSkills = new List<IMergeSkill>(animal.MergeSkills);
                Debug.Log($"[MergeStateSnapshot] Captured {snapshot.MergeSkills.Count} merge skills");
            }

            return snapshot;
        }
    }

    /// <summary>
    /// Snapshot of visual effects applied during merge
    /// </summary>
    [Serializable]
    public class VisualStateSnapshot
    {
        public List<AnimalType> AppliedVisualTypes { get; set; }
        public Vector3 ScaleBeforeMerge { get; set; }

        public VisualStateSnapshot()
        {
            AppliedVisualTypes = new List<AnimalType>();
        }
    }
}
