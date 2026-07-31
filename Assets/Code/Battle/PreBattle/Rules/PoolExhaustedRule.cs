namespace Code.Battle.PreBattle.Rules
{
    public class PoolExhaustedRule : IBattleReadinessRule
    {
        private readonly IAllySpawnPool _pool;

        public PoolExhaustedRule(IAllySpawnPool pool)
        {
            _pool = pool;
        }

        public bool IsSatisfied => _pool.Remaining == 0;
    }
}
