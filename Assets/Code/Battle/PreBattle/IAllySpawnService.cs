namespace Code.Battle.PreBattle
{
    public interface IAllySpawnService
    {
        bool CanSpawn { get; }
        int PoolRemaining { get; }

        bool RequestSpawn();

        int QueueStartingPool();

        int QueueReinforcements();
    }
}
