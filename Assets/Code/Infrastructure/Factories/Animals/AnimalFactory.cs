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
        private readonly IGridManager _gridManager;
        private readonly AnimalDatabase _database;
        private Dictionary<AnimalType, AnimalFacade> _animalPrefabs;
        private IGridManager _mergeGrid;

        [Inject]
        public AnimalFactory(IAssetProvider assetProvider, DiContainer container, [Inject(Id = GridIdentifier.MergeGrid)]IGridManager gridManager, AnimalDatabase database)
        {
            _assetProvider = assetProvider;
            _container = container;
            _gridManager = gridManager;
            _database = database;
        }

        public void Load()
        {
            _animalPrefabs = _assetProvider.LoadCollection<AnimalFacade>(AssetPath.Animals)
                .ToDictionary(animal => animal.Type);
        }

        //todo Temp, delete later
        public void SetMergeGrid(IGridManager mergeGrid)
        {
            _mergeGrid = mergeGrid;
        }
        
        public AnimalFacade Create(AnimalType type)
        {
            var animal = _container.InstantiatePrefabForComponent<AnimalFacade>(_animalPrefabs[type]);
            animal.ApplyStats(_database.GetStats(type));
            return animal;
        }

        /// <summary>
        /// CHICKEN FEATURE: Spawns 3 additional chickens near the main chicken
        /// and registers them as neighbors for future "remove all" functionality.
        /// Returns list of created chickens (without the main chicken).
        /// </summary>
        public List<ChickenFacade> SpawnAdditionalChickens(ChickenFacade mainChicken)
        {
            var additionalChickens = new List<ChickenFacade>();

            if (mainChicken == null)
            {
                Debug.LogWarning("[AnimalFactory] SpawnAdditionalChickens called with null mainChicken");
                return additionalChickens;
            }

            // Find 3 free neighboring cells
            var mainCell = mainChicken.Movement.CurrentPathNode;
            if (mainCell == null)
            {
                Debug.LogWarning("[AnimalFactory] Main chicken has no CurrentPathNode - cannot spawn neighbors");
                return additionalChickens;
            }

            var freeCells = FindFreeNeighborCells(mainCell, 3);
            if (freeCells.Count == 0)
            {
                Debug.LogWarning("[AnimalFactory] No free cells found for additional chickens");
                return additionalChickens;
            }

            Debug.Log($"[AnimalFactory] Spawning {freeCells.Count} additional chickens near {mainChicken.name}");

            // Create additional chickens at the free cells
            foreach (var cell in freeCells)
            {
                var chicken = CreateChickenAtCell(cell);
                if (chicken != null)
                {
                    additionalChickens.Add(chicken);
                }
            }

            if (additionalChickens.Count == 0)
            {
                Debug.LogWarning("[AnimalFactory] Failed to create any additional chickens");
                return additionalChickens;
            }

            // Register neighbors: all 4 chickens know about each other
            // Main chicken knows about the 3 additional chickens
            mainChicken.RegisterNeighbors(additionalChickens);

            // Each additional chicken knows about the main + other additional chickens
            foreach (var chicken in additionalChickens)
            {
                var allNeighbors = new List<ChickenFacade> { mainChicken };
                allNeighbors.AddRange(additionalChickens.Where(c => c != chicken));
                chicken.RegisterNeighbors(allNeighbors);
            }

            Debug.Log($"[AnimalFactory] Successfully created {additionalChickens.Count} additional chickens with neighbor tracking");
            return additionalChickens;
        }

        /// <summary>
        /// Finds up to 'count' free neighbor cells around the origin cell.
        /// Searches in order: cardinal directions first, then diagonals.
        /// </summary>
        private List<IGridCell> FindFreeNeighborCells(IGridCell origin, int count)
        {
            var result = new List<IGridCell>();
            if (origin == null) return result;

            // Check neighbors in priority order: cardinal directions first, then diagonals
            var offsets = new Vector2Int[]
            {
                new Vector2Int(1, 0),   // Right
                new Vector2Int(0, 1),   // Up
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(0, -1),  // Down
                new Vector2Int(1, 1),   // Diagonal: top-right
                new Vector2Int(-1, 1),  // Diagonal: top-left
                new Vector2Int(1, -1),  // Diagonal: bottom-right
                new Vector2Int(-1, -1)  // Diagonal: bottom-left
            };

            foreach (var offset in offsets)
            {
                if (result.Count >= count) break;

                var pos = origin.GridPosition + offset;
                var cell = _mergeGrid.GetCell(pos);

                // Check if cell is walkable and can place a 1x1 unit (chicken)
                if (cell != null && cell.IsWalkable && _mergeGrid.CanPlaceUnit(pos, UnitSize.Small, Direction.North))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a chicken at the specified cell.
        /// </summary>
        private ChickenFacade CreateChickenAtCell(IGridCell cell)
        {
            if (cell == null)
            {
                Debug.LogError("[AnimalFactory] Cannot create chicken - cell is null");
                return null;
            }

            // Create chicken instance
            var chicken = _container.InstantiatePrefabForComponent<AnimalFacade>(_animalPrefabs[AnimalType.Chicken]) as ChickenFacade;
            if (chicken == null)
            {
                Debug.LogError("[AnimalFactory] Failed to create chicken instance");
                return null;
            }

            // Place chicken at the cell
            var gridCell = cell as GridCell;
            if (gridCell != null)
            {
                chicken.Movement.SetNewNode(gridCell);
                chicken.ApplyStats(_database.GetStats(AnimalType.Chicken));
                Debug.Log($"[AnimalFactory] Created chicken at {cell.GridPosition}");
            }
            else
            {
                Debug.LogError("[AnimalFactory] Cell is not a GridCell - cannot place chicken");
                Object.Destroy(chicken.gameObject);
                return null;
            }

            return chicken;
        }
    }
}