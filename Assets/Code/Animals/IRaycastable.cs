using Code.Animals.Facades;
using Code.Animals.Movement;

namespace Code.Animals
{
    public interface IRaycastable
    {
        bool Accept(PlayerAnimalFacade animal);
    }
}