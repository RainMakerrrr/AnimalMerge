using Code.Animals;

namespace Code.Battle.PreBattle
{
    public interface IAllySpawnPool
    {
        int Remaining { get; }
        bool HasNext { get; }

        bool TryPeekNext(out AnimalType type);
        bool TryTakeNext(out AnimalType type);

        void Enqueue(AnimalType type);

        void RefillFromConfig();
        void Clear();
    }
}
