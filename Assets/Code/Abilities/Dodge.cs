using UnityEngine;

namespace Code.Abilities
{
    public class Dodge : IAbility
    {
        private readonly ITransformable _transformable;
        private readonly Collider[] _colliders;
        private readonly bool _isOwner;
        private int _counter;

        public bool IsBlockingDamage => true;
        public int Priority => 1;

        public bool CanUse
        {
            get
            {
                // First use: always 100%
                if (_counter == 0) return true;

                // Subsequent uses: 80% for owner, 50% for inherited
                int successThreshold = _isOwner ? 80 : 50;
                return Random.Range(0, 100) < successThreshold;
            }
        }

        public Dodge(ITransformable transformable, Collider[] colliders, bool isOwner)
        {
            _transformable = transformable;
            _colliders = colliders;
            _isOwner = isOwner;
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