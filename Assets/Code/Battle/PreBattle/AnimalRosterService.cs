using System.Collections.Generic;
using Code.Animals;
using Code.Battle.Config;
using Framework.Code.Infrastructure.Services.PersistentProgress;

namespace Code.Battle.PreBattle
{
    public class AnimalRosterService : IAnimalRosterService
    {
        private readonly PreBattleConfig _config;
        private readonly IPersistentProgressService _progressService;

        public AnimalRosterService(PreBattleConfig config, IPersistentProgressService progressService)
        {
            _config = config;
            _progressService = progressService;
        }

        public int CurrentLevel => _progressService.Progress.Level;

        public IReadOnlyList<AnimalType> AvailableAnimals => AnimalRosterResolver.Resolve(_config, CurrentLevel);

        public IReadOnlyList<AnimalType> NewlyUnlockedAnimals => AnimalRosterResolver.ResolveNewlyUnlocked(_config, CurrentLevel);
    }
}
