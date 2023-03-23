using System;
using System.Collections;
using Code.Animals;
using UnityEngine;

namespace Code
{
    public class AutoFight : MonoBehaviour
    {
        [SerializeField] private AnimalSpawner _spawner;
        [SerializeField] private TargetFinder _targetFinder;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                _targetFinder.Setup();
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                //Sequence sequence = DOTween.Sequence();

                MoveAllAnimals();
                // foreach (Animal animal in _spawner.Animals)
                // {
                //     //sequence.Append(animal.GetComponent<AnimalMovement>().Move());
                // }
            }
        }

        private async void MoveAllAnimals()
        {
            foreach (Animal animal in _spawner.Animals)
            {
                AnimalMovement closestEnemy = _targetFinder.FindClosestEnemy(animal.transform.position, "Enemy");

                var animalMovement = animal.GetComponent<AnimalMovement>();

                if (animalMovement.CurrentTarget == null)
                    animalMovement.CurrentTarget = closestEnemy;

                if (animalMovement.CurrentTarget == null) return;
                
                if (animalMovement.IsCloseToTarget(animalMovement.CurrentTarget.transform.position))
                {
                    await animal.GetComponent<AnimalAttack>().Attack();
                }
                else
                {
                    await animalMovement.Move(animalMovement.CurrentTarget.transform.position,
                        animal.GetComponent<AnimalAttack>().Attack);
                }
            }
        }
    }
}