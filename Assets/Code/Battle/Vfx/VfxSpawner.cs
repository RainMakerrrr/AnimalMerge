using System.Collections.Generic;
using UnityEngine;

namespace Code.Battle.Vfx
{
    public class VfxSpawner : IVfxSpawner
    {
        private const string PoolRootName = "VfxPool";

        private readonly Dictionary<ParticleSystem, Stack<PooledVfx>> _pools = new();
        private Transform _poolRoot;

        public void Spawn(ParticleSystem prefab, Vector3 position, Quaternion rotation, float uniformScale)
        {
            if (prefab == null)
                return;

            var instance = TakeFromPool(prefab);
            if (instance == null)
                instance = CreateInstance(prefab);

            instance.Play(position, rotation, uniformScale);
        }

        private PooledVfx TakeFromPool(ParticleSystem prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
                return null;

            while (pool.Count > 0)
            {
                var instance = pool.Pop();
                if (instance != null)
                    return instance;
            }

            return null;
        }

        private PooledVfx CreateInstance(ParticleSystem prefab)
        {
            var root = Object.Instantiate(prefab, GetPoolRoot());
            var instance = root.gameObject.AddComponent<PooledVfx>();
            instance.Setup(prefab, root, Release);
            return instance;
        }

        private void Release(PooledVfx instance)
        {
            if (!_pools.TryGetValue(instance.Prefab, out var pool))
            {
                pool = new Stack<PooledVfx>();
                _pools.Add(instance.Prefab, pool);
            }

            pool.Push(instance);
        }

        private Transform GetPoolRoot()
        {
            if (_poolRoot == null)
                _poolRoot = new GameObject(PoolRootName).transform;

            return _poolRoot;
        }
    }
}
