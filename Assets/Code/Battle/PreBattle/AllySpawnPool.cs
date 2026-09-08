using System.Collections.Generic;
using Code.Animals;

namespace Code.Battle.PreBattle
{
    public class AllySpawnPool : IAllySpawnPool
    {
        private readonly Queue<AnimalType> _pending = new Queue<AnimalType>();

        private int _total;

        public int Remaining => _pending.Count;
        public int Total => _total;
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

        public void Enqueue(AnimalType type)
        {
            _pending.Enqueue(type);
            _total++;
        }

        public void Clear()
        {
            _pending.Clear();
            _total = 0;
        }
    }
}
