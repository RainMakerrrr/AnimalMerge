using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public interface ITurnOrderRecorder
    {
        void RecordPlayerTurns(IReadOnlyList<AnimalFacade> order);
        void RecordEnemyTurns(IReadOnlyList<AnimalFacade> order);
        void Clear();
    }
}
