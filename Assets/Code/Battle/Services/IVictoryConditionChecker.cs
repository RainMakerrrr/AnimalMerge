using Code.Battle;

namespace Code.Battle.Services
{
    public interface IVictoryConditionChecker
    {
        BattleResult CheckBattleConditions();
    }
}
