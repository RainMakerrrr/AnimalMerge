using System.Collections.Generic;
using Code.Animals.Merge.MergeAttributes;
using UnityEngine;

namespace Code.Animals.Merge
{
    public class MergeView : MonoBehaviour
    {
        [SerializeField] private MergeTarget _target;

        private List<VisualMergeAttribute> _appliedAttributes = new List<VisualMergeAttribute>();

        private void Start()
        {
            _target.Merge += OnMerge;
        }

        private void OnDestroy()
        {
            _target.Merge -= OnMerge;
        }

        private void OnMerge(List<VisualMergeAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                attribute.Apply(_target.transform.root);

                if (!_appliedAttributes.Contains(attribute))
                    _appliedAttributes.Add(attribute);
            }
        }

        public void UndoVisuals(List<VisualMergeAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                attribute.Undo();
                _appliedAttributes.Remove(attribute);
            }
        }
    }
}
