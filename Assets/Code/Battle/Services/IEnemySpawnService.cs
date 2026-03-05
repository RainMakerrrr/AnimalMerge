using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Animals.Facades;
using Code.Battle.Config;

namespace Code.Battle.Services
{
    public interface IEnemySpawnService
    {
        Task<List<AnimalFacade>> SpawnEnemiesForStageAsync(
            LevelStageConfig stageConfig,
            CancellationToken cancellationToken);
        void ClearEnemies();
    }
}
