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
            if (animal == null)
                return false;

            if (_facade == null)
            {
                Debug.LogError("MergeTarget: target AnimalFacade is not assigned.");
                return false;
            }

            List<AnimalType> types = animal.MergeSkills.Select(skill => skill.AnimalType).ToList();
            types.Add(animal.Type);

            Merge?.Invoke(types);
            
            animal.MergeSkill.Merge(_facade, animal);
            
            animal.gameObject.SetActive(false);
            
            return true;
        }
    }
}