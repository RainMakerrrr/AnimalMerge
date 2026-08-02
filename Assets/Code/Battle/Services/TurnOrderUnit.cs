using Code.Animals.Facades;

namespace Code.Battle.Services
{
    public readonly struct TurnOrderUnit
    {
        public readonly AnimalFacade Unit;
        public readonly bool IsEnemy;

        public TurnOrderUnit(AnimalFacade unit, bool isEnemy)
        {
            Unit = unit;
            IsEnemy = isEnemy;
        }
    }
}
