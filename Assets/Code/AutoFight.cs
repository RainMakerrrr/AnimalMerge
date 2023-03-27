using System;
using System.Collections;
using System.Threading.Tasks;
using Code.Animals;
using UnityEngine;

namespace Code
{
    public class AutoFight : MonoBehaviour
    {
        [SerializeField] private AnimalSpawner _spawner;
        [SerializeField] private TargetFinder _targetFinder;
        [SerializeField] private TestEnemiesSpawner _enemiesSpawner;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                _targetFinder.Setup();
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                //Sequence sequence = DOTween.Sequence();

                Move();
                // foreach (Animal animal in _spawner.Animals)
                // {
                //     //sequence.Append(animal.GetComponent<AnimalMovement>().Move());
                // }
            }
        }

        private async void Move()
        {
            await MoveAllAnimals();
            await MoveAllEnemies();
        }

        private async Task MoveAllEnemies()
        {
            foreach (AnimalMovement enemy in _enemiesSpawner.AnimalInstances)
            {
                AnimalMovement closestAnimal = _targetFinder.FindClosestEnemy(enemy.transform.position, "Animal");
                
                if (enemy.CurrentTarget == null)
                    enemy.CurrentTarget = closestAnimal;

                if (enemy.CurrentTarget == null) return;

                if (enemy.IsCloseToTarget(enemy.CurrentTarget.CurrentPathNode.WorldPosition))
                {
                    enemy.RotateToTarget(closestAnimal.transform.position - enemy.transform.position);
                    await enemy.GetComponent<AnimalAttack>().Attack();
                }
                else
                {
                    await enemy.Move(enemy.CurrentTarget.CurrentPathNode.WorldPosition,
                        enemy.GetComponent<AnimalAttack>().Attack);
                }
            }
        }

        private async Task MoveAllAnimals()
        {
            foreach (Animal animal in _spawner.Animals)
            {
                AnimalMovement closestEnemy = _targetFinder.FindClosestEnemy(animal.transform.position, "Enemy");

                var animalMovement = animal.GetComponent<AnimalMovement>();

                if (animalMovement.CurrentTarget == null)
                    animalMovement.CurrentTarget = closestEnemy;

                if (animalMovement.CurrentTarget == null) return;

                if (animalMovement.IsCloseToTarget(animalMovement.CurrentTarget.CurrentPathNode.WorldPosition))
                {
                    animalMovement.RotateToTarget(closestEnemy.transform.position - animal.transform.position);
                    await animal.GetComponent<AnimalAttack>().Attack();
                }
                else
                {
                    await animalMovement.Move(animalMovement.CurrentTarget.CurrentPathNode.WorldPosition,
                        animal.GetComponent<AnimalAttack>().Attack);
                }
            }
        }
    }
}