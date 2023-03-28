using Code.Animals.Movement;

namespace Code.Animals
{
    public interface IRaycastable
    {
        bool Accept(AnimalMovement animal);
    }
}