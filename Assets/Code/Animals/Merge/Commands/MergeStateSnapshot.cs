using System;
using System.Collections.Generic;
using System.Linq;
using Code.Abilities;
using Code.Animals.Facades;
using Code.Animals.Merge.MergeAttributes;
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

        // Abilities (we store references to actual ability instances)
        public List<IAbility> Abilities { get; set; }

        // Merge skills (reference to the actual skill objects)
        public List<IMergeSkill> MergeSkills { get; set; }

        // Accumulated visual attributes from previous merges
        public List<VisualMergeAttribute> AccumulatedVisualAttributes { get; set; }

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
            Abilities = new List<IAbility>();
            MergeSkills = new List<IMergeSkill>();
            AccumulatedVisualAttributes = new List<VisualMergeAttribute>();
        }

        /// <summary>
        /// Captures the current state of an animal
        /// </summary>
        public static MergeStateSnapshot Capture(PlayerAnimalFacade animal)
        {
            if (animal == null)
            {
                Debug.LogError("[MergeStateSnapshot] Cannot capture state - animal is null");
                return null;
            }

            var scaleAnimator = animal.ScaleAnimator;

            var snapshot = new MergeStateSnapshot
            {
                MaxHealth = animal.GetMaxHealth(),
                CurrentHealth = animal.GetCurrentHealth(),
                Damage = animal.GetDamage(),
                WasActive = animal.gameObject.activeSelf,
                LocalScale = scaleAnimator != null ? scaleAnimator.LogicalScale : animal.transform.localScale
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

            // Capture abilities - store references to actual ability instances
            var abilityManager = animal.AbilityManager;
            if (abilityManager != null)
            {
                snapshot.Abilities = new List<IAbility>(abilityManager.Abilities);
                Debug.Log($"[MergeStateSnapshot] Captured {snapshot.Abilities.Count} abilities for {animal.name}");
            }

            Debug.Log($"[MergeStateSnapshot] Captured state for {animal.name} - HP: {snapshot.CurrentHealth}/{snapshot.MaxHealth}, Damage: {snapshot.Damage}");

            // Capture merge skills (we can store references since these are persistent objects)
            if (animal.MergeSkills != null)
            {
                snapshot.MergeSkills = new List<IMergeSkill>(animal.MergeSkills);
                Debug.Log($"[MergeStateSnapshot] Captured {snapshot.MergeSkills.Count} merge skills");
            }

            // Capture accumulated visual attributes
            if (animal.AccumulatedVisualAttributes != null)
            {
                snapshot.AccumulatedVisualAttributes = new List<VisualMergeAttribute>(animal.AccumulatedVisualAttributes);
                Debug.Log($"[MergeStateSnapshot] Captured {snapshot.AccumulatedVisualAttributes.Count} accumulated visual attributes");
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
        public List<VisualMergeAttribute> AppliedVisualAttributes { get; set; }
        public Vector3 ScaleBeforeMerge { get; set; }

        public VisualStateSnapshot()
        {
            AppliedVisualAttributes = new List<VisualMergeAttribute>();
        }
    }
}
