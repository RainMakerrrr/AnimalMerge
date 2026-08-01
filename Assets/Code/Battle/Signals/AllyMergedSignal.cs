using Code.Animals;
using Code.Animals.Facades;

namespace Code.Battle.Signals
{
    public class AllyMergedSignal
    {
        public PlayerAnimalFacade Source;
        public PlayerAnimalFacade Target;
        public AnimalType SourceType;
        public AnimalType TargetType;
    }
}
