using System;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    public class FoxHealth : AnimalHealth
    {
        public override async Task TakeDamageAsync(AnimalAttack attacker)
        {
            LastAttack = attacker;

            bool isAbilityApply = await ApplyAbilities();

            if (isAbilityApply) return;

            await base.TakeDamageAsync(attacker);
        }
        
        private async Task<bool> ApplyAbilities()
        {
            bool isBlockedDamage = false;

            // Apply merged abilities first
            foreach (var ability in MergedAbilities.Where(a => a != null))
            {
                if (ability.CanUse)
                {
                    if (ability.IsBlockingDamage)
                        isBlockedDamage = true;
                    await ability.Apply();
                }
            }

            // Apply own ability
            if (Ability != null && Ability.CanUse)
            {
                if (Ability.IsBlockingDamage)
                    isBlockedDamage = true;
                await Ability.Apply();
            }

            return isBlockedDamage; // Returns true if ANY ability blocked damage
        }
    }
}