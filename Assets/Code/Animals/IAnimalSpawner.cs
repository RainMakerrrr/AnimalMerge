using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Animals
{
    public interface IAnimalSpawner
    {
        bool HasFreeCellFor(AnimalType type);

        IReadOnlyList<AnimalFacade> Spawn(AnimalType type);
    }
}
