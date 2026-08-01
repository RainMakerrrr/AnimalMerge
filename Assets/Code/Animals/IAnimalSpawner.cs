using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Animals
{
    public interface IAnimalSpawner
    {
        IReadOnlyList<AnimalFacade> Animals { get; }

        bool TryPickRandomType(out AnimalType type);

        bool HasFreeCellFor(AnimalType type);

        IReadOnlyList<AnimalFacade> Spawn(AnimalType type);
    }
}
