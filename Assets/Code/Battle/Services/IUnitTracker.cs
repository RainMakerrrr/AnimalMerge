using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public interface IUnitTracker
    {
        void RegisterPlayerUnit(AnimalFacade unit);
        void RegisterEnemyUnit(AnimalFacade unit);
        void Reset();

        int AlivePlayerUnitsCount { get; }
        int AliveEnemyUnitsCount { get; }
        bool HasAliveBoss { get; }
        bool WasBossRegistered { get; }

        IReadOnlyList<AnimalFacade> GetAlivePlayerUnits();
        IReadOnlyList<AnimalFacade> GetAliveEnemyUnits();
    }
}
