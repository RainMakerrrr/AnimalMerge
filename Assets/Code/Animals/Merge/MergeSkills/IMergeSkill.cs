using Code.Abilities;
using Code.Animals.Facades;
using Code.Animals.Vfx;
using Code.Animals.Vfx.Config;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals.Merge.MergeSkills
{
    public interface IMergeSkill
    {
        AnimalType AnimalType { get; }
        /// <summary>
        /// Merges this skill into the target animal
        /// </summary>
        /// <returns>True if merge was successful, false otherwise</returns>
        bool Merge(PlayerAnimalFacade animal);
        /// <summary>
        /// Merges this skill into the target animal, also applying skills from other animal
        /// </summary>
        /// <returns>True if merge was successful, false otherwise</returns>
        bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other);
    }

    public class ElephantMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Elephant;
        private readonly float _multiplier;

        public ElephantMergeSkill(float multiplier)
        {
            _multiplier = multiplier;
        }

        public bool Merge(PlayerAnimalFacade animal)
        {
            animal.UpgradeHealth(_multiplier);
            return true;
        }

        public bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other)
        {
            animal.UpgradeHealth(_multiplier);

            foreach (var mergeSkill in other.MergeSkills)
            {
                if (!mergeSkill.Merge(animal))
                {
                    Debug.LogWarning($"[ElephantMergeSkill] Failed to merge skill {mergeSkill.AnimalType}");
                }
            }
            return true;
        }
    }

    public class CheetahMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Cheetah;

        private readonly int _multiplier;

        public CheetahMergeSkill(int multiplier)
        {
            _multiplier = multiplier;
        }

        public bool Merge(PlayerAnimalFacade animal)
        {
            animal.UpgradeSpeed(_multiplier);
            return true;
        }

        public bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other)
        {
            animal.UpgradeSpeed(_multiplier);

            foreach (var mergeSkill in other.MergeSkills)
            {
                if (!mergeSkill.Merge(animal))
                {
                    Debug.LogWarning($"[CheetahMergeSkill] Failed to merge skill {mergeSkill.AnimalType}");
                }
            }
            return true;
        }
    }

    public class DeerMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Deer;

        private readonly float _multiplier;

        public DeerMergeSkill(float multiplier)
        {
            _multiplier = multiplier;
        }

        public bool Merge(PlayerAnimalFacade animal)
        {
            animal.UpgradeDamage(_multiplier);
            return true;
        }

        public bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other)
        {
            animal.UpgradeDamage(_multiplier);

            foreach (var mergeSkill in other.MergeSkills)
            {
                if (!mergeSkill.Merge(animal))
                {
                    Debug.LogWarning($"[DeerMergeSkill] Failed to merge skill {mergeSkill.AnimalType}");
                }
            }
            return true;
        }
    }

    public class HedgehogMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Hedgehog;

        public bool Merge(PlayerAnimalFacade animal)
        {
            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);

            animal.MergeSkills.ForEach(Debug.Log);

            animal.AddAbility(new CounterAttack(animal.Health, animal.Animator, animal.AttackInstance, isOwner: false, animal.RandomProvider));
            return true;
        }

        public bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other)
        {
            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);

            animal.AddAbility(new CounterAttack(animal.Health, animal.Animator, animal.AttackInstance, isOwner: false, animal.RandomProvider));

            foreach (var mergeSkill in other.MergeSkills)
            {
                if (!mergeSkill.Merge(animal))
                {
                    Debug.LogWarning($"[HedgehogMergeSkill] Failed to merge skill {mergeSkill.AnimalType}");
                }
            }
            return true;
        }
    }

    public class FoxMergeSkill : IMergeSkill
    {
        public AnimalType AnimalType => AnimalType.Fox;

        private readonly int _inheritedChance;

        public FoxMergeSkill(int inheritedChance)
        {
            _inheritedChance = inheritedChance;
        }

        public bool Merge(PlayerAnimalFacade animal)
        {
            animal.AddAbility(new Dodge(animal.Movement, animal.Colliders, _inheritedChance, animal.RandomProvider));

            Debug.Log(animal.gameObject.name);
            animal.MergeSkills.ForEach(Debug.Log);

            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);
            return true;
        }

        public bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other)
        {
            if (animal == null)
                return false;

            animal.AddAbility(new Dodge(animal.Movement, animal.Colliders, _inheritedChance, animal.RandomProvider));

            if (animal.MergeSkills.Contains(this) == false)
                animal.MergeSkills.Add(this);

            if (other == null)
                return true;

            foreach (var mergeSkill in other.MergeSkills)
            {
                if (!mergeSkill.Merge(animal))
                {
                    Debug.LogWarning($"[FoxMergeSkill] Failed to merge skill {mergeSkill.AnimalType}");
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Chicken merge skill - duplicates the target animal with 75% stats for both original and clone.
    /// When Chicken is merged into another animal (e.g., Elephant), creates a clone of that animal.
    /// Both the original and clone get 75% of their stats (HP and Damage).
    /// </summary>
    public class ChickenMergeSkill : IMergeSkill
    {
        private readonly Code.GridPathfinding.IGridManager _gridManager;
        private readonly Code.Infrastructure.Factories.Animals.IAnimalFactory _animalFactory;
        private readonly Code.Battle.Services.IUnitTracker _unitTracker;
        private readonly MergeAnimationConfig _animationConfig;

        public AnimalType AnimalType => AnimalType.Chicken;

        public ChickenMergeSkill(
            Code.GridPathfinding.IGridManager gridManager,
            Code.Infrastructure.Factories.Animals.IAnimalFactory animalFactory,
            Code.Battle.Services.IUnitTracker unitTracker,
            MergeAnimationConfig animationConfig)
        {
            _gridManager = gridManager;
            _animalFactory = animalFactory;
            _unitTracker = unitTracker;
            _animationConfig = animationConfig;
        }

        public bool Merge(PlayerAnimalFacade animal)
        {
            if (animal == null)
            {
                Debug.LogWarning("[ChickenMergeSkill] Cannot merge - target animal is null");
                return false;
            }

            // 1. First check if there's a free cell for clone BEFORE applying downgrade
            var freeCell = FindFreeMergeCell(animal);
            if (freeCell == null)
            {
                Debug.LogWarning($"[ChickenMergeSkill] No free cell found for clone! Merge aborted. Original {animal.Type} stats remain unchanged.");
                return false;
            }

            // 2. Downgrade original animal to 75% stats
            Debug.Log($"[ChickenMergeSkill] Downgrading {animal.Type} to 75% stats");
            animal.UpgradeHealth(0.75f);
            animal.UpgradeDamage(0.75f);

            // 3. Create clone of the same type
            Debug.Log($"[ChickenMergeSkill] Creating clone of {animal.Type} at {freeCell.GridPosition}");
            var clone = _animalFactory.Create(animal.Type) as PlayerAnimalFacade;
            if (clone == null)
            {
                Debug.LogError($"[ChickenMergeSkill] Failed to create clone of {animal.Type}!");
                return false;
            }

            // 4. Place clone on the grid using proper placement logic
            var gridCell = freeCell as Code.GridPathfinding.GridCell;
            if (gridCell == null)
            {
                Debug.LogError("[ChickenMergeSkill] Free cell is not a GridCell!");
                return false;
            }

            // Use Place() method which handles offsets correctly
            clone.Movement.Place(gridCell.WorldPosition);
            clone.Movement.SetCurrentNode(gridCell);

            // Mark all cells as occupied (this sets IsWalkable = false)
            _gridManager.SetOccupied(
                gridCell.GridPosition,
                clone.Movement.UnitSize,
                clone.Movement.Direction,
                clone.Movement);

            // Get neighbor cells (excluding the base cell) for the animal's internal tracking
            var neighbourCells = _gridManager.GetNeighborCells(
                gridCell.GridPosition,
                clone.Movement.UnitSize,
                clone.Movement.Direction);
            if (neighbourCells.Count > 0)
            {
                clone.Movement.FillNodes(neighbourCells.ConvertAll(cell => cell as Code.GridPathfinding.GridCell));
            }

            if (_unitTracker != null)
            {
                _unitTracker.RegisterPlayerUnit(clone);
            }
            else
            {
                Debug.LogWarning("[ChickenMergeSkill] No unit tracker - clone stays untracked");
            }

            // 5. Downgrade clone to 75% stats
            Debug.Log($"[ChickenMergeSkill] Downgrading clone to 75% stats");
            clone.UpgradeHealth(0.75f);
            clone.UpgradeDamage(0.75f);

            // 6. Copy all merge skills from original to clone
            Debug.Log($"[ChickenMergeSkill] Copying {animal.MergeSkills.Count} merge skills to clone");
            foreach (var skill in animal.MergeSkills)
            {
                if (!clone.MergeSkills.Contains(skill))
                {
                    clone.MergeSkills.Add(skill);
                }
            }

            // 7. Add this ChickenMergeSkill to both original and clone
            if (!animal.MergeSkills.Contains(this))
            {
                animal.MergeSkills.Add(this);
            }
            if (!clone.MergeSkills.Contains(this))
            {
                clone.MergeSkills.Add(this);
            }

            PlayCloneAppearAnimation(clone);

            Debug.Log($"[ChickenMergeSkill] Clone created successfully! Original and clone both have 75% stats.");
            return true;
        }

        public bool Merge(PlayerAnimalFacade animal, PlayerAnimalFacade other)
        {
            if (animal == null)
            {
                Debug.LogWarning("[ChickenMergeSkill] Cannot merge - target animal is null");
                return false;
            }

            // First, apply chicken merge (create clone with 75% stats)
            if (!Merge(animal))
            {
                Debug.LogWarning("[ChickenMergeSkill] Failed to create clone, merge aborted");
                return false;
            }

            // Then, apply merge skills from the other animal
            if (other != null)
            {
                foreach (var mergeSkill in other.MergeSkills)
                {
                    if (!mergeSkill.Merge(animal))
                    {
                        Debug.LogWarning($"[ChickenMergeSkill] Failed to merge skill {mergeSkill.AnimalType}");
                    }
                }
            }
            return true;
        }

        private void PlayCloneAppearAnimation(PlayerAnimalFacade clone)
        {
            if (_animationConfig == null)
                return;

            var appearAnimation = MergeAppearAnimation.Begin(
                clone.transform,
                clone.transform.localScale,
                _animationConfig);

            appearAnimation.PlayAsync(clone.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// Finds a free cell on the entire grid where a clone can be placed.
        /// Searches all cells on the grid, prioritizing cells closer to the animal's current position.
        /// Accounts for unit size - for 2x2 units, needs 4 free cells.
        /// </summary>
        private Code.GridPathfinding.IGridCell FindFreeMergeCell(AnimalFacade animal)
        {
            var currentCell = animal.Movement.CurrentPathNode;
            if (currentCell == null)
            {
                Debug.LogWarning("[ChickenMergeSkill] Animal has no current cell!");
                return null;
            }

            var unitSize = animal.Movement.UnitSize;
            var direction = animal.Movement.Direction;

            // First try nearby cells for better placement
            var searchOffsets = new[]
            {
                new UnityEngine.Vector2Int(1, 0),   // Right
                new UnityEngine.Vector2Int(-1, 0),  // Left
                new UnityEngine.Vector2Int(0, 1),   // Up
                new UnityEngine.Vector2Int(0, -1),  // Down
                new UnityEngine.Vector2Int(1, 1),   // Up-right
                new UnityEngine.Vector2Int(-1, 1),  // Up-left
                new UnityEngine.Vector2Int(1, -1),  // Down-right
                new UnityEngine.Vector2Int(-1, -1)  // Down-left
            };

            foreach (var offset in searchOffsets)
            {
                var testPosition = currentCell.GridPosition + offset;

                if (_gridManager.CanPlaceUnit(testPosition, unitSize, direction))
                {
                    var cell = _gridManager.GetCell(testPosition);
                    if (cell != null && cell.IsWalkable)
                    {
                        Debug.Log($"[ChickenMergeSkill] Found free cell nearby at {testPosition}");
                        return cell;
                    }
                }
            }

            // If no nearby cells found, search the entire grid
            Debug.Log("[ChickenMergeSkill] No nearby cells found, searching entire grid...");

            for (int x = 0; x < _gridManager.Width; x++)
            {
                for (int y = 0; y < _gridManager.Height; y++)
                {
                    var testPosition = new UnityEngine.Vector2Int(x, y);

                    if (_gridManager.CanPlaceUnit(testPosition, unitSize, direction))
                    {
                        var cell = _gridManager.GetCell(testPosition);
                        if (cell != null && cell.IsWalkable)
                        {
                            Debug.Log($"[ChickenMergeSkill] Found free cell on grid at {testPosition}");
                            return cell;
                        }
                    }
                }
            }

            Debug.LogWarning($"[ChickenMergeSkill] No free cell found on entire grid for {unitSize}");
            return null;
        }
    }
}