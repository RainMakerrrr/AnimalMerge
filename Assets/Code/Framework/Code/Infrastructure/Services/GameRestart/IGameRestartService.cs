namespace Framework.Code.Infrastructure.Services.GameRestart
{
    public interface IGameRestartService
    {
        void RetryCurrentLevel();
        void RestartCampaign();
    }
}
