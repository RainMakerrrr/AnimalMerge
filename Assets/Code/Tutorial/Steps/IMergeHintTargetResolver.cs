namespace Code.Tutorial.Steps
{
    public interface IMergeHintTargetResolver
    {
        MergeHintTargetMode Mode { get; }

        bool TryResolve(out MergeHintPair pair);
    }
}
