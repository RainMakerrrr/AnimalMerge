using UnityEngine;

namespace Code.Abilities
{
    public class Dodge : IAbility
    {
        private readonly ITransformable _transformable;
        private readonly Collider[] _colliders;
        private readonly int _probability;
        private int _counter;

        public bool IsBlockingDamage => true;
        public int Priority => 1;

        public bool CanUse
        {
            get
            {
                if (_counter == 0) return true;
                return Random.Range(0, 101) > _probability;
            }
        }

        public Dodge(ITransformable transformable, Collider[] colliders, int probability)
        {
            _transformable = transformable;
            _colliders = colliders;
            _probability = probability;
        }

        public void Apply()
        {
            foreach (Collider collider in _colliders)
            {
                collider.enabled = false;
            }
            
            _counter++;
            _transformable.Shift();
        }
    }
}