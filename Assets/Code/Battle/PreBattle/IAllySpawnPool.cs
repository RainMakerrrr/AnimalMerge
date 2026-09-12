using System.Collections.Generic;
using Code.Animals;

namespace Code.Battle.PreBattle
{
    public interface IAllySpawnPool
    {
        int Remaining { get; }
        int Total { get; }
        bool HasNext { get; }
        IReadOnlyList<AnimalType> PendingAnimals { get; }

        bool TryPeekNext(out AnimalType type);
        bool TryTakeNext(out AnimalType type);
        bool TryTakeAt(int index, out AnimalType type);

        void Enqueue(AnimalType type);

        void Clear();
    }
}
