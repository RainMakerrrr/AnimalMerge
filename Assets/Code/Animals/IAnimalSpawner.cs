using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Animals
{
    public interface IAnimalSpawner
    {
        IReadOnlyList<AnimalFacade> Animals { get; }
        IReadOnlyList<AnimalType> DefaultTypes { get; }

        bool HasFreeCellFor(AnimalType type);

        IReadOnlyList<AnimalFacade> Spawn(AnimalType type);

        IReadOnlyList<AnimalFacade> SpawnRandom();
    }
}
