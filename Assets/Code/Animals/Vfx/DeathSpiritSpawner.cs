using System;
using System.Threading;
using Code.Animals.Health;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Vfx
{
    public class DeathSpiritSpawner : MonoBehaviour
    {
        [SerializeField] private AnimalHealth _health;
        [SerializeField] private SkinnedMeshRenderer _sourceRenderer;
        [SerializeField] private DeathSpiritView _spiritPrefab;
        [SerializeField] private float _delay = 0.35f;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            if (_health == null)
            {
                Debug.LogError($"[DeathSpiritSpawner] {name}: Health is not assigned", this);
                return;
            }

            _cts = new CancellationTokenSource();
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;

            if (_cts == null)
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        [ContextMenu("OnDead")]
        private void OnDied()
        {
            _cts ??= new CancellationTokenSource();
            SpawnSpiritAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid SpawnSpiritAsync(CancellationToken cancellationToken)
        {
            if (_sourceRenderer == null || _spiritPrefab == null)
            {
                Debug.LogError($"[DeathSpiritSpawner] {name}: Source Renderer or Spirit Prefab is not assigned", this);
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_delay), cancellationToken: cancellationToken);

            var bakedMesh = new Mesh();
            _sourceRenderer.BakeMesh(bakedMesh, true);

            var origin = _sourceRenderer.transform;
            var spirit = Instantiate(_spiritPrefab, origin.position, origin.rotation);
            spirit.Play(bakedMesh);
        }
    }
}
