using Code.Animals.Vfx.Config;
using UnityEngine;
using Zenject;

namespace Code.Animals.Merge.MergeAttributes
{
    public class VisualMergeAttribute : MonoBehaviour
    {
        [SerializeField] private AnimalType _type;

        private MergeAnimationConfig _animationConfig;

        public AnimalType Type => _type;

        protected Transform Target { get; private set; }
        protected MergeAnimationConfig AnimationConfig => _animationConfig;

        [Inject]
        private void ConstructVisual([InjectOptional] MergeAnimationConfig animationConfig) =>
            _animationConfig = animationConfig;

        public virtual void Apply(Transform target)
        {
            Target = target;
            Debug.Log($"[VisualMergeAttribute] Applying visual for {_type} to {target.name}");
        }

        public virtual void Undo()
        {
            Debug.Log($"[VisualMergeAttribute] Undoing visual for {_type}");
            Target = null;
        }
    }
}
