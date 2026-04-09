using System;
using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.Animals.Merge.Commands;
using Code.Animals.Merge.Services;
using Code.Battle.Services;
using UnityEngine;
using Zenject;

namespace Code.Animals.Merge
{
    public class MergeTarget : MonoBehaviour, IRaycastable
    {
        [SerializeField] private PlayerAnimalFacade _facade;
        public event Action<List<AnimalType>> Merge;

        private IMergeUndoService _mergeUndoService;
        private IUnitTracker _unitTracker; 

        private void Awake()
        {
            if (_facade == null)
                _facade = GetComponent<PlayerAnimalFacade>();
        }

        [Inject]
        private void Construct(
            IMergeUndoService mergeUndoService,
            IUnitTracker unitTracker)
        {
            _mergeUndoService = mergeUndoService;
            _unitTracker = unitTracker;
        }

        public bool Accept(PlayerAnimalFacade animal)
        {
            Debug.Log($"[Merge] merge {_facade.name} with {animal.name}");

            if (animal == null)
                return false;

            if (_facade == null)
            {
                Debug.LogError("MergeTarget: target AnimalFacade is not assigned.");
                return false;
            }

            // Prevent self-merge
            if (animal == _facade)
            {
                Debug.LogWarning($"[Merge] Cannot merge {animal.name} with itself.");
                return false;
            }

            // Check if undo is enabled - if so, use command pattern
            if (_mergeUndoService != null && _mergeUndoService.IsEnabled)
            {
                Debug.Log("[MergeTarget] Undo enabled - executing merge via MergeCommand");

                var command = new MergeCommand(
                    this,
                    _facade,
                    animal,
                    _unitTracker);

                var success = _mergeUndoService.ExecuteMerge(command);

                // Note: Visual effects are already invoked by ExecuteMergeDirectly() inside the command
                // No need to invoke them again here

                return success;
            }
            else
            {
                // Undo disabled - execute merge directly (original behavior)
                return ExecuteMergeDirectly(animal);
            }
        }

        /// <summary>
        /// Executes merge directly without command pattern (original behavior)
        /// Called when undo is disabled or for backward compatibility
        /// </summary>
        public bool ExecuteMergeDirectly(PlayerAnimalFacade animal)
        {
            // Collect types for visual effects
            var types = animal.MergeSkills.Select(skill => skill.AnimalType).ToList();
            types.Add(animal.Type);

            // Check: same type or different type?
            if (animal.Type == _facade.Type)
            {
                // Same-type merge: apply formula (HP1 + HP2) × 0.75
                var combinedHP = (animal.GetMaxHealth() + _facade.GetMaxHealth()) * 0.75f;
                var combinedDamage = (animal.GetDamage() + _facade.GetDamage()) * 0.75f;

                _facade.SetHealth(combinedHP);
                _facade.SetDamage(combinedDamage);

                Debug.Log($"Same-type merge: {animal.Type}. New HP: {combinedHP:F1}, New Damage: {combinedDamage:F1}");
            }
            else
            {
                // Different-type merge: apply skills as usual
                if (!animal.MergeSkill.Merge(_facade, animal))
                {
                    Debug.LogWarning($"[MergeTarget] Merge failed for {animal.Type} into {_facade.Type}. Animal not consumed.");
                    return false;
                }
            }

            // Visual effects are invoked only if merge succeeded
            Merge?.Invoke(types);

            // Notify that animal is being removed (before deactivation)
            animal.NotifyRemoved();

            // Deactivate merged animal
            animal.gameObject.SetActive(false);

            return true;
        }
    }
}