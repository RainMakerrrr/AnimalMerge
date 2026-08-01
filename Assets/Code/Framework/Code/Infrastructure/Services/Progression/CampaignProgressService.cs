using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.PersistentProgress;

namespace Framework.Code.Infrastructure.Services.Progression
{
    public class CampaignProgressService : ICampaignProgressService
    {
        private readonly IPersistentProgressService _progressService;
        private readonly ILevelFactory _levelFactory;

        public CampaignProgressService(IPersistentProgressService progressService, ILevelFactory levelFactory)
        {
            _progressService = progressService;
            _levelFactory = levelFactory;
        }

        public int TotalLevels => _levelFactory.TotalLevelsCount;

        public bool IsCampaignCompleted => _progressService.Progress.Level > TotalLevels;

        public void ResetToFirstLevel()
        {
            _progressService.Progress.Level = 1;
            _progressService.Progress.Collectables.Clear();
        }
    }
}
