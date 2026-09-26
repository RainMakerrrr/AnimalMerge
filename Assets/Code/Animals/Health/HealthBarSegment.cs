using UnityEngine;
using UnityEngine.UI;

namespace Code.Animals.Health
{
    public class HealthBarSegment : MonoBehaviour
    {
        [SerializeField] private Image _fill;

        public RectTransform Rect => (RectTransform)transform;
        public Image Fill => _fill;
    }
}
