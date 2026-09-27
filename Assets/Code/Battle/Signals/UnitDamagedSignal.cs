using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Health;

namespace Code.Battle.Signals
{
    public class UnitDamagedSignal
    {
        public AnimalAttack Attacker;
        public AnimalHealth Target;
        public AnimalFacade TargetUnit;
        public float Damage;
    }
}
