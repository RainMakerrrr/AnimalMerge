using System;
using Code.Animals.Facades;

namespace Code.Battle.Selection
{
    public interface IEnemySelectionService
    {
        AnimalFacade Selected { get; }

        event Action<AnimalFacade> SelectionChanged;

        void Toggle(AnimalFacade enemy);

        void Clear();
    }
}
