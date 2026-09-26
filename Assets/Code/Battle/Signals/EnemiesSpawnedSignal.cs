using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Battle.Signals
{
    public class EnemiesSpawnedSignal
    {
        public IReadOnlyList<AnimalFacade> Units;
    }
}
