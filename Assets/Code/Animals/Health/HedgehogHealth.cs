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
        
        public override async void TakeDamage(AnimalAttack attacker)
        {
            LastAttack = attacker;
            await ApplyAbilities();

            base.TakeDamage(attacker);
        }

        private async Task ApplyAbilities()
        {
            List<IAbility> abilities = new List<IAbility>(MergedAbilities);

            foreach (IAbility ability in abilities.OrderBy(a => a.Priority))
            {
                if (ability.CanUse)
                {
                    ability.Apply();
                    await Task.Delay(TimeSpan.FromSeconds(0.3f));
                }
            }

            // if (Ability.CanUse)
            // {
            //     _animator.PlayAttackAnimation();
            //     Ability.Apply();
            //     await Task.Delay(TimeSpan.FromSeconds(0.5f));
            // }
            //
            // if (MergedAbilities.Count > 0)
            // {
            //     foreach (IAbility ability in MergedAbilities)
            //     {
            //         if (ability.CanUse)
            //         {
            //             ability.Apply();
            //             return true;
            //         }
            //     }
            // }
            //
            // return false;
        }
    }
}