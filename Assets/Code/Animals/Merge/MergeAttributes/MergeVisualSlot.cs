using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class MergeVisualSlot : MonoBehaviour
    {
        [SerializeField] private AnimalType _sourceType;
        public AnimalType SourceType => _sourceType;
    }
}
