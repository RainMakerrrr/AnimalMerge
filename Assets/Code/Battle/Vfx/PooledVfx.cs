using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Battle.Vfx
{
    public class PooledVfx : MonoBehaviour
    {
        private ParticleSystem _root;
        private Action<PooledVfx> _release;
        private bool _isPlaying;

        public ParticleSystem Prefab { get; private set; }

        private void OnParticleSystemStopped()
        {
            if (!_isPlaying)
                return;

            ReleaseWhenFinishedAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void Setup(ParticleSystem prefab, ParticleSystem root, Action<PooledVfx> release)
        {
            Prefab = prefab;
            _root = root;
            _release = release;

            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }

            var rootMain = root.main;
            rootMain.stopAction = ParticleSystemStopAction.Callback;
        }

        public void Play(Vector3 position, Quaternion rotation, float uniformScale)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * uniformScale;
            gameObject.SetActive(true);

            _root.Clear(true);
            _root.Play(true);
            _isPlaying = true;
        }

        private async UniTaskVoid ReleaseWhenFinishedAsync(CancellationToken cancellationToken)
        {
            bool isCancelled = await UniTask
                .WaitWhile(() => _root.IsAlive(true), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();

            if (isCancelled || !_isPlaying)
                return;

            _isPlaying = false;
            gameObject.SetActive(false);
            _release(this);
        }
    }
}
