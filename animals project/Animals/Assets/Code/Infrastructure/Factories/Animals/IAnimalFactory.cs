using Code.Logic.Animals;

namespace Code.Infrastructure.Factories.Animals
{
    public interface IAnimalFactory
    {
        void Load();
        Animal Create(AnimalType type);
    }
}