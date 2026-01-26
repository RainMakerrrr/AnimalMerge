namespace Code.Animals.Movement
{
    /// <summary>
    /// Интерфейс для управления анимацией движения юнита
    /// </summary>
    public interface IMovementAnimator
    {
        /// <summary>
        /// Проиграть анимацию движения с указанной скоростью
        /// </summary>
        /// <param name="speed">Скорость анимации (0 = стоп, 1 = нормально)</param>
        void PlayMovementAnimation(float speed);

        /// <summary>
        /// Остановить анимацию движения
        /// </summary>
        void StopMovementAnimation();

        /// <summary>
        /// Проиграть анимацию прыжка (для уклонения)
        /// </summary>
        void PlayJumpAnimation();
    }
}
