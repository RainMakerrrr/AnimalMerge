using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "StageBannerConfig", menuName = "Game/Stage Banner Config")]
    public class StageBannerConfig : ScriptableObject
    {
        [Header("Band")]
        [SerializeField, Min(0f)] private float _bandExpandDuration = 0.15f;
        [SerializeField] private Ease _bandExpandEase = Ease.OutQuad;

        [Header("Title Slam")]
        [SerializeField, Min(0f)] private float _titleDelay = 0.15f;
        [SerializeField, Min(0f)] private float _titleStartScale = 2.5f;
        [SerializeField, Min(0f)] private float _titleFadeInDuration = 0.08f;
        [SerializeField, Min(0f)] private float _slamDuration = 0.2f;
        [SerializeField] private Vector2 _squashScale = new Vector2(1.1f, 0.9f);
        [SerializeField] private Ease _slamEase = Ease.InCubic;
        [SerializeField, Min(0f)] private float _reboundDuration = 0.1f;
        [SerializeField] private Vector2 _stretchScale = new Vector2(0.95f, 1.08f);
        [SerializeField] private Ease _reboundEase = Ease.OutQuad;
        [SerializeField, Min(0f)] private float _settleDuration = 0.1f;
        [SerializeField] private Ease _settleEase = Ease.OutQuad;

        [Header("Subtitle Pop")]
        [SerializeField, Min(0f)] private float _subtitleDelay = 0.6f;
        [SerializeField, Min(0f)] private float _subtitlePopDuration = 0.25f;
        [SerializeField] private Ease _subtitlePopEase = Ease.OutBack;
        [SerializeField] private float _subtitleWobbleAngle = 12f;
        [SerializeField, Min(0f)] private float _subtitleWobbleDuration = 0.4f;
        [SerializeField, Min(0)] private int _subtitleWobbleVibrato = 6;
        [SerializeField, Range(0f, 1f)] private float _subtitleWobbleElasticity = 0.6f;

        [Header("Hold")]
        [SerializeField, Min(0f)] private float _holdDuration = 0.8f;

        [Header("Exit")]
        [SerializeField, Min(0f)] private float _exitDuration = 0.3f;
        [SerializeField, Min(0f)] private float _exitSlideDistance = 1300f;
        [SerializeField] private StageBannerExitSide _exitSide = StageBannerExitSide.Left;
        [SerializeField] private Ease _exitEase = Ease.InBack;
        [SerializeField, Min(0f)] private float _bandFadeOutDuration = 0.3f;

        [Header("Styles")]
        [SerializeField] private StageBannerStyle[] _styles = new StageBannerStyle[0];

        public float BandExpandDuration => _bandExpandDuration;
        public Ease BandExpandEase => _bandExpandEase;

        public float TitleDelay => _titleDelay;
        public float TitleStartScale => _titleStartScale;
        public float TitleFadeInDuration => _titleFadeInDuration;
        public float SlamDuration => _slamDuration;
        public Vector2 SquashScale => _squashScale;
        public Ease SlamEase => _slamEase;
        public float ReboundDuration => _reboundDuration;
        public Vector2 StretchScale => _stretchScale;
        public Ease ReboundEase => _reboundEase;
        public float SettleDuration => _settleDuration;
        public Ease SettleEase => _settleEase;

        public float SubtitleDelay => _subtitleDelay;
        public float SubtitlePopDuration => _subtitlePopDuration;
        public Ease SubtitlePopEase => _subtitlePopEase;
        public float SubtitleWobbleAngle => _subtitleWobbleAngle;
        public float SubtitleWobbleDuration => _subtitleWobbleDuration;
        public int SubtitleWobbleVibrato => _subtitleWobbleVibrato;
        public float SubtitleWobbleElasticity => _subtitleWobbleElasticity;

        public float HoldDuration => _holdDuration;

        public float ExitDuration => _exitDuration;
        public float ExitSlideDistance => _exitSlideDistance;
        public float ExitDirection => _exitSide == StageBannerExitSide.Left ? -1f : 1f;
        public Ease ExitEase => _exitEase;
        public float BandFadeOutDuration => _bandFadeOutDuration;

        public bool TryGetStyle(StageBannerKind kind, out StageBannerStyle style)
        {
            foreach (var candidate in _styles)
            {
                if (candidate != null && candidate.Kind == kind)
                {
                    style = candidate;
                    return true;
                }
            }

            style = null;
            return false;
        }

        private void OnValidate()
        {
            if (_styles == null)
                return;

            var seenKinds = new HashSet<StageBannerKind>();

            foreach (var style in _styles)
            {
                if (style == null)
                    continue;

                if (!seenKinds.Add(style.Kind))
                    Debug.LogWarning($"[{nameof(StageBannerConfig)}] Duplicate style for {style.Kind} - only the first one is used.", this);

                if (style.TitleMaterial == null)
                    Debug.LogWarning($"[{nameof(StageBannerConfig)}] Style {style.Kind} has no title material - the font default material is used.", this);

                if (style.HasSubtitle && style.SubtitleMaterial == null)
                    Debug.LogWarning($"[{nameof(StageBannerConfig)}] Style {style.Kind} has a subtitle but no subtitle material - the font default material is used.", this);
            }

            foreach (StageBannerKind kind in Enum.GetValues(typeof(StageBannerKind)))
            {
                if (!seenKinds.Contains(kind))
                    Debug.LogWarning($"[{nameof(StageBannerConfig)}] No style configured for {kind} - the banner is skipped.", this);
            }
        }
    }
}
