using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Facades;
using Code.Data.Animals;
using Code.GridPathfinding;
using Framework.Code;
using Framework.Code.Infrastructure.Services.Assets;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Factories.Animals
{
    public class AnimalFactory : IAnimalFactory
    {
        private readonly IAssetProvider _assetProvider;
        private readonly DiContainer _container;
        private readonly AnimalDatabase _database;
        private Dictionary<AnimalType, AnimalFacade> _animalPrefabs;

        [Inject]
        public AnimalFactory(IAssetProvider assetProvider, DiContainer container, AnimalDatabase database)
        {
            _assetProvider = assetProvider;
            _container = container;
            _database = database;
        }

        public void Load()
        {
            _animalPrefabs = _assetProvider.LoadCollection<AnimalFacade>(AssetPath.Animals)
                .ToDictionary(animal => animal.Type);
        }

        public AnimalFacade Create(AnimalType type)
        {
            if (!_animalPrefabs.TryGetValue(type, out var prefab))
            {
                Debug.LogError($"[AnimalFactory] No prefab registered for {type} - check that a facade with this type exists in {AssetPath.Animals}");
                return null;
            }

            var animal = _container.InstantiatePrefabForComponent<AnimalFacade>(prefab);
            animal.ApplyStats(_database.GetStats(type));
            return animal;
        }

        public IReadOnlyList<ChickenFacade> CreateChickenFlock(IReadOnlyList<IGridCell> cells)
        {
            if (cells == null || cells.Count != ChickenFlock.Count)
            {
                Debug.LogError($"[AnimalFactory] Chicken flock needs exactly {ChickenFlock.Count} cells, got {cells?.Count ?? 0}");
                return new List<ChickenFacade>();
            }

            var flock = new List<ChickenFacade>(ChickenFlock.Count);

            foreach (var cell in cells)
            {
                var chicken = CreateChickenAtCell(cell);

                if (chicken == null)
                {
                    Debug.LogError("[AnimalFactory] Chicken flock creation failed - rolling back partially created chickens");
                    foreach (var created in flock)
                    {
                        if (created.Movement != null)
                            created.Movement.ClearNodes();

                        Object.Destroy(created.gameObject);
                    }

                    return new List<ChickenFacade>();
                }

                flock.Add(chicken);
            }

            foreach (var chicken in flock)
                chicken.RegisterNeighbors(flock.Where(other => other != chicken).ToList());

            return flock;
        }

        private ChickenFacade CreateChickenAtCell(IGridCell cell)
        {
            if (!(cell is GridCell gridCell))
            {
                Debug.LogError("[AnimalFactory] Cannot create chicken - cell is not a GridCell");
                return null;
            }

            if (!_animalPrefabs.TryGetValue(AnimalType.Chicken, out var prefab))
            {
                Debug.LogError($"[AnimalFactory] No prefab registered for {AnimalType.Chicken} - check that a facade with this type exists in {AssetPath.Animals}");
                return null;
            }

            var chicken = _container.InstantiatePrefabForComponent<AnimalFacade>(prefab) as ChickenFacade;

            if (chicken == null)
            {
                Debug.LogError("[AnimalFactory] Failed to create chicken instance");
                return null;
            }

            chicken.Movement.SetNewNode(gridCell);
            chicken.ApplyStats(_database.GetStats(AnimalType.Chicken));

            return chicken;
        }
    }
}