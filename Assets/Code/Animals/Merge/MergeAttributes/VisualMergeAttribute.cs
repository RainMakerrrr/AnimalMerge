using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class VisualMergeAttribute : MonoBehaviour
    {
        [SerializeField] private AnimalType _type;
        public AnimalType Type => _type;

        protected Transform Target { get; private set; }

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
