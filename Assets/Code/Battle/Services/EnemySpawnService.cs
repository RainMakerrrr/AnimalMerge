using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Animals.Facades;
using Code.Data.Animals;
using Cysharp.Threading.Tasks;
using Code.Battle.Config;
using Code.GridPathfinding;
using Code.Pathfinding;
using UnityEngine;
using Zenject;

namespace Code.Battle.Services
{
    public class EnemySpawnService : IEnemySpawnService
    {
        private readonly DiContainer _container;
        private readonly IGridManager _gridManager;
        private readonly AnimalDatabase _database;
        private readonly List<AnimalFacade> _spawnedEnemies;
        private int _counter;

        public EnemySpawnService(
            DiContainer container,
            [Inject(Id = GridIdentifier.GameGrid)] IGridManager gridManager,
            AnimalDatabase database)
        {
            _container = container;
            _gridManager = gridManager;
            _database = database;
            _spawnedEnemies = new List<AnimalFacade>();
        }

        public async UniTask<List<AnimalFacade>> SpawnEnemiesForStageAsync(
            LevelStageConfig stageConfig,
            CancellationToken cancellationToken)
        {
            if (stageConfig == null || stageConfig.Enemies == null)
            {
                Debug.LogWarning("[EnemySpawnService] Invalid stage config");
                return new List<AnimalFacade>();
            }

            Debug.Log($"[EnemySpawnService] Spawning {stageConfig.Enemies.Length} enemies for stage {stageConfig.StageNumber}");

            var spawnedUnits = new List<AnimalFacade>();

            foreach (var enemyConfig in stageConfig.Enemies)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (enemyConfig?.Prefab == null)
                {
                    Debug.LogWarning("[EnemySpawnService] Null enemy config or prefab");
                    continue;
                }

                var spawnedEnemy = SpawnEnemyAtPosition(enemyConfig);
                if (spawnedEnemy != null)
                {
                    spawnedUnits.Add(spawnedEnemy);
                    _spawnedEnemies.Add(spawnedEnemy);
                }
            }

            Debug.Log($"[EnemySpawnService] Successfully spawned {spawnedUnits.Count} enemies");

            // Allow frame to complete spawning
            await UniTask.Yield();

            return spawnedUnits;
        }

        private AnimalFacade SpawnEnemyAtPosition(StageEnemyConfig enemyConfig)
        {
            var gridPosition = enemyConfig.GridPosition;
            var gridCell = _gridManager.GetCell(gridPosition.x, gridPosition.y) as GridCell;

            if (gridCell == null)
            {
                Debug.LogError($"[EnemySpawnService] Invalid grid cell at {gridPosition}");
                return null;
            }

            var enemy = _container.InstantiatePrefabForComponent<AnimalFacade>(enemyConfig.Prefab);

            if (enemy == null)
            {
                Debug.LogError($"[EnemySpawnService] Failed to instantiate enemy prefab");
                return null;
            }

            enemy.ApplyStats(_database.GetStats(enemy.Type));
            enemy.name += $"_{_counter}";

            var parentName = enemy.transform.parent != null ? enemy.transform.parent.name : "null";

            // Set boss flag if applicable
            if (enemyConfig.IsBoss)
            {
                enemy.IsBoss = true;
                Debug.Log($"[EnemySpawnService] Spawned boss: {enemy.name}");
            }

            // Set layer to Enemy
            enemy.gameObject.layer = LayerMask.NameToLayer("Enemy");

            // Get unit parameters BEFORE placing
            var unitSize = enemy.Movement.UnitSize;
            var direction = enemy.Movement.Direction;
            
            enemy.Movement.Place(gridCell.WorldPosition);

            enemy.Movement.SetCurrentNode(gridCell);

            var neighbours = _gridManager.GetNeighborCells(gridCell.GridPosition, unitSize, direction);

            if (neighbours.Count == 0)
            {
                gridCell.IsWalkable = false;
                gridCell.UpdateVisual();
            }
            else
            {
                foreach (var neighbour in neighbours)
                {
                    if (neighbour is GridCell cell)
                    {
                        cell.IsWalkable = false;
                        cell.UpdateVisual();
                    }
                }

                gridCell.IsWalkable = false;
                gridCell.UpdateVisual();

                enemy.Movement.FillNodes(neighbours.Cast<GridCell>().ToList());
            }

            Debug.Log($"[EnemySpawnService] Spawned {enemy.name} at grid {gridPosition}, FINAL position: {enemy.transform.position}");

            _counter++;
            
            return enemy;
        }

        public void ClearEnemies()
        {
            foreach (var enemy in _spawnedEnemies.Where(e => e != null))
            {
                // Clear grid occupancy
                if (enemy.Movement?.CurrentPathNode is GridCell gridCell)
                {
                    gridCell.IsWalkable = true;
                    gridCell.UpdateVisual();

                    // Clear neighbor cells
                    if (enemy.Movement.Nodes != null)
                    {
                        foreach (var node in enemy.Movement.Nodes.OfType<GridCell>())
                        {
                            node.IsWalkable = true;
                            node.UpdateVisual();
                        }
                    }
                }

                // Destroy GameObject
                Object.Destroy(enemy.gameObject);
            }

            _spawnedEnemies.Clear();
        }
    }
}
