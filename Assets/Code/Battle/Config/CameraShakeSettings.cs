using System;
using UnityEngine;

namespace Code.Battle.Config
{
    [Serializable]
    public class CameraShakeSettings
    {
        [SerializeField, Min(0f)] private float _duration = 0.25f;
        [SerializeField] private Vector3 _strength = new Vector3(0.6f, 0.3f, 0.4f);
        [SerializeField, Min(0)] private int _vibrato = 12;
        [SerializeField, Range(0f, 180f)] private float _randomness = 90f;
        [SerializeField] private bool _fadeOut = true;

        public float Duration => _duration;
        public Vector3 Strength => _strength;
        public int Vibrato => _vibrato;
        public float Randomness => _randomness;
        public bool FadeOut => _fadeOut;
    }
}
