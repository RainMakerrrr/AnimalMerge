using Code.Abilities;
using Code.Data.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals.Facades
{
    public class VelociraptorFacade : EnemyAnimalFacade
    {
        private AnimalDatabase _database;

        [Inject]
        private void ConstructDatabase(AnimalDatabase database) => _database = database;

        public override void InitBehaviours()
        {
            var ownerRetreatChance = VelociraptorStats.DefaultOwnerRetreatChance;

            if (_database.GetStats(Type) is VelociraptorStats stats)
            {
                ownerRetreatChance = stats.OwnerRetreatChance;
            }
            else
            {
                Debug.LogWarning($"[VelociraptorFacade] Stats for {Type} are not {nameof(VelociraptorStats)}; using default retreat chances.");
            }

            Ability = new RetreatAbility(
                transformable: _movement,
                randomProvider: _randomProvider,
                minDistance: 1,
                maxDistance: 4,
                isOwner: true,
                successChance: ownerRetreatChance);

            AddAbility(Ability);
        }
    }
}
