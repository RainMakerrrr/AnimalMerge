using System;
using Code.Animals.Movement;
using UnityEngine;

namespace Code.Animals.Merge
{
    public class MergeTarget : MonoBehaviour, IRaycastable
    {
        public event Action<AnimalType> Merge; 

        public bool Accept(AnimalMovement animal)
        {
            Merge?.Invoke(animal.GetComponent<Animal>().Type);
                
            animal.gameObject.SetActive(false);
            
            return true;
        }
    }
}