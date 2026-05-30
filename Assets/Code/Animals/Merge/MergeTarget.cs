using System;
using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Animals.Merge.Commands;
using Code.Animals.Merge.MergeAttributes;
using Code.Animals.Merge.Services;
using Code.Battle.Services;
using UnityEngine;
using Zenject;

namespace Code.Animals.Merge
{
    public class MergeTarget : MonoBehaviour, IRaycastable
    {
        [SerializeField] private PlayerAnimalFacade _facade;
        public event Action<List<VisualMergeAttribute>> Merge;

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

                return success;
            }
            else
            {
                return ExecuteMergeDirectly(animal);
            }
        }

        /// <summary>
        /// Executes merge directly without command pattern (original behavior)
        /// Called when undo is disabled or for backward compatibility
        /// </summary>
        public bool ExecuteMergeDirectly(PlayerAnimalFacade animal)
        {
            // Collect visual attributes: source's own + accumulated from previous merges
            var visualAttributes = new List<VisualMergeAttribute>(
                animal.GetComponentsInChildren<VisualMergeAttribute>(true));
            visualAttributes.AddRange(animal.AccumulatedVisualAttributes);

            // Check: same type or different type?
            if (animal.Type == _facade.Type)
            {
                var combinedHP = (animal.GetMaxHealth() + _facade.GetMaxHealth()) * 0.75f;
                var combinedDamage = (animal.GetDamage() + _facade.GetDamage()) * 0.75f;

                _facade.SetHealth(combinedHP);
                _facade.SetDamage(combinedDamage);

                Debug.Log($"Same-type merge: {animal.Type}. New HP: {combinedHP:F1}, New Damage: {combinedDamage:F1}");
            }
            else
            {
                if (!animal.MergeSkill.Merge(_facade, animal))
                {
                    Debug.LogWarning($"[MergeTarget] Merge failed for {animal.Type} into {_facade.Type}. Animal not consumed.");
                    return false;
                }
            }

            // Fire visual effects
            Merge?.Invoke(visualAttributes);

            // Accumulate visual attributes on target for future merges
            _facade.AccumulatedVisualAttributes.AddRange(visualAttributes);

            animal.NotifyRemoved();
            animal.gameObject.SetActive(false);

            return true;
        }
    }
}
