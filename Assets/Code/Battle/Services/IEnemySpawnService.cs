using System.Collections.Generic;
using System.Threading;
using Code.Animals.Facades;
using Code.Battle.Config;
using Cysharp.Threading.Tasks;

namespace Code.Battle.Services
{
    public interface IEnemySpawnService
    {
        UniTask<List<AnimalFacade>> SpawnEnemiesForStageAsync(
            LevelStageConfig stageConfig,
            CancellationToken cancellationToken);
        void ClearEnemies();
    }
}
