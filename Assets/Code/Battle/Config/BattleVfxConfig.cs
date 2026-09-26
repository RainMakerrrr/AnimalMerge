using UnityEngine;

namespace Code.Battle.Config
{
    [CreateAssetMenu(fileName = "BattleVfxConfig", menuName = "Game/Battle Vfx Config")]
    public class BattleVfxConfig : ScriptableObject
    {
        [Header("Spawn Puff")]
        [SerializeField] private ParticleSystem _spawnPuffPrefab;
        [SerializeField, Min(0f)] private float _spawnPuffScalePerCell = 1f;
        [SerializeField] private float _spawnPuffHeightOffset;
        [SerializeField] private bool _playSpawnPuffForEnemies = true;

        [Header("AoE Wave")]
        [SerializeField] private ParticleSystem _aoeWavePrefab;
        [SerializeField, Min(0.01f)] private float _aoeWaveAuthoredRadius = 1f;
        [SerializeField, Min(0f)] private float _aoeWaveRadiusMultiplier = 1f;
        [SerializeField] private float _aoeWaveHeightOffset = 0.05f;

        public ParticleSystem SpawnPuffPrefab => _spawnPuffPrefab;
        public float SpawnPuffScalePerCell => _spawnPuffScalePerCell;
        public float SpawnPuffHeightOffset => _spawnPuffHeightOffset;
        public bool PlaySpawnPuffForEnemies => _playSpawnPuffForEnemies;

        public ParticleSystem AoeWavePrefab => _aoeWavePrefab;
        public float AoeWaveAuthoredRadius => _aoeWaveAuthoredRadius;
        public float AoeWaveRadiusMultiplier => _aoeWaveRadiusMultiplier;
        public float AoeWaveHeightOffset => _aoeWaveHeightOffset;
    }
}
