using Framework.Code.Data;

namespace Framework.Code.Infrastructure.Services.SaveSystem
{
    public interface ISaveLoadService
    {
        void Save(PlayerProgress progress);
        PlayerProgress LoadProgress();
        void Save(PlayerData data);
        PlayerData LoadData();
    }
}