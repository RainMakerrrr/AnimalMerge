using System.Collections.Generic;
using Code.Animals.Merge.MergeSkills;
using Code.GridPathfinding;
using Code.Infrastructure.Factories.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals.Facades
{
    /// <summary>
    /// Facade for Chicken - each chicken is a separate 1x1 unit.
    /// CHICKEN FEATURE: When creating 1 chicken, 3 additional chickens spawn nearby.
    /// Each chicken is independent but linked via _neighborChickens for "remove all" functionality.
    /// - Merge Skill: ChickenMergeSkill - duplicates merged animal with 75% stats
    /// </summary>
    public class ChickenFacade : PlayerAnimalFacade
    {
        private IGridManager _gridManager;
        private IAnimalFactory _animalFactory;

        /// <summary>
        /// References to neighboring chickens spawned together with this one.
        /// Used for "remove to deck" functionality - removing one removes all neighbors.
        /// </summary>
        private List<ChickenFacade> _neighborChickens = new List<ChickenFacade>();

        [Inject]
        private void ConstructChicken([Inject(Id = GridIdentifier.MergeGrid)] IGridManager gridManager, IAnimalFactory animalFactory)
        {
            _gridManager = gridManager;
            _animalFactory = animalFactory;
        }

        public override void InitBehaviours()
        {
            // ChickenMergeSkill - duplicates merged animal with 75% stats
            MergeSkill = new ChickenMergeSkill(_gridManager, _animalFactory);

            // MultipleCharacters - this is a MERGE SKILL, NOT auto-spawn!
            // It will be added when this chicken is merged with another animal.

            Debug.Log($"[ChickenFacade] Initialized - {gameObject.name}");
            Debug.Log($"[ChickenFacade] MergeSkill: {MergeSkill}");
            Debug.Log($"[ChickenFacade] MergeSkills count: {MergeSkills.Count}");
            MergeSkills.ForEach(Debug.Log);
        }

        /// <summary>
        /// Registers neighboring chickens.
        /// Called by AnimalFactory after creating a group of 4 chickens.
        /// </summary>
        public void RegisterNeighbors(List<ChickenFacade> neighbors)
        {
            _neighborChickens.Clear();
            _neighborChickens.AddRange(neighbors);
            Debug.Log($"[ChickenFacade] {gameObject.name} registered {_neighborChickens.Count} neighbors");
        }

        /// <summary>
        /// TODO (Future): Called when removing this chicken to deck.
        /// Removes all neighboring chickens along with this one.
        /// </summary>
        public void OnRemovedToDeck()
        {
            Debug.Log($"[ChickenFacade] {gameObject.name} removing {_neighborChickens.Count} neighbors");

            // Remove all neighbors
            foreach (var neighbor in _neighborChickens)
            {
                if (neighbor != null && neighbor.gameObject != null)
                {
                    Destroy(neighbor.gameObject);
                }
            }

            // Clear the list
            _neighborChickens.Clear();

            // Remove self
            Destroy(gameObject);
        }
    }
}
