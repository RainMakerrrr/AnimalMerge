using System;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    /// <summary>
    /// Fox health - uses Dodge ability (Priority=1, blocks damage)
    /// Now uses base AnimalHealth.ApplyAbilities() with AbilityManager
    /// No need to override - AbilityManager handles priority-based execution
    /// </summary>
    public class FoxHealth : AnimalHealth
    {
        // NOTE: ApplyAbilities() override removed - base implementation with AbilityManager
        // now correctly handles priority-based execution (Dodge has Priority=1)
    }
}