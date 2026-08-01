using Code.Animals;
using Code.Tutorial.Steps;
using NaughtyAttributes;
using UnityEngine;

namespace Code.Tutorial.Config
{
    [CreateAssetMenu(fileName = "MergeHintStepConfig", menuName = "Game/Tutorial/Merge Hint Step Config")]
    public class MergeHintStepConfig : ScriptableObject
    {
        [SerializeField] private string _stepId = "MergeHint";
        [SerializeField] private int _levelNumber = 1;
        [SerializeField] private MergeHintTargetMode _targetMode = MergeHintTargetMode.SpawnOrder;

        [ShowIf(nameof(IsSpawnOrderMode))]
        [SerializeField] private int _sourceIndex;

        [ShowIf(nameof(IsSpawnOrderMode))]
        [SerializeField] private int _targetIndex = 1;

        [HideIf(nameof(IsSpawnOrderMode))]
        [SerializeField] private AnimalType _sourceType = AnimalType.Elephant;

        [HideIf(nameof(IsSpawnOrderMode))]
        [SerializeField] private AnimalType _targetType = AnimalType.Cheetah;

        [SerializeField] private bool _markCompletedWhenPhaseEnds;

        public string StepId => _stepId;
        public int LevelNumber => _levelNumber;
        public MergeHintTargetMode TargetMode => _targetMode;
        public int SourceIndex => _sourceIndex;
        public int TargetIndex => _targetIndex;
        public AnimalType SourceType => _sourceType;
        public AnimalType TargetType => _targetType;
        public bool MarkCompletedWhenPhaseEnds => _markCompletedWhenPhaseEnds;

        private bool IsSpawnOrderMode => _targetMode == MergeHintTargetMode.SpawnOrder;
    }
}
