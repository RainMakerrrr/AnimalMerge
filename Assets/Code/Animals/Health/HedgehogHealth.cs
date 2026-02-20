using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    /// <summary>
    /// Hedgehog health - uses CounterAttack ability (Priority=0, doesn't block damage)
    /// Now uses base AnimalHealth.ApplyAbilities() with AbilityManager
    /// No need to override - AbilityManager handles priority-based execution
    /// CounterAttack.IsBlockingDamage=false ensures damage is never blocked
    /// </summary>
    public class HedgehogHealth : AnimalHealth
    {
        // NOTE: ApplyAbilities() override removed - base implementation with AbilityManager
        // now correctly handles priority-based execution (CounterAttack has Priority=0)
        // CounterAttack.IsBlockingDamage=false ensures return value is correct
    }
}