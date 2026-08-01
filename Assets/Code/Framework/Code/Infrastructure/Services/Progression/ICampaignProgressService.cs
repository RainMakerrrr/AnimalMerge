namespace Framework.Code.Infrastructure.Services.Progression
{
    public interface ICampaignProgressService
    {
        int TotalLevels { get; }
        bool IsCampaignCompleted { get; }
        void ResetToFirstLevel();
    }
}
