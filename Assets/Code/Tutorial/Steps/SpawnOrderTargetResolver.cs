using Code.Battle.Services;
using Code.Tutorial.Config;

namespace Code.Tutorial.Steps
{
    public class SpawnOrderTargetResolver : IMergeHintTargetResolver
    {
        private readonly IUnitTracker _unitTracker;
        private readonly MergeHintStepConfig _config;

        public SpawnOrderTargetResolver(IUnitTracker unitTracker, MergeHintStepConfig config)
        {
            _unitTracker = unitTracker;
            _config = config;
        }

        public MergeHintTargetMode Mode => MergeHintTargetMode.SpawnOrder;

        public bool TryResolve(out MergeHintPair pair)
        {
            pair = default;

            var sourceIndex = _config.SourceIndex;
            var targetIndex = _config.TargetIndex;

            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
                return false;

            var units = _unitTracker.GetAlivePlayerUnits();

            if (units.Count <= sourceIndex || units.Count <= targetIndex)
                return false;

            var source = units[sourceIndex];
            var target = units[targetIndex];

            if (source == null || target == null)
                return false;

            pair = new MergeHintPair(source.transform, target.transform);
            return true;
        }
    }
}
