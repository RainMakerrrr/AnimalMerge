using System.Collections.Generic;
using System.Linq;
using Code.Animals.Merge.MergeAttributes;
using UnityEngine;

namespace Code.Animals.Merge
{
    public class MergeView : MonoBehaviour
    {
        [SerializeField] private VisualMergeAttribute[] _attributes;
        [SerializeField] private MergeTarget _target;
        
        private Dictionary<AnimalType, VisualMergeAttribute> _cachedAttributes;

        private void Start()
        {
            _cachedAttributes = _attributes.ToDictionary(attribute => attribute.Type);
            _target.Merge += OnMerge;
        }

        private void OnDestroy()
        {
            _target.Merge -= OnMerge;
        }

        private void OnMerge(List<AnimalType> types)
        {
            foreach (AnimalType type in types)
            {
                if (_cachedAttributes.TryGetValue(type, out VisualMergeAttribute attribute))
                {
                    attribute.Apply();
                }
            }
        }
    }
}