using Code.Battle.Config;
using Framework.Code.Factories.Levels;
using UnityEngine;

namespace Code.Infrastructure
{
    /// <summary>
    /// Extended version of Level with support for battle stages
    /// </summary>
    public class ExtendedLevel : Level
    {
        [SerializeField] private LevelStageConfig[] _stages;

        public LevelStageConfig[] Stages => _stages;
    }
}
