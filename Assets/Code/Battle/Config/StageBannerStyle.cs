using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Battle.Config
{
    [Serializable]
    public class StageBannerStyle
    {
        [SerializeField] private StageBannerKind _kind;

        [Header("Title")]
        [SerializeField] private string _title;
        [SerializeField] private Material _titleMaterial;
        [SerializeField] private TMP_ColorGradient _titleGradient;
        [FormerlySerializedAs("_titleOffset")]
        [SerializeField] private Vector2 _contentOffset;

        [Header("Subtitle")]
        [SerializeField] private string _subtitle;
        [SerializeField] private Material _subtitleMaterial;
        [SerializeField] private TMP_ColorGradient _subtitleGradient;
        [SerializeField] private Vector2 _subtitleOffset;
        [SerializeField] private float _subtitleTiltZ = -8f;

        public StageBannerKind Kind => _kind;

        public string Title => _title;
        public Material TitleMaterial => _titleMaterial;
        public TMP_ColorGradient TitleGradient => _titleGradient;
        public Vector2 ContentOffset => _contentOffset;

        public string Subtitle => _subtitle;
        public Material SubtitleMaterial => _subtitleMaterial;
        public TMP_ColorGradient SubtitleGradient => _subtitleGradient;
        public Vector2 SubtitleOffset => _subtitleOffset;
        public float SubtitleTiltZ => _subtitleTiltZ;

        public bool HasSubtitle => !string.IsNullOrEmpty(_subtitle);
    }
}
