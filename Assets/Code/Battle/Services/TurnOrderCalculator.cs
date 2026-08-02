using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public static class TurnOrderCalculator
    {
        public static List<AnimalFacade> OrderPlayerUnits(IReadOnlyList<AnimalFacade> units) =>
            units
                .Where(u => u != null && u.gameObject != null)
                .OrderBy(u => u.Movement?.CurrentPathNode?.GridPosition.x ?? 0)
                .ThenByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.y ?? 0)
                .ToList();

        public static List<AnimalFacade> OrderEnemyUnits(IReadOnlyList<AnimalFacade> units) =>
            units
                .Where(u => u != null && u.gameObject != null)
                .OrderByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.x ?? 0)
                .ThenByDescending(u => u.Movement?.CurrentPathNode?.GridPosition.y ?? 0)
                .ToList();
    }
}
