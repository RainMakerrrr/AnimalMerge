using UnityEngine;

namespace Code.Data.Animals
{
    [CreateAssetMenu(fileName = "Chicken Stats", menuName = "Stats/Chicken Stats")]
    public class ChickenStats : AnimalStats
    {
        [SerializeField] private float _jumpDuration = 0.5f;
        [SerializeField] private float _jumpPower = 2f;
        [SerializeField] private int _numJumps = 1;
        [SerializeField] private float _jumpDistance = 1.5f;

        public float JumpDuration => _jumpDuration;
        public float JumpPower => _jumpPower;
        public int NumJumps => _numJumps;
        public float JumpDistance => _jumpDistance;
    }
}
