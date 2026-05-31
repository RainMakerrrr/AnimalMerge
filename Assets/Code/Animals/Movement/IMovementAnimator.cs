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

        /// <summary>
        /// Обновить направление поворота для плавного Blend Tree
        /// </summary>
        /// <param name="direction">Направление поворота (-1 = влево, 0 = прямо, 1 = вправо)</param>
        void UpdateTurnDirection(float direction);

        /// <summary>
        /// Установить скорость воспроизведения аниматора (1 = нормально, 2 = вдвое быстрее)
        /// </summary>
        void SetPlaybackSpeed(float speed);
    }
}
