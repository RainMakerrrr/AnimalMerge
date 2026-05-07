using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Cysharp.Threading.Tasks;
using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.GridPathfinding;
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

        private IPathfindingService _pathfinder;

        [Inject]
        private void Construct(IPathfindingService pathfinder)
        {
            _pathfinder = pathfinder;
        }

        private void Update()
        {
            // if (Input.GetKeyDown(KeyCode.K))
            // {
            //     _targetFinder.Setup();
            // }
            //
            // if (Input.GetKeyDown(KeyCode.Z))
            // {
            //     Move();
            // }
        }

        private async UniTask Move()
        {
            Debug.Log("[PathFindDebug] === START PLAYER UNITS MOVE ===");
            await MoveUnits(_spawner.Animals, EnemyLayerMask);
            Debug.Log("[PathFindDebug] === START ENEMY UNITS MOVE ===");
            await MoveUnits(_enemiesSpawner.AnimalInstances, AnimalLayerMask);
            Debug.Log("[PathFindDebug] === ALL UNITS MOVED ===");
        }


        private GridCell GetClosestEnemyTileWorldPosition(AnimalMovement mover, ITarget enemy)
        {
            var enemyTransformable = enemy.Transformable;
            var rootCell = enemyTransformable.CurrentPathNode as GridCell;
            if (rootCell == null)
                return null;

            // If we don't know our current node, fallback to root
            if (mover.CurrentPathNode == null)
                return rootCell;

            // Build candidate list: enemy occupied root + its reserved neighbours
            List<GridCell> candidates = new List<GridCell> { rootCell };
            if (enemyTransformable is AnimalMovement enemyMovement && enemyMovement.Nodes != null &&
                enemyMovement.Nodes.Count > 0)
            {
                for (int i = 0; i < enemyMovement.Nodes.Count; i++)
                {
                    var cell = enemyMovement.Nodes[i];
                    if (cell != null) candidates.Add(cell);
                }
            }

            // Choose candidate by direct distance from mover to candidate node
            // Find the closest candidate node to our current position
            GridCell bestCell = null;
            float minDistance = float.MaxValue;
            Vector3 moverPos = mover.CurrentPathNode.WorldPosition;

            for (int i = 0; i < candidates.Count; i++)
            {
                var cell = candidates[i];
                if (cell == null) continue;

                // Calculate direct distance from mover to candidate
                float distance = (cell.WorldPosition - moverPos).sqrMagnitude;

                Debug.Log($"[PathFindDebug] Candidate cell {cell} distance: {distance},");

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestCell = cell;
                }
            }

            Debug.Log($"[PathFindDebug] Selected best cell: {bestCell} with distance: {minDistance}");

            // Fallback to root cell if no candidate path was found
            return bestCell ?? rootCell;
        }

        private async UniTask MoveUnits(IEnumerable<AnimalFacade> animals, string layerMask)
        {
            // Sort units by turn priority using grid positions: rows first, then columns
            // Reading order: complete each row from left to right before moving to next row
            var sortedAnimals = animals
                .Where(a => a != null && a.gameObject != null)
                .OrderBy(a => a.Movement?.CurrentPathNode?.GridPosition.x ?? 0)  // Top to bottom (rows) - higher Y first
                .ThenByDescending(a => a.Movement?.CurrentPathNode?.GridPosition.y ?? 0)            // Left to right (columns in each row)
                .ToList();

            Debug.Log($"[AutoFight] Turn order for {sortedAnimals.Count} units:");
            for (int i = 0; i < sortedAnimals.Count; i++)
            {
                var animal = sortedAnimals[i];
                var gridPos = animal.Movement?.CurrentPathNode?.GridPosition;
                Debug.Log($"  {i + 1}. {animal.name} at Grid({gridPos?.x ?? -1}, {gridPos?.y ?? -1}) World(X={animal.transform.position.x:F2}, Z={animal.transform.position.z:F2})");
            }

            foreach (AnimalFacade animal in sortedAnimals)
            {
                if (animal.Health.IsDead) continue;

                ITarget closestEnemy = _targetFinder.FindClosestTarget(animal.transform.position, layerMask);
                Debug.Log(
                    $"[PathFindDebug] {animal.name} searching for target on layer {layerMask}, found: {(closestEnemy != null ? closestEnemy.Transformable.Position.ToString() : "NULL")}");

                var animalMovement = animal.Movement;

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


                GridCell targetCell = GetClosestEnemyTileWorldPosition(animalMovement, animalMovement.CurrentTarget);

                Debug.Log($"[PathFindDebug] target is {targetCell}");

                if (targetCell != null && animalMovement.IsCloseToTarget(targetCell.WorldPosition))
                {
                    Debug.Log($"[PathFindDebug] is close to target, can attack {animal.name}");

                    // Update target right before attack to ensure we attack the closest enemy
                    animalMovement.CurrentTarget = _targetFinder.FindClosestTarget(animal.transform.position, layerMask);

                    if (animalMovement.CurrentTarget == null || animalMovement.CurrentTarget.Damageable.IsDead)
                    {
                        Debug.Log($"[PathFindDebug] {animal.name} target became invalid before attack, skipping");
                        continue;
                    }

                    animalMovement.RotateToTarget(animalMovement.CurrentTarget.Transformable.Position -
                                                  animal.transform.position);

                    // Pass the specific target to attack
                    await animal.AttackInstance.Attack(animalMovement.CurrentTarget);
                }
                else
                {
                    Debug.Log($"[PathFindDebug] move to target {animal.name}, target cell - {targetCell}");

                    // Update target before movement
                    animalMovement.CurrentTarget = _targetFinder.FindClosestTarget(animal.transform.position, layerMask);

                    if (animalMovement.CurrentTarget == null || animalMovement.CurrentTarget.Damageable.IsDead)
                    {
                        Debug.Log($"[PathFindDebug] {animal.name} target became invalid before move, skipping");
                        continue;
                    }

                    var isCloseToTarget = await animalMovement.Move(
                        (targetCell ?? animalMovement.CurrentTarget.Transformable.CurrentPathNode).WorldPosition);

                    if (isCloseToTarget)
                    {
                        await animal.AttackInstance.Attack(animalMovement.CurrentTarget);
                    }
                }
            }
        }
    }
}