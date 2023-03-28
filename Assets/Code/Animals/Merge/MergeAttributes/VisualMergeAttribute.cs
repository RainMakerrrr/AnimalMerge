using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class VisualMergeAttribute : MonoBehaviour
    {
        [SerializeField] private AnimalType _type;
        public AnimalType Type => _type;

        public virtual void Apply() => gameObject.SetActive(true);
    }
}