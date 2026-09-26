using Code.Animals.Facades;

namespace Code.Animals.Merge
{
    public readonly struct MergeOutcome
    {
        public MergeOutcome(
            PlayerAnimalFacade source,
            PlayerAnimalFacade target,
            bool grantedSkill,
            MergeStatValues before,
            MergeStatValues after)
        {
            Source = source;
            Target = target;
            GrantedSkill = grantedSkill;
            Before = before;
            After = after;
        }

        public PlayerAnimalFacade Source { get; }
        public PlayerAnimalFacade Target { get; }
        public bool GrantedSkill { get; }
        public MergeStatValues Before { get; }
        public MergeStatValues After { get; }
    }
}
