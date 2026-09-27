using System.Collections.Generic;
using Code.Animals;
using DG.Tweening;
using UnityEngine;

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "UnitBattleFeedbackConfig", menuName = "Game/Unit Battle Feedback Config")]
    public class UnitBattleFeedbackConfig : ScriptableObject
    {
        [Header("Vignette")]
        [SerializeField, Min(0f)] private float _vignetteDelay;
        [SerializeField, Min(0f)] private float _vignetteFadeInDuration = 0.6f;
        [SerializeField] private Ease _vignetteFadeInEase = Ease.OutQuad;
        [SerializeField, Min(0f)] private float _vignetteFadeOutDuration = 0.4f;
        [SerializeField] private Ease _vignetteFadeOutEase = Ease.InQuad;
        [SerializeField, Range(0f, 1f)] private float _vignetteMaxAlpha = 1f;

        [Header("Units")]
        [SerializeField] private UnitBattleFeedbackEntry[] _entries = new UnitBattleFeedbackEntry[0];

        public float VignetteDelay => _vignetteDelay;
        public float VignetteFadeInDuration => _vignetteFadeInDuration;
        public Ease VignetteFadeInEase => _vignetteFadeInEase;
        public float VignetteFadeOutDuration => _vignetteFadeOutDuration;
        public Ease VignetteFadeOutEase => _vignetteFadeOutEase;
        public float VignetteMaxAlpha => _vignetteMaxAlpha;

        public bool TryGetFeedback(AnimalType type, out UnitBattleFeedbackEntry entry)
        {
            foreach (var candidate in _entries)
            {
                if (candidate != null && candidate.Type == type)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        private void OnValidate()
        {
            if (_entries == null)
                return;

            var seenTypes = new HashSet<AnimalType>();

            foreach (var entry in _entries)
            {
                if (entry == null)
                    continue;

                if (!seenTypes.Add(entry.Type))
                    Debug.LogWarning($"[{nameof(UnitBattleFeedbackConfig)}] Duplicate entry for {entry.Type} - only the first one is used.", this);
            }
        }
    }
}
