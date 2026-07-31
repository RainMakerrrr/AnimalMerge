namespace Code.Battle.PreBattle
{
    public interface IBattleReadinessService
    {
        bool CanStartBattle { get; }

        void Evaluate();
        void Activate();
        void Deactivate();
    }
}
