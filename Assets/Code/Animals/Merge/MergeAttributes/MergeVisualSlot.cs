using UnityEngine;

namespace Code.Animals.Merge.MergeAttributes
{
    public class MergeVisualSlot : MonoBehaviour
    {
        [SerializeField] private AnimalType _sourceType;

        private Vector3 _authoredScale;
        private bool _hasAuthoredScale;

        public AnimalType SourceType => _sourceType;

        public Vector3 AuthoredScale
        {
            get
            {
                CaptureAuthoredScale();
                return _authoredScale;
            }
        }

        private void Awake() => CaptureAuthoredScale();

        private void CaptureAuthoredScale()
        {
            if (_hasAuthoredScale)
                return;

            _authoredScale = transform.localScale;
            _hasAuthoredScale = true;
        }
    }
}
