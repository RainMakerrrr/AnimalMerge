using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Battle.Services
{
    public class UnitRepositioningService : IUnitRepositioningService
    {
        private readonly IGridManager _grid;

        public UnitRepositioningService(IGridManager grid)
        {
            _grid = grid;
        }

        public void RepositionUnitsToDeploymentZone(IEnumerable<AnimalFacade> units)
        {
            var unitsList = units.ToList();

            if (unitsList.Count == 0)
            {
                Debug.Log("[UnitRepositioningService] No units to reposition");
                return;
            }

            Debug.Log($"[UnitRepositioningService] Repositioning {unitsList.Count} units to the deployment zone");

            foreach (var unit in unitsList)
            {
                if (unit == null || unit.Movement == null)
                {
                    Debug.LogWarning("[UnitRepositioningService] Skipping null unit");
                    continue;
                }

                Debug.Log($"[UnitRepositioningService] Processing {unit.name} - Active: {unit.gameObject.activeInHierarchy}, Dead: {unit.Health.IsDead}");

                unit.Movement.ClearNodes();
                Debug.Log($"[UnitRepositioningService] Cleared nodes for {unit.name}");

                unit.transform.rotation = Quaternion.identity;

                var gridManager = _grid as GridManager;

                if (gridManager != null)
                {
                    gridManager.PlaceOnGrid(unit.Movement);
                    Debug.Log($"[UnitRepositioningService] Repositioned {unit.name} to {unit.Movement.CurrentPathNode.GridPosition} - Still active: {unit.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.LogError("[UnitRepositioningService] Failed to cast IGridManager to GridManager");
                }
            }

            Debug.Log($"[UnitRepositioningService] Repositioning complete. All units still exist and active.");
        }
    }
}
