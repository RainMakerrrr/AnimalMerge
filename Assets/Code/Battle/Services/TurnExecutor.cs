using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Cysharp.Threading.Tasks;
using Code.Animals.Facades;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Battle.Services
{
    public class TurnExecutor : ITurnExecutor
    {
        private const string AnimalLayerMask = "Animal";
        private const string EnemyLayerMask = "Enemy";

        private readonly IUnitTracker _unitTracker;
        private readonly TargetFinder _targetFinder;

        public TurnExecutor(
            IUnitTracker unitTracker,
            TargetFinder targetFinder)
        {
            _unitTracker = unitTracker;
            _targetFinder = targetFinder;
        }

        public async UniTask ExecutePlayerTurnsAsync()
        {
            Debug.Log("[BattleSystem] === START PLAYER UNITS TURN ===");

            // CRITICAL: Initialize target finder with current colliders
            _targetFinder.Setup();

            var playerUnits = _unitTracker.GetAlivePlayerUnits();

            // Sort by grid position: left-to-right, top-to-bottom
            var sortedUnits = playerUnits
                .Where(u => u != null && u.gameObject != null)
                .OrderBy(u => u.Movement?.CurrentPathNode?.GridPosition.x ?? 0)
                .ThenByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.y ?? 0)
                .ToList();

            await ExecuteUnitTurnsAsync(sortedUnits, EnemyLayerMask);
            Debug.Log("[BattleSystem] === PLAYER UNITS TURN COMPLETE ===");
        }

        public async UniTask ExecuteEnemyTurnsAsync()
        {
            Debug.Log("[BattleSystem] === START ENEMY UNITS TURN ===");

            // CRITICAL: Initialize target finder with current colliders
            _targetFinder.Setup();

            var enemyUnits = _unitTracker.GetAliveEnemyUnits();

            // Sort by grid position: bottom-to-top (reversed Y order for enemies)
            var sortedUnits = enemyUnits
                .Where(u => u != null && u.gameObject != null)
                .OrderByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.x ?? 0)
                .ThenByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.y ?? 0)
                .ToList();

            await ExecuteUnitTurnsAsync(sortedUnits, AnimalLayerMask);
            Debug.Log("[BattleSystem] === ENEMY UNITS TURN COMPLETE ===");
        }

        private async UniTask ExecuteUnitTurnsAsync(
            List<AnimalFacade> units,
            string targetLayerMask)
        {
            Debug.Log($"[BattleSystem] Turn order for {units.Count} units:");
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                var gridPos = unit.Movement?.CurrentPathNode?.GridPosition;
                Debug.Log($"  {i + 1}. {unit.name} at Grid({gridPos?.x ?? -1}, {gridPos?.y ?? -1})");
            }

            foreach (var unit in units)
            {
                if (unit == null || unit.Health.IsDead)
                    continue;

                await ExecuteSingleUnitTurnAsync(unit, targetLayerMask);
            }
        }

        private async UniTask ExecuteSingleUnitTurnAsync(
            AnimalFacade unit,
            string targetLayerMask)
        {
            var movement = unit.Movement;
            if (movement == null)
            {
                Debug.LogWarning($"[BattleSystem] {unit.name} has no AnimalMovement component");
                return;
            }

            // Find or retarget
            ITarget target = FindValidTarget(unit, movement, targetLayerMask);
            if (target == null)
            {
                Debug.Log($"[BattleSystem] {unit.name} could not find valid target, skipping turn");
                return;
            }

            movement.CurrentTarget = target;

            // Get closest target cell
            var targetCell = GetClosestEnemyCell(movement, target);
            if (targetCell == null)
            {
                Debug.LogWarning($"[BattleSystem] {unit.name} could not determine target cell");
                return;
            }

            Debug.Log($"[PathfindingDebug][ExecuteSingleUnitTurnAsync] Unit at {movement.CurrentPathNode?.GridPosition}, Target cell from GetClosestEnemyCell: {targetCell.GridPosition}, Enemy at {target.Transformable.CurrentPathNode?.GridPosition}");

            // Check if close enough to attack
            if (movement.IsCloseToTarget(targetCell.WorldPosition))
            {
                Debug.Log($"[BattleSystem] {unit.name} is close to target, attacking directly");

                // Refresh target before attack
                target = FindValidTarget(unit, movement, targetLayerMask);
                if (target == null || target.Damageable.IsDead)
                {
                    Debug.Log($"[BattleSystem] {unit.name} target became invalid before attack");
                    return;
                }

                movement.CurrentTarget = target;
                movement.RotateToTarget(target.Transformable.Position - unit.transform.position);
                await unit.AttackInstance.Attack(target);
            }
            else
            {
                Debug.Log($"[BattleSystem] {unit.name} moving to target at {targetCell}");

                // Refresh target before movement
                target = FindValidTarget(unit, movement, targetLayerMask);
                if (target == null || target.Damageable.IsDead)
                {
                    Debug.Log($"[BattleSystem] {unit.name} target became invalid before move");
                    return;
                }

                movement.CurrentTarget = target;
                var isCloseToTarget = await movement.Move(targetCell.WorldPosition);

                if (isCloseToTarget)
                {
                    await unit.AttackInstance.Attack(target);
                }
            }
        }

        private ITarget FindValidTarget(AnimalFacade unit, AnimalMovement movement, string targetLayerMask)
        {
            // Use existing target if valid
            // if (movement.CurrentTarget != null && !movement.CurrentTarget.Damageable.IsDead)
            // {
            //     return movement.CurrentTarget;
            // }

            // Find new target
            var newTarget = _targetFinder.FindClosestTarget(unit.transform.position, targetLayerMask);

            if (newTarget != null && !newTarget.Damageable.IsDead)
            {
                return newTarget;
            }

            return null;
        }

        private GridCell GetClosestEnemyCell(AnimalMovement mover, ITarget enemy)
        {
            var enemyTransformable = enemy.Transformable;
            var rootCell = enemyTransformable.CurrentPathNode as GridCell;
            if (rootCell == null)
                return null;

            if (mover.CurrentPathNode == null)
                return rootCell;

            // Build candidate list: enemy root + reserved neighbours
            var candidates = new List<GridCell> { rootCell };
            if (enemyTransformable is AnimalMovement enemyMovement &&
                enemyMovement.Nodes != null &&
                enemyMovement.Nodes.Count > 0)
            {
                candidates.AddRange(enemyMovement.Nodes.Where(cell => cell != null));
            }

            Debug.Log($"[PathfindingDebug] Start finding best cell for {mover.name}");
            Debug.Log($"[PathfindingDebug][GetClosestEnemyCell] Enemy root: {rootCell.GridPosition}, candidates: {string.Join(", ", candidates.Select(c => c.GridPosition))}");

            // Find closest candidate by distance
            GridCell bestCell = null;
            var minDistance = float.MaxValue;
            var moverPos = mover.CurrentPathNode.WorldPosition;

            foreach (var cell in candidates)
            {
                if (cell == null)
                    continue;

                var distance = (cell.WorldPosition - moverPos).sqrMagnitude;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestCell = cell;
                }
            }

            Debug.Log($"[PathfindingDebug][GetClosestEnemyCell] Best cell: {bestCell?.GridPosition}");

            return bestCell ?? rootCell;
        }
    }
}
