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
        private List<AnimalType> _appliedTypes = new List<AnimalType>();

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
            foreach (var type in types)
            {
                if (_cachedAttributes.TryGetValue(type, out var attribute))
                {
                    attribute.Apply();

                    // Track applied type for undo
                    if (!_appliedTypes.Contains(type))
                    {
                        _appliedTypes.Add(type);
                    }
                }
            }
        }

        /// <summary>
        /// Undoes the visual effect for a specific animal type
        /// </summary>
        public void UndoVisual(AnimalType type)
        {
            Debug.Log($"[MergeView] UndoVisual called for type: {type}");

            if (_cachedAttributes.TryGetValue(type, out var attribute))
            {
                Debug.Log($"[MergeView] Found attribute for {type}, calling Undo()");
                attribute.Undo();

                // Remove from applied types
                _appliedTypes.Remove(type);

                Debug.Log($"[MergeView] Successfully undone visual for {type}");
            }
            else
            {
                Debug.LogWarning($"[MergeView] No visual attribute found for type {type}. Available types: {string.Join(", ", _cachedAttributes.Keys)}");
            }
        }

        /// <summary>
        /// Undoes all visual effects for the given types
        /// </summary>
        public void UndoVisuals(List<AnimalType> types)
        {
            foreach (var type in types)
            {
                UndoVisual(type);
            }
        }

        /// <summary>
        /// Clears the list of applied visual types
        /// </summary>
        public void ClearAppliedTypes()
        {
            _appliedTypes.Clear();
        }

        /// <summary>
        /// Gets the list of currently applied visual types
        /// </summary>
        public List<AnimalType> GetAppliedTypes()
        {
            return new List<AnimalType>(_appliedTypes);
        }
    }
}