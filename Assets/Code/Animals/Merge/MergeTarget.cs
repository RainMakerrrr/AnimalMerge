using System;
using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using UnityEngine;

namespace Code.Animals.Merge
{
    public class MergeTarget : MonoBehaviour, IRaycastable
    {
        [SerializeField] private AnimalFacade _facade;
        public event Action<List<AnimalType>> Merge; 

        private void Awake()
        {
            if (_facade == null)
                _facade = GetComponent<AnimalFacade>();
        }

        public bool Accept(AnimalFacade animal)
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

            // Collect types for visual effects
            List<AnimalType> types = animal.MergeSkills.Select(skill => skill.AnimalType).ToList();
            types.Add(animal.Type);

            // Check: same type or different type?
            if (animal.Type == _facade.Type)
            {
                // Same-type merge: apply formula (HP1 + HP2) × 0.75
                float combinedHP = (animal.GetMaxHealth() + _facade.GetMaxHealth()) * 0.75f;
                float combinedDamage = (animal.GetDamage() + _facade.GetDamage()) * 0.75f;

                _facade.SetHealth(combinedHP);
                _facade.SetDamage(combinedDamage);

                Debug.Log($"Same-type merge: {animal.Type}. New HP: {combinedHP:F1}, New Damage: {combinedDamage:F1}");
            }
            else
            {
                // Different-type merge: apply skills as usual
                animal.MergeSkill.Merge(_facade, animal);
            }

            // Visual effects are invoked in any case
            Merge?.Invoke(types);

            // Deactivate merged animal
            animal.gameObject.SetActive(false);

            return true;
        }
    }
}