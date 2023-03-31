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

        public bool Accept(AnimalFacade animal)
        {
            List<AnimalType> types = animal.MergeSkills.Select(skill => skill.AnimalType).ToList();
            types.Add(animal.Type);

            Merge?.Invoke(types);
            
            animal.MergeSkill.Merge(_facade, animal);
            
            animal.gameObject.SetActive(false);
            
            return true;
        }
    }
}