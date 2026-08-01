using System;
using Code.Animals.Facades;

namespace Code.Animals.Selection
{
    public interface IAnimalSelectionService
    {
        AnimalFacade Selected { get; }

        event Action<AnimalFacade> SelectionChanged;

        void Select(AnimalFacade animal);

        void Clear();
    }
}
