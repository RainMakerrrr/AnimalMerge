using Framework.Code.Data;
using Framework.Code.Infrastructure.Services.SaveSystem;

namespace Framework.Code.Infrastructure.Services.PersistentProgress
{
    public class PersistentProgressService : IPersistentProgressService
    {
        public PlayerProgress Progress { get; set; }
        public PlayerData Data { get; set; }

        public PersistentProgressService(ISaveLoadService saveLoadService)
        {
            Progress = saveLoadService.LoadProgress();
            Data = saveLoadService.LoadData();
        }
    }
}