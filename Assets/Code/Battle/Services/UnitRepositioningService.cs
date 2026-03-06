using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.GridPathfinding;
using UnityEngine;
using Zenject;

namespace Code.Battle.Services
{
    public class UnitRepositioningService : IUnitRepositioningService
    {
        private readonly IGridManager _mergeGrid;

        public UnitRepositioningService(
            [Inject(Id = GridIdentifier.MergeGrid)] IGridManager mergeGrid)
        {
            _mergeGrid = mergeGrid;
        }

        public void RepositionUnitsToMergeGrid(IEnumerable<AnimalFacade> units)
        {
            var unitsList = units.ToList();

            if (unitsList.Count == 0)
            {
                Debug.Log("[UnitRepositioningService] No units to reposition");
                return;
            }

            Debug.Log($"[UnitRepositioningService] Repositioning {unitsList.Count} units to merge grid");

            foreach (var unit in unitsList)
            {
                if (unit == null || unit.Movement == null)
                {
                    Debug.LogWarning("[UnitRepositioningService] Skipping null unit");
                    continue;
                }

                // Clear current grid occupancy (on GameGrid)
                unit.Movement.ClearNodes();

                // Reset rotation to default
                unit.transform.rotation = Quaternion.identity;

                // Place on merge grid (automatically finds free position in bottom 2 rows)
                var mergeGridManager = _mergeGrid as GridManager;
                if (mergeGridManager != null)
                {
                    mergeGridManager.PlaceOnGrid(unit.Movement);
                    Debug.Log($"[UnitRepositioningService] Repositioned {unit.name} to {unit.Movement.CurrentPathNode.GridPosition}");
                }
                else
                {
                    Debug.LogError("[UnitRepositioningService] Failed to cast IGridManager to GridManager");
                }
            }
        }
    }
}
