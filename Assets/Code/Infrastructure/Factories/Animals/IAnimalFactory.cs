using Code.Animals;
using Code.Animals.Facades;

namespace Code.Infrastructure.Factories.Animals
{
    public interface IAnimalFactory
    {
        void Load();
        AnimalFacade Create(AnimalType type);
    }
}