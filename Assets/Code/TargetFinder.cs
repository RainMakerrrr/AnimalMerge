using System.Linq;
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
            _colliders = Physics.OverlapSphere(transform.position, _radius, LayerMask.GetMask(AnimalLayerName, EnemyLayerName));
        }

        public AnimalMovement FindClosestEnemy(Vector3 position, string layerMask)
        {
            var animals = _colliders.Where(c => c.gameObject.layer == LayerMask.NameToLayer(layerMask))
                .Select(c => c.GetComponent<AnimalMovement>());

            Debug.Log(animals.Count());
            
            animals = animals.OrderBy(animal => Mathf.Abs(position.x - animal.transform.position.x));

            return animals.FirstOrDefault();
        }
    }
}