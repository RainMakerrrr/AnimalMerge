namespace Framework.Code.Infrastructure.Services.Analytics
{
    public interface IAnalyticsService
    {
        void LevelLoaded(string levelId, int userLevel);
        void LevelStarted(string levelId, int userLevel);

        void LevelCompleted(string levelId, int userLevel, bool isFinished, int coinsCollected,
            float timeSpent);

        void LevelRestarted(string levelId, float timeSpent);
    }
}