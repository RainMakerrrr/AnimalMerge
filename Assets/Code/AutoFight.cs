using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Pathfinding;
using UnityEngine;
using Zenject;

namespace Code
{
    public class AutoFight : MonoBehaviour
    {
        private const string AnimalLayerMask = "Animal";
        private const string EnemyLayerMask = "Enemy";

        [SerializeField] private AnimalSpawner _spawner;
        [SerializeField] private TargetFinder _targetFinder;
        [SerializeField] private TestEnemiesSpawner _enemiesSpawner;

        private IPathfinder _pathfinder;

        [Inject]
        private void Construct(IPathfinder pathfinder)
        {
            _pathfinder = pathfinder;
        }

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
            Debug.Log("[PathFindDebug] === START PLAYER UNITS MOVE ===");
            await MoveUnits(_spawner.Animals, EnemyLayerMask);
            Debug.Log("[PathFindDebug] === START ENEMY UNITS MOVE ===");
            await MoveUnits(_enemiesSpawner.AnimalInstances, AnimalLayerMask);
            Debug.Log("[PathFindDebug] === ALL UNITS MOVED ===");
        }


        private PathNode GetClosestEnemyTileWorldPosition(AnimalMovement mover, ITarget enemy)
        {
            var enemyTransformable = enemy.Transformable;
            var rootNode = enemyTransformable.CurrentPathNode;
            if (rootNode == null)
                return null;

            // If we don't know our current node, fallback to root
            if (mover.CurrentPathNode == null)
                return rootNode;

            // Build candidate list: enemy occupied root + its reserved neighbours
            List<PathNode> candidates = new List<PathNode> { rootNode };
            if (enemyTransformable is AnimalMovement enemyMovement && enemyMovement.Nodes != null &&
                enemyMovement.Nodes.Count > 0)
            {
                for (int i = 0; i < enemyMovement.Nodes.Count; i++)
                {
                    var node = enemyMovement.Nodes[i];
                    if (node != null) candidates.Add(node);
                }
            }

            // Choose candidate by direct distance from mover to candidate node
            // Find the closest candidate node to our current position
            PathNode bestNode = null;
            float minDistance = float.MaxValue;
            Vector3 moverPos = mover.CurrentPathNode.WorldPosition;

            for (int i = 0; i < candidates.Count; i++)
            {
                var node = candidates[i];
                if (node == null) continue;
                
                // Calculate direct distance from mover to candidate
                float distance = (node.WorldPosition - moverPos).sqrMagnitude;

                Debug.Log($"[PathFindDebug] Candidate node {node} distance: {distance},");

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestNode = node;
                }
            }

            Debug.Log($"[PathFindDebug] Selected best node: {bestNode} with distance: {minDistance}");

            // Fallback to root node if no candidate path was found
            return bestNode ?? rootNode;
        }

        private async Task MoveUnits(IEnumerable<AnimalFacade> animals, string layerMask)
        {
            foreach (AnimalFacade animal in animals)
            {
                if (animal.GetComponent<IDamageable>().IsDead) continue;

                ITarget closestEnemy = _targetFinder.FindClosestTarget(animal.transform.position, layerMask);
                Debug.Log(
                    $"[PathFindDebug] {animal.name} searching for target on layer {layerMask}, found: {(closestEnemy != null ? closestEnemy.Transformable.Position.ToString() : "NULL")}");

                var animalMovement = animal.GetComponent<AnimalMovement>();

                animalMovement.CurrentTarget ??= closestEnemy;

                // Retarget if no target or dead target
                if (animalMovement.CurrentTarget == null || animalMovement.CurrentTarget.Damageable.IsDead)
                {
                    Debug.Log(
                        $"[PathFindDebug] {animal.name} retargeting, current target is {(animalMovement.CurrentTarget == null ? "NULL" : "DEAD")}");
                    animalMovement.CurrentTarget =
                        _targetFinder.FindClosestTarget(animal.transform.position, layerMask);
                    if (animalMovement.CurrentTarget == null || animalMovement.CurrentTarget.Damageable.IsDead)
                    {
                        Debug.Log($"[PathFindDebug] {animal.name} could not find valid target, skipping");
                        continue;
                    }
                }


                PathNode targetNode = GetClosestEnemyTileWorldPosition(animalMovement, animalMovement.CurrentTarget);

                Debug.Log($"[PathFindDebug] target is {targetNode}");

                if (targetNode != null && animalMovement.IsCloseToTarget(targetNode.WorldPosition))
                {
                    Debug.Log($"[PathFindDebug] is close to target, can attack {animal.name}");
                    animalMovement.RotateToTarget(animalMovement.CurrentTarget.Transformable.Position -
                                                  animal.transform.position);
                    await animal.AttackInstance.Attack();
                }
                else
                {
                    Debug.Log($"[PathFindDebug] move to target {animal.name}, target node - {targetNode}");

                    await animalMovement.Move(
                        (targetNode ?? animalMovement.CurrentTarget.Transformable.CurrentPathNode).WorldPosition,
                        animal.AttackInstance.Attack);
                }
            }
        }
    }
}