using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Facades
{
    public class VelociraptorFacade : EnemyAnimalFacade
    {
        public override void InitBehaviours()
        {
            // Register Retreat as a post-attack ability
            Ability = new RetreatAbility(
                transformable: _movement,
                randomProvider: _randomProvider,
                minDistance: 1,
                maxDistance: 4,
                isOwner: true  // Boss Velociraptor: 100% success rate
            );

            // Register in facade's AbilityManager (used by both AnimalAttack and AnimalHealth)
            AddAbility(Ability);

            Debug.Log("[VelociraptorFacade] Registered RetreatAbility");
        }
    }
}