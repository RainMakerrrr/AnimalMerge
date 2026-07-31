using System.Collections.Generic;
using Code.Animals;
using Code.Battle.Config;
using UnityEngine;

namespace Code.Battle.PreBattle
{
    public class AllySpawnPool : IAllySpawnPool
    {
        private readonly PreBattleConfig _config;
        private readonly Queue<AnimalType> _pending = new Queue<AnimalType>();

        public AllySpawnPool(PreBattleConfig config)
        {
            _config = config;
        }

        public int Remaining => _pending.Count;
        public bool HasNext => _pending.Count > 0;

        public bool TryPeekNext(out AnimalType type)
        {
            if (_pending.Count == 0)
            {
                type = default;
                return false;
            }

            type = _pending.Peek();
            return true;
        }

        public bool TryTakeNext(out AnimalType type)
        {
            if (_pending.Count == 0)
            {
                type = default;
                return false;
            }

            type = _pending.Dequeue();
            return true;
        }

        public void RefillFromConfig()
        {
            _pending.Clear();

            if (_config == null)
            {
                Debug.LogError("[AllySpawnPool] No PreBattleConfig assigned - starting pool is empty");
                return;
            }

            var startingPool = _config.StartingPool;
            if (startingPool == null || startingPool.Count == 0)
            {
                Debug.LogError("[AllySpawnPool] PreBattleConfig has an empty starting pool");
                return;
            }

            foreach (var type in startingPool)
            {
                _pending.Enqueue(type);
            }

            Debug.Log($"[AllySpawnPool] Refilled with {_pending.Count} animals");
        }

        public void Clear() => _pending.Clear();
    }
}
