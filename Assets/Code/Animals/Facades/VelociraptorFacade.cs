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

            _health.SetAbility(Ability);

            Debug.Log("[VelociraptorFacade] Registered RetreatAbility");
        }
    }
}