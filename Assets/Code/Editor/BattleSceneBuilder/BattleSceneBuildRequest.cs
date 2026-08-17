#if UNITY_EDITOR
using Code.Battle.Config;
using Code.GridPathfinding;

namespace Code.Editor.BattleSceneBuilder
{
    internal class BattleSceneBuildRequest
    {
        public LevelStageConfig StageConfig { get; set; }
        public PreBattleConfig PreBattleConfig { get; set; }
        public GridManager GameGrid { get; set; }
        public bool BuildAllies { get; set; }
        public bool BuildEnemies { get; set; }
    }
}
#endif
