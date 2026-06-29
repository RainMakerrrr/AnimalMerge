using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Code.Animals.UI
{
    public class MergePopupView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private float _floatHeight = 150f;
        [SerializeField] private float _duration = 1.2f;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Play(string content, Color color)
        {
            _text.text = content;
            _text.color = color;

            DOTween.Sequence()
                .Append(_rectTransform
                    .DOAnchorPosY(_rectTransform.anchoredPosition.y + _floatHeight, _duration)
                    .SetEase(Ease.OutCubic))
                .Join(_text.DOFade(0f, _duration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
