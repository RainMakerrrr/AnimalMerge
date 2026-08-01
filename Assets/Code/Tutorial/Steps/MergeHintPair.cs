using UnityEngine;

namespace Code.Tutorial.Steps
{
    public readonly struct MergeHintPair
    {
        public readonly Transform Source;
        public readonly Transform Target;

        public MergeHintPair(Transform source, Transform target)
        {
            Source = source;
            Target = target;
        }

        public bool IsValid => Source != null && Target != null;
    }
}
