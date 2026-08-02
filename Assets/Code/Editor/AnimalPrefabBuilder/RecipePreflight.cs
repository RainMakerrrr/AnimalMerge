#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Merge.MergeAttributes;
using Code.Data.Animals;
using Code.GridPathfinding;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class RecipePreflight
    {
        private static readonly Dictionary<AnimalType, System.Type> ExpectedStatsTypes =
            new Dictionary<AnimalType, System.Type>
            {
                { AnimalType.Fox, typeof(FoxStats) },
                { AnimalType.Hedgehog, typeof(HedgehogStats) },
                { AnimalType.Chicken, typeof(ChickenStats) }
            };

        public static List<ValidationFinding> Run(AnimalRecipe recipe)
        {
            var findings = new List<ValidationFinding>();
            if (recipe == null) return findings;

            RequireAsset(findings, recipe, recipe.ModelPrefab, nameof(recipe.ModelPrefab));
            RequireAsset(findings, recipe, recipe.AnimatorController, nameof(recipe.AnimatorController));

            ValidateFacadeScript(findings, recipe);
            ValidateAttackScript(findings, recipe);
            ValidateStatsScript(findings, recipe);
            ValidateAttackClip(findings, recipe);
            ValidateAnimatorCount(findings, recipe);
            ValidateAllyMergeAttribute(findings, recipe);
            ValidateAllyFootprintTable(findings, recipe);
            ValidateAllyTypeUniqueness(findings, recipe);

            if (recipe.MaxAnimSpeed < recipe.MinAnimSpeed)
            {
                findings.Add(ValidationFinding.Warning(
                    "R25",
                    $"{recipe.name}: MaxAnimSpeed {recipe.MaxAnimSpeed} is below MinAnimSpeed {recipe.MinAnimSpeed}",
                    recipe));
            }

            return findings;
        }

        private static void RequireAsset(
            List<ValidationFinding> findings,
            AnimalRecipe recipe,
            Object asset,
            string fieldName)
        {
            if (asset != null) return;

            findings.Add(ValidationFinding.Error(
                "PRE",
                $"{recipe.name}: {fieldName} is not assigned",
                recipe));
        }

        private static void ValidateFacadeScript(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.FacadeScript == null)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: FacadeScript is not assigned",
                    recipe));
                return;
            }

            var type = recipe.FacadeScript.GetClass();
            if (type == null)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: FacadeScript does not resolve to a class",
                    recipe));
                return;
            }

            if (type.IsAbstract)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: FacadeScript {type.Name} is abstract and cannot be added to a prefab",
                    recipe));
                return;
            }

            if (!recipe.Side.FacadeBaseType.IsAssignableFrom(type))
            {
                findings.Add(ValidationFinding.Error(
                    "R19",
                    $"{recipe.name}: FacadeScript {type.Name} does not derive from " +
                    $"{recipe.Side.FacadeBaseType.Name}, which is required for the {recipe.Side.Name} side",
                    recipe));
            }
        }

        private static void ValidateAttackScript(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.AttackScript == null) return;

            var type = recipe.AttackScript.GetClass();
            if (type == null || !typeof(AnimalAttack).IsAssignableFrom(type) || type.IsAbstract)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: AttackScript must be a concrete class deriving from AnimalAttack",
                    recipe));
            }
        }

        private static void ValidateStatsScript(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.Stats != null)
            {
                CheckStatsType(findings, recipe, recipe.Stats.GetType());
                return;
            }

            if (recipe.StatsScript == null)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: assign either Stats or StatsScript so a stats asset can be resolved",
                    recipe));
                return;
            }

            var type = recipe.StatsScript.GetClass();
            if (type == null || !typeof(AnimalStats).IsAssignableFrom(type) || type.IsAbstract)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: StatsScript must be a concrete class deriving from AnimalStats",
                    recipe));
                return;
            }

            CheckStatsType(findings, recipe, type);
        }

        private static void CheckStatsType(List<ValidationFinding> findings, AnimalRecipe recipe, System.Type actual)
        {
            if (!ExpectedStatsTypes.TryGetValue(recipe.Type, out var expected)) return;
            if (expected.IsAssignableFrom(actual)) return;

            findings.Add(ValidationFinding.Error(
                "R17",
                $"{recipe.name}: {recipe.Type} needs a {expected.Name} asset but the recipe resolves to " +
                $"{actual.Name}. The facade checks the concrete type and falls back to code defaults",
                recipe));
        }

        private static void ValidateAttackClip(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.Type == AnimalType.Hedgehog) return;

            var overridesAttack = recipe.AttackScript != null
                                  && recipe.AttackScript.GetClass() != typeof(AnimalAttack);

            var severity = overridesAttack ? FindingSeverity.Warning : FindingSeverity.Error;

            if (recipe.AttackClip == null)
            {
                findings.Add(new ValidationFinding(
                    "R2",
                    severity,
                    $"{recipe.name}: AttackClip is not assigned",
                    recipe));
                return;
            }

            var hasEvent = AnimationUtility.GetAnimationEvents(recipe.AttackClip)
                .Any(e => e.functionName == AnimalPrefabConstants.AttackEventFunctionName);

            if (hasEvent) return;

            findings.Add(new ValidationFinding(
                "R2",
                severity,
                $"{recipe.name}: attack clip '{recipe.AttackClip.name}' has no " +
                $"{AnimalPrefabConstants.AttackEventFunctionName} animation event. " +
                "AnimalAttack.Attack spins on _isAttackDone and the turn hangs forever. " +
                "Use Tools/Animals/Add Attack Animation Event on a standalone clip",
                recipe));
        }

        private static void ValidateAnimatorCount(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.ModelPrefab == null) return;

            var animators = recipe.ModelPrefab.GetComponentsInChildren<Animator>(true);
            if (animators.Length <= 1) return;

            findings.Add(ValidationFinding.Warning(
                "R4",
                $"{recipe.name}: the model carries {animators.Length} Animators. Clips played by the extra " +
                "ones dispatch their animation events to their own GameObject, not to the prefab root",
                recipe));
        }

        private static void ValidateAllyMergeAttribute(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            var ally = recipe as AllyAnimalRecipe;
            if (ally == null || ally.MergeAttributeScript == null) return;

            var type = ally.MergeAttributeScript.GetClass();
            if (type == null || !typeof(VisualMergeAttribute).IsAssignableFrom(type) || type.IsAbstract)
            {
                findings.Add(ValidationFinding.Error(
                    "PRE",
                    $"{recipe.name}: MergeAttributeScript must be a concrete class deriving from " +
                    "VisualMergeAttribute",
                    recipe));
            }
        }

        private static void ValidateAllyFootprintTable(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.Side != AnimalSideProfile.Ally) return;
            if (recipe.Type == AnimalType.Chicken) return;

            var method = typeof(GridManager).GetMethod(
                "GetFootprint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (method == null) return;

            var declared = (UnitSize)method.Invoke(null, new object[] { recipe.Type });
            var recipeSize = recipe.Footprint.ToUnitSize();

            if (declared == recipeSize) return;

            findings.Add(ValidationFinding.Error(
                "R14",
                $"{recipe.name}: GridManager.GetFootprint({recipe.Type}) is {declared} but the recipe " +
                $"footprint is {recipeSize}. Add '{recipe.Type} => new UnitSize({recipeSize.Width}, " +
                $"{recipeSize.Height})' to GetFootprint before building",
                recipe));
        }

        private static void ValidateAllyTypeUniqueness(List<ValidationFinding> findings, AnimalRecipe recipe)
        {
            if (recipe.Side != AnimalSideProfile.Ally) return;

            var outputPath = recipe.OutputPath;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { AnimalPrefabPaths.AnimalsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == outputPath) continue;

                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var facade = existing == null ? null : existing.GetComponent<AnimalFacade>();
                if (facade == null || facade.Type != recipe.Type) continue;

                findings.Add(ValidationFinding.Error(
                    "R18",
                    $"{recipe.name}: {path} already declares _type {recipe.Type}. " +
                    "AnimalFactory.Load throws on ToDictionary and the whole factory dies",
                    recipe));
            }
        }
    }
}
#endif
