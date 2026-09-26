using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Merge;

namespace Code.Battle.Signals
{
    public class AllyMergedSignal
    {
        public PlayerAnimalFacade Source;
        public PlayerAnimalFacade Target;
        public AnimalType SourceType;
        public AnimalType TargetType;
        public MergeStatValues StatsBefore;
        public MergeStatValues StatsAfter;
    }
}
