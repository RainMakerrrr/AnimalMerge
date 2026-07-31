using DG.Tweening;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public class DeathSpiritView : MonoBehaviour
    {
        private static readonly int Fade = Shader.PropertyToID("_Fade");
        private static readonly int SweepAxisExtent = Shader.PropertyToID("_SweepAxisExtent");

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private float _riseHeight = 2.5f;
        [SerializeField] private float _duration = 1.8f;
        [SerializeField] private float _endScale = 1.4f;
        [SerializeField] private float _swayAmplitude = 0.25f;
        [SerializeField] private float _yawDegrees = 40f;
        [SerializeField, Range(0f, 1f)] private float _fadeStart = 0.55f;

        private Mesh _mesh;
        private Material _material;
        private Sequence _sequence;

        public void Play(Mesh bakedMesh)
        {
            if (bakedMesh == null || _meshFilter == null || _renderer == null)
            {
                Destroy(gameObject);
                return;
            }

            _mesh = bakedMesh;
            _meshFilter.sharedMesh = _mesh;

            _material = _renderer.material;
            _material.SetFloat(SweepAxisExtent, Mathf.Max(_mesh.bounds.size.y, 0.0001f));
            _material.SetFloat(Fade, 1f);

            var position = transform.position;
            var fadeDelay = _duration * _fadeStart;

            _sequence = DOTween.Sequence()
                .Append(transform
                    .DOMoveY(position.y + _riseHeight, _duration)
                    .SetEase(Ease.InOutSine))
                .Join(transform
                    .DOScale(transform.localScale * _endScale, _duration)
                    .SetEase(Ease.OutQuad))
                .Join(transform
                    .DORotate(new Vector3(0f, _yawDegrees, 0f), _duration, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.InOutSine))
                .Join(transform
                    .DOMoveX(position.x + _swayAmplitude, _duration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(2, LoopType.Yoyo))
                .Insert(fadeDelay, DOVirtual
                    .Float(1f, 0f, _duration - fadeDelay, ApplyFade)
                    .SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(gameObject));
        }

        private void ApplyFade(float fade)
        {
            if (_material != null)
                _material.SetFloat(Fade, fade);
        }

        private void OnDestroy()
        {
            if (_sequence != null)
            {
                _sequence.Kill();
                _sequence = null;
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
                _mesh = null;
            }

            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }
    }
}
