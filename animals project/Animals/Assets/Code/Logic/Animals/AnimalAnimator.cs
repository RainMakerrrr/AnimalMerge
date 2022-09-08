using UnityEngine;

namespace Code.Logic.Animals
{
    public class AnimalAnimator : MonoBehaviour
    {
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");

        [SerializeField] private Animator _animator;

        public void SetIdle() => _animator.SetTrigger(Idle);

        public void UpdateMovementAnimation(float moveSpeed) => _animator.SetFloat(MoveSpeed, moveSpeed);
    }
}