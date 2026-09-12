using System.Collections.Generic;
using Code.Animals.Facades;

namespace Code.Animals.UI
{
    public interface IAbilityLinesProvider
    {
        IReadOnlyList<string> Build(AnimalFacade animal);
    }
}
