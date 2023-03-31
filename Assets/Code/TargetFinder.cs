using System;
using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Health;
using Code.Animals.Movement;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code
{
    public class TargetFinder : MonoBehaviour
    {
        private const string AnimalLayerName = "Animal";
        private const string EnemyLayerName = "Enemy";

        [SerializeField] private float _radius;

        private Collider[] _colliders;

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, _radius);
        }

        public void Setup()
        {
            _colliders = Physics.OverlapSphere(transform.position, _radius,
                LayerMask.GetMask(AnimalLayerName, EnemyLayerName));
        }

        public AnimalMovement FindClosestEnemy(Vector3 position, string layerMask)
        {
            var animals = _colliders.Where(c =>
                    c != null && c.gameObject.activeSelf && c.gameObject.layer == LayerMask.NameToLayer(layerMask))
                .Select(c => c.GetComponentInParent<AnimalMovement>())
                .Where(a => a.GetComponent<IDamageable>().IsDead == false);

            animals = animals.OrderBy(animal => Mathf.Abs(position.x - animal.transform.position.x));

            return animals.FirstOrDefault();
        }

        public ITarget FindClosestTarget(Vector3 position, string layerMask)
        {
            IEnumerable<ITarget> targets = _colliders.Where(c =>
                    c != null && c.gameObject.activeSelf && c.gameObject.layer == LayerMask.NameToLayer(layerMask))
                .Select(c => c.GetComponentInParent<ITarget>()).Where(t => t.Damageable.IsDead == false);

            Debug.Log(targets.Count());

            targets = targets.OrderBy(target => Mathf.Abs(position.x - target.Transformable.Position.x));

            return targets.FirstOrDefault();
        }
    }
}