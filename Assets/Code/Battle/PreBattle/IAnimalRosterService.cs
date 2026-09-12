using System.Collections.Generic;
using Code.Animals;

namespace Code.Battle.PreBattle
{
    public interface IAnimalRosterService
    {
        int CurrentLevel { get; }

        IReadOnlyList<AnimalType> AvailableAnimals { get; }

        IReadOnlyList<AnimalType> NewlyUnlockedAnimals { get; }
    }
}
