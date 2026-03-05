using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public interface IHealthRestorationService
    {
        void RestoreHealthForSurvivingUnits(IEnumerable<AnimalFacade> units);
    }
}
