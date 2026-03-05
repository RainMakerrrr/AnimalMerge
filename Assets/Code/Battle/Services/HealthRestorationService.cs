using System.Collections.Generic;
using Code.Animals.Facades;
using UnityEngine;

namespace Code.Battle.Services
{
    public class HealthRestorationService : IHealthRestorationService
    {
        public void RestoreHealthForSurvivingUnits(IEnumerable<AnimalFacade> units)
        {
            foreach (var unit in units)
            {
                if (unit == null || unit.Health.IsDead)
                    continue;

                var healthToRestore = unit.Health.Max - unit.Health.Current;
                if (healthToRestore > 0)
                {
                    unit.Health.Restore(healthToRestore);
                    Debug.Log($"[HealthRestorationService] Restored {unit.name} to full HP ({unit.Health.Max})");
                }
            }
        }
    }
}
