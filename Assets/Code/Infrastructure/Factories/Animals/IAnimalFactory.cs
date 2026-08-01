using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Facades;
using Code.GridPathfinding;

namespace Code.Infrastructure.Factories.Animals
{
    public interface IAnimalFactory
    {
        void Load();
        AnimalFacade Create(AnimalType type);

        IReadOnlyList<ChickenFacade> CreateChickenFlock(IReadOnlyList<IGridCell> cells);
    }
}