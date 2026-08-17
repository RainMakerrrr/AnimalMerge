using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public interface IUnitRepositioningService
    {
        void RepositionUnitsToDeploymentZone(IEnumerable<AnimalFacade> units);
    }
}
