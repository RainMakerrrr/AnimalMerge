using System.Linq;
using Code.Animals;
using UnityEngine;

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
                .Select(c => c.GetComponent<AnimalMovement>());

            Debug.Log(animals.Count());

            foreach (AnimalMovement animalMovement in animals)
            {
                Debug.LogError($"Distance between me and {animalMovement.name} = {Mathf.Abs(position.x - animalMovement.transform.position.x)}");
            }
            
            animals = animals.OrderBy(animal => Mathf.Abs(position.x - animal.transform.position.x));

            return animals.FirstOrDefault();
        }

        public ITarget FindClosestTarget(Vector3 position, string layerMask)
        {
            var targets = _colliders.Where(c => c.gameObject.layer == LayerMask.NameToLayer(layerMask))
                .Select(c => c.GetComponent<ITarget>());

            Debug.Log(targets.Count());

            targets = targets.OrderBy(target => Mathf.Abs(position.x - target.Transformable.Position.x));

            return targets.FirstOrDefault();
        }
    }
}