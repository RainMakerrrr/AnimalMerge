using UnityEngine;

namespace Code.Data.Animals
{
    [CreateAssetMenu(fileName = "Animal Stats", menuName = "Stats/Animal Stats")]
    public class AnimalStats : ScriptableObject
    {
        [SerializeField] private int _health;
        [SerializeField] private float _damage;
        [SerializeField] private int _tilesPerMove;

        public int Health => _health;
        public float Damage => _damage;
        public int TilesPerMove => _tilesPerMove;
    }
}