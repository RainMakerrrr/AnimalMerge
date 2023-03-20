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

                if (animal.GetComponent<AnimalMovement>().CanMove(closestEnemy.transform.position))
                {
                    animal.GetComponent<AnimalAttack>().Attack();
                }
                else
                {
                    await animal.GetComponent<AnimalMovement>().Move(closestEnemy.transform.position);
                    //yield return StartCoroutine(animal.GetComponent<AnimalMovement>().Move(closestEnemy.transform.position));
                }
            }
        }
    }
}