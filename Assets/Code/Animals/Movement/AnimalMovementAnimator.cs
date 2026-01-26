using Code.Animals;

namespace Code.Animals.Movement
{
    /// <summary>
    /// Адаптер для AnimalAnimator, реализующий IMovementAnimator
    /// Изолирует прямую зависимость от AnimalAnimator и упрощает тестирование
    /// </summary>
    public class AnimalMovementAnimator : IMovementAnimator
    {
        private readonly AnimalAnimator _animator;

        public AnimalMovementAnimator(AnimalAnimator animator)
        {
            _animator = animator;
        }

        public void PlayMovementAnimation(float speed)
        {
            _animator.UpdateMovementAnimation(speed);
        }

        public void StopMovementAnimation()
        {
            _animator.UpdateMovementAnimation(0f);
        }

        public void PlayJumpAnimation()
        {
            _animator.JumpAnimation();
        }
    }
}
