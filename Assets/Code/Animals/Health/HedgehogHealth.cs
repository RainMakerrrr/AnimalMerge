using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    public class HedgehogHealth : AnimalHealth
    {
        
        protected override async Task<bool> ApplyAbilities()
        {
            // Apply own ability first (owner, 100% chance)
            if (Ability != null && Ability.CanUse)
            {
                await Ability.Apply();
            }

            // Then apply merged abilities
            foreach (var ability in MergedAbilities.Where(a => a != null))
            {
                if (ability.CanUse)
                {
                    await ability.Apply();
                }
            }

            // CounterAttack never blocks damage
            return false;
        }
    }
}