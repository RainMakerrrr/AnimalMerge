using System;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    public class FoxHealth : AnimalHealth
    {
        protected override async Task<bool> ApplyAbilities(AnimalAttack attacker)
        {
            bool isBlockedDamage = false;

            // Apply merged abilities first
            foreach (var ability in MergedAbilities.Where(a => a != null))
            {
                if (ability.CanUse(attacker))
                {
                    if (ability.IsBlockingDamage)
                        isBlockedDamage = true;
                    await ability.Apply();
                }
            }

            // Apply own ability
            if (Ability != null && Ability.CanUse(attacker))
            {
                if (Ability.IsBlockingDamage)
                    isBlockedDamage = true;
                await Ability.Apply();
            }

            return isBlockedDamage; // Returns true if ANY ability blocked damage
        }
    }
}