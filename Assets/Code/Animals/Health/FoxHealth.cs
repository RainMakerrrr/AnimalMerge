using System;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    public class FoxHealth : AnimalHealth
    {
        public override async void TakeDamage(AnimalAttack attacker)
        {
            LastAttack = attacker;

            bool isAbilityApply = await ApplyAbilities();

            if (isAbilityApply) return;

            base.TakeDamage(attacker);
        }
        
        private async Task<bool> ApplyAbilities()
        {
            if (MergedAbilities.Count > 0)
            {
                foreach (IAbility ability in MergedAbilities)
                {
                    if (ability.CanUse)
                    {
                        //_animator.PlayAttackAnimation();
                        await ability.Apply();
                    }
                }
            }

            if (Ability.CanUse)
            {
                await Ability.Apply();
                return true;
            }

            return false;
        }
    }
}