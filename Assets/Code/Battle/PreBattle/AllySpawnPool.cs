using System.Collections.Generic;
using Code.Animals;

namespace Code.Battle.PreBattle
{
    public class AllySpawnPool : IAllySpawnPool
    {
        private readonly List<AnimalType> _pending = new List<AnimalType>();

        private int _total;

        public int Remaining => _pending.Count;
        public int Total => _total;
        public bool HasNext => _pending.Count > 0;
        public IReadOnlyList<AnimalType> PendingAnimals => _pending;

        public bool TryPeekNext(out AnimalType type)
        {
            if (_pending.Count == 0)
            {
                type = default;
                return false;
            }

            type = _pending[0];
            return true;
        }

        public bool TryTakeNext(out AnimalType type) => TryTakeAt(0, out type);

        public bool TryTakeAt(int index, out AnimalType type)
        {
            if (index < 0 || index >= _pending.Count)
            {
                type = default;
                return false;
            }

            type = _pending[index];
            _pending.RemoveAt(index);
            return true;
        }

        public void Enqueue(AnimalType type)
        {
            _pending.Add(type);
            _total++;
        }

        public void Clear()
        {
            _pending.Clear();
            _total = 0;
        }
    }
}
