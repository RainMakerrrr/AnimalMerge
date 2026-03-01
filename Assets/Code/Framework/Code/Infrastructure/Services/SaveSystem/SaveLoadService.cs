using Framework.Code.Data;
using Framework.Code.EditorExtensions;
using UnityEngine;

namespace Framework.Code.Infrastructure.Services.SaveSystem
{
    public class SaveLoadService : ISaveLoadService
    {
        const string PROGRESS_KEY = "Progress";
        private const string DATA_KEY = "Data";
		
        public void Save(PlayerProgress progress) => PlayerPrefs.SetString(PROGRESS_KEY, progress.ToJson());

        public void Save(PlayerData data) => PlayerPrefs.SetString(DATA_KEY, data.ToJson());
		
        public PlayerProgress LoadProgress()
        {
            var progress = PlayerPrefs.GetString(PROGRESS_KEY).FromJson<PlayerProgress>();

            return progress ?? CreateNewProgress();
        }

        public PlayerData LoadData()
        {
            var data = PlayerPrefs.GetString(DATA_KEY).FromJson<PlayerData>();

            return data ?? CreateNewData();
        }

        PlayerProgress CreateNewProgress() => new PlayerProgress();

        PlayerData CreateNewData() => new PlayerData();
    }
}