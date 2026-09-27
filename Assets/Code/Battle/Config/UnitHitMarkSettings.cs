using System;
using UnityEngine;

namespace Code.Battle.Config
{
    [Serializable]
    public class UnitHitMarkSettings
    {
        [SerializeField] private ParticleSystem _prefab;
        [SerializeField, Min(0f)] private float _scalePerCell = 1f;
        [SerializeField] private float _heightOffset = 1f;
        [SerializeField, Min(0f)] private float _towardCameraOffset = 0.5f;

        public ParticleSystem Prefab => _prefab;
        public float ScalePerCell => _scalePerCell;
        public float HeightOffset => _heightOffset;
        public float TowardCameraOffset => _towardCameraOffset;
    }
}
