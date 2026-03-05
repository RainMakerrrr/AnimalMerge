using Code.Animals.Facades;
using UnityEngine;

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "LevelStageConfig", menuName = "Game/Level Stage Config")]
    public class LevelStageConfig : ScriptableObject
    {
        [SerializeField] private StageEnemyConfig[] _enemies;
        [SerializeField] private bool _isBossStage;
        [SerializeField] private int _stageNumber;

        public StageEnemyConfig[] Enemies => _enemies;
        public bool IsBossStage => _isBossStage;
        public int StageNumber => _stageNumber;
    }

    [System.Serializable]
    public class StageEnemyConfig
    {
        [SerializeField] private AnimalFacade _prefab;
        [SerializeField] private Vector2Int _gridPosition;
        [SerializeField] private bool _isBoss;

        public AnimalFacade Prefab => _prefab;
        public Vector2Int GridPosition => _gridPosition;
        public bool IsBoss => _isBoss;
    }
}
