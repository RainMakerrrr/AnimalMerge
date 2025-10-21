using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Health;
using Code.Animals.Movement;
using UnityEngine;

namespace Code
{
    public class AutoFight : MonoBehaviour
    {
        private const string AnimalLayerMask = "Animal";
        private const string EnemyLayerMask = "Enemy";

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
                Move();
            }
        }

        private async void Move()
        {
            await MoveUnits(_spawner.Animals, EnemyLayerMask);
            await MoveUnits(_enemiesSpawner.AnimalInstances, AnimalLayerMask);
        }
        

        private async Task MoveUnits(IEnumerable<AnimalFacade> animals, string layerMask)
        {
            foreach (AnimalFacade animal in animals)
            {
                if (animal.GetComponent<IDamageable>().IsDead) return;

                ITarget closestEnemy = _targetFinder.FindClosestTarget(animal.transform.position, layerMask);

                var animalMovement = animal.GetComponent<AnimalMovement>();

                animalMovement.CurrentTarget ??= closestEnemy;

                if (animalMovement.CurrentTarget == null || animalMovement.CurrentTarget.Damageable.IsDead)
                {
                    Debug.Log($"[PathFindDebug] Target is null for {animal.name}");
                    return;
                }

                Debug.Log($"[PathFindDebug] target is {animalMovement.CurrentTarget.Transformable.CurrentPathNode}");
                
                if (animalMovement.IsCloseToTarget(animalMovement.CurrentTarget.Transformable.CurrentPathNode
                        .WorldPosition))
                {
                    Debug.Log($"[PathFindDebug] is close to target, can attack {animal.name}");
                    animalMovement.RotateToTarget(closestEnemy.Transformable.Position - animal.transform.position);
                    await animal.AttackInstance.Attack();
                }
                else
                {
                    Debug.Log($"[PathFindDebug] move to target {animal.name}");

                    await animalMovement.Move(animalMovement.CurrentTarget.Transformable.CurrentPathNode.WorldPosition,
                        animal.AttackInstance.Attack);
                }
            }
        }
    }
}