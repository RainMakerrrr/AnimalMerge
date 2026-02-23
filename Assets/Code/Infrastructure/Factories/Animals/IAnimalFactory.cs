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

        /// <summary>
        /// CHICKEN FEATURE: Spawns 3 additional chickens near the main chicken
        /// and registers them as neighbors for future "remove all" functionality.
        /// Returns list of created chickens (without the main chicken).
        /// </summary>
        List<ChickenFacade> SpawnAdditionalChickens(ChickenFacade mainChicken);

        void SetMergeGrid(IGridManager mergeGrid);
    }
}