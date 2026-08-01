using System.Linq;
using Code.Battle.Services;
using Code.Tutorial.Config;

namespace Code.Tutorial.Steps
{
    public class AnimalTypeTargetResolver : IMergeHintTargetResolver
    {
        private readonly IUnitTracker _unitTracker;
        private readonly MergeHintStepConfig _config;

        public AnimalTypeTargetResolver(IUnitTracker unitTracker, MergeHintStepConfig config)
        {
            _unitTracker = unitTracker;
            _config = config;
        }

        public MergeHintTargetMode Mode => MergeHintTargetMode.AnimalType;

        public bool TryResolve(out MergeHintPair pair)
        {
            pair = default;

            var units = _unitTracker.GetAlivePlayerUnits();

            var source = units.FirstOrDefault(unit => unit != null && unit.Type == _config.SourceType);
            if (source == null)
                return false;

            var target = units.FirstOrDefault(unit =>
                unit != null && unit != source && unit.Type == _config.TargetType);

            if (target == null)
                return false;

            pair = new MergeHintPair(source.transform, target.transform);
            return true;
        }
    }
}
