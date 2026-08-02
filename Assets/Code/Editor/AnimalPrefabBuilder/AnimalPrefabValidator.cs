#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Merge;
using Code.Animals.Merge.MergeAttributes;
using Code.Animals.Movement;
using Code.Animals.Vfx;
using Code.Data.Animals;
using Code.GridPathfinding;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalPrefabValidator
    {
        private static readonly Dictionary<AnimalType, System.Type> ExpectedStatsTypes =
            new Dictionary<AnimalType, System.Type>
            {
                { AnimalType.Fox, typeof(FoxStats) },
                { AnimalType.Hedgehog, typeof(HedgehogStats) },
                { AnimalType.Chicken, typeof(ChickenStats) }
            };

        public static List<ValidationFinding> Validate(
            GameObject root,
            AnimalSideProfile side,
            AnimalPrefabAuditContext context)
        {
            var findings = new List<ValidationFinding>();
            if (root == null || side == null) return findings;

            var facade = root.GetComponent<AnimalFacade>();
            if (facade == null)
            {
                findings.Add(ValidationFinding.Error("R19", $"{root.name}: no AnimalFacade on the root", root));
                return findings;
            }

            var movement = root.GetComponent<AnimalMovement>();
            var attack = root.GetComponent<AnimalAttack>();
            var animalAnimator = root.GetComponent<AnimalAnimator>();
            var animators = root.GetComponentsInChildren<Animator>(true);
            var rootAnimator = root.GetComponent<Animator>();

            ValidateFacadeSide(findings, root, facade, side);
            ValidateFootprint(findings, root, facade, movement, side, context);
            ValidateAnimators(findings, root, animators, rootAnimator, attack);
            ValidateAttack(findings, root, facade, attack, animalAnimator, side);
            ValidateAnimatorController(findings, root, rootAnimator);
            ValidateLayers(findings, root, side);
            ValidateColliders(findings, root, facade, side);
            ValidateRootTransform(findings, root);
            ValidateRuntimeOnlyComponents(findings, root);
            ValidateDatabase(findings, root, facade, context);
            ValidateMovementTuning(findings, root, movement);

            if (side == AnimalSideProfile.Ally)
            {
                ValidateTypeUniqueness(findings, root, facade, context);
                ValidateMergePlacement(findings, root, movement, side);
                ValidateMergeVisualSlots(findings, root, facade, context);
            }

            return findings;
        }

        private static void ValidateFacadeSide(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalSideProfile side)
        {
            if (!side.FacadeBaseType.IsInstanceOfType(facade))
            {
                findings.Add(ValidationFinding.Error(
                    "R19",
                    $"{root.name}: facade {facade.GetType().Name} does not derive from {side.FacadeBaseType.Name}, " +
                    $"but the prefab sits in the {side.Name} folder",
                    root));
            }
        }

        private static void ValidateFootprint(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalMovement movement,
            AnimalSideProfile side,
            AnimalPrefabAuditContext context)
        {
            if (movement == null)
            {
                findings.Add(ValidationFinding.Error("R1", $"{root.name}: no AnimalMovement on the root", root));
                return;
            }

            var size = movement.UnitSize;
            if (!AnimalFootprintExtensions.IsSupportedByGridStack(size))
            {
                findings.Add(ValidationFinding.Error(
                    "R1",
                    $"{root.name}: _unitSize {size} is not supported by the grid stack. " +
                    "TargetPositionCalculator.GetAnchorPointsForCell returns an empty list, so the unit " +
                    "silently never moves and never attacks. Only 1x1, 1x2 and 2x2 are safe",
                    root));
            }

            if (side != AnimalSideProfile.Ally) return;
            if (facade.Type == AnimalType.Chicken) return;

            if (!TryGetGridFootprint(facade.Type, out var declared)) return;

            if (declared != size)
            {
                findings.Add(ValidationFinding.Error(
                    "R14",
                    $"{root.name}: GridManager.GetFootprint({facade.Type}) is {declared} but the prefab " +
                    $"_unitSize is {size}. HasCellFor reserves a different shape than PlaceOnGrid occupies",
                    root));
            }
        }

        private static void ValidateAnimators(
            List<ValidationFinding> findings,
            GameObject root,
            Animator[] animators,
            Animator rootAnimator,
            AnimalAttack attack)
        {
            if (rootAnimator == null)
            {
                findings.Add(ValidationFinding.Error("R3", $"{root.name}: no Animator on the root", root));
                return;
            }

            if (animators.Length > 1)
            {
                var extra = string.Join(", ", animators
                    .Where(a => a != rootAnimator)
                    .Select(a => a.gameObject.name));

                findings.Add(ValidationFinding.Warning(
                    "R4",
                    $"{root.name}: {animators.Length} Animators in the hierarchy ({extra}). " +
                    "Harmless while the extra ones only play cosmetic clips, but any clip they play " +
                    "dispatches its animation events to their own GameObject, not to the root",
                    root));
            }

            if (attack != null && attack.gameObject != rootAnimator.gameObject)
            {
                findings.Add(ValidationFinding.Error(
                    "R3",
                    $"{root.name}: the attack component sits on '{attack.gameObject.name}' but the Animator " +
                    $"is on '{rootAnimator.gameObject.name}'. AttackAnimationHandler will never fire",
                    root));
            }

            if (rootAnimator.cullingMode == AnimatorCullingMode.CullCompletely)
            {
                findings.Add(ValidationFinding.Warning(
                    "R7",
                    $"{root.name}: Animator.cullingMode is CullCompletely, which stops the state machine " +
                    "off-screen. Animation events never fire and the turn hangs",
                    root));
            }

            if (rootAnimator.applyRootMotion)
            {
                findings.Add(ValidationFinding.Warning(
                    "R21",
                    $"{root.name}: Animator.applyRootMotion is on and fights AnimalMovement for the transform",
                    root));
            }
        }

        private static void ValidateAttack(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalAttack attack,
            AnimalAnimator animalAnimator,
            AnimalSideProfile side)
        {
            if (attack == null)
            {
                findings.Add(ValidationFinding.Error("R5", $"{root.name}: no AnimalAttack on the root", root));
                return;
            }

            var isHedgehog = facade.Type == AnimalType.Hedgehog;
            var overridesAttack = attack.GetType() != typeof(AnimalAttack);

            if (SerializedFieldReader.TryGetObject(attack, "_attackPoint", out var attackPoint)
                && attackPoint == null
                && !isHedgehog)
            {
                findings.Add(ValidationFinding.Error(
                    "R5",
                    $"{root.name}: AnimalAttack._attackPoint is not assigned. " +
                    "AttackAnimationHandlerAsync returns early and the turn hangs forever",
                    root));
            }

            if (SerializedFieldReader.TryGetInt(attack, "_maxTargets", out var maxTargets) && maxTargets < 1)
            {
                findings.Add(ValidationFinding.Error(
                    "R6",
                    $"{root.name}: AnimalAttack._maxTargets is {maxTargets}. The overlap buffer is empty, " +
                    "no target is ever found and the turn hangs",
                    root));
            }

            if (SerializedFieldReader.TryGetLayerMask(attack, "_mask", out var bits) && bits != side.AttackMask)
            {
                findings.Add(ValidationFinding.Error(
                    "R12",
                    $"{root.name}: AnimalAttack._mask is {bits}, expected {side.AttackMask} " +
                    $"(1 << {side.TargetLayerName}). The attacker cannot see the other side",
                    root));
            }

            if (attack.IsAoE && maxTargets == 1)
            {
                findings.Add(ValidationFinding.Warning(
                    "R25",
                    $"{root.name}: _isAoE is on but _maxTargets is 1, so the AoE can only ever hit one target",
                    root));
            }

            if (isHedgehog) return;

            if (animalAnimator == null)
            {
                findings.Add(ValidationFinding.Error("R2", $"{root.name}: no AnimalAnimator on the root", root));
                return;
            }

            SerializedFieldReader.TryGetObject(animalAnimator, "_attackClip", out var clipObject);
            var clip = clipObject as AnimationClip;

            if (clip == null)
            {
                var severity = overridesAttack ? FindingSeverity.Warning : FindingSeverity.Error;
                findings.Add(new ValidationFinding(
                    "R2",
                    severity,
                    $"{root.name}: AnimalAnimator._attackClip is not assigned" +
                    (overridesAttack ? $" ({attack.GetType().Name} may override the attack flow)" : string.Empty),
                    root));
                return;
            }

            var hasEvent = AnimationUtility.GetAnimationEvents(clip)
                .Any(e => e.functionName == AnimalPrefabConstants.AttackEventFunctionName);

            if (!hasEvent)
            {
                var severity = overridesAttack ? FindingSeverity.Warning : FindingSeverity.Error;
                findings.Add(new ValidationFinding(
                    "R2",
                    severity,
                    $"{root.name}: attack clip '{clip.name}' has no " +
                    $"{AnimalPrefabConstants.AttackEventFunctionName} animation event. " +
                    "AnimalAttack.Attack spins on _isAttackDone and the turn hangs forever" +
                    (overridesAttack ? $" (unless {attack.GetType().Name} overrides the attack flow)" : string.Empty),
                    root));
            }
        }

        private static void ValidateAnimatorController(
            List<ValidationFinding> findings,
            GameObject root,
            Animator rootAnimator)
        {
            if (rootAnimator == null) return;

            var controller = ResolveController(rootAnimator.runtimeAnimatorController);
            if (controller == null)
            {
                findings.Add(ValidationFinding.Warning(
                    "R9",
                    $"{root.name}: animator controller could not be resolved as an AnimatorController, " +
                    "parameter checks were skipped",
                    root));
                return;
            }

            var parameters = controller.parameters.ToDictionary(p => p.name, p => p.type);

            RequireParameter(findings, root, parameters, AnimalPrefabConstants.MoveSpeedParameter,
                AnimatorControllerParameterType.Float, "R9", FindingSeverity.Error);
            RequireParameter(findings, root, parameters, AnimalPrefabConstants.AttackParameter,
                AnimatorControllerParameterType.Trigger, "R9", FindingSeverity.Error);
            RequireParameter(findings, root, parameters, AnimalPrefabConstants.TakeDamageParameter,
                AnimatorControllerParameterType.Trigger, "R9", FindingSeverity.Error);
            RequireParameter(findings, root, parameters, AnimalPrefabConstants.IsDeadParameter,
                AnimatorControllerParameterType.Bool, "R8", FindingSeverity.Error);
            RequireParameter(findings, root, parameters, AnimalPrefabConstants.TurnDirectionParameter,
                AnimatorControllerParameterType.Float, "R10", FindingSeverity.Warning);
            RequireParameter(findings, root, parameters, AnimalPrefabConstants.JumpParameter,
                AnimatorControllerParameterType.Trigger, "R10", FindingSeverity.Warning);
            RequireParameter(findings, root, parameters, AnimalPrefabConstants.CounterAttackParameter,
                AnimatorControllerParameterType.Trigger, "R10", FindingSeverity.Warning);

            if (!HasAnyStateDeathTransition(controller))
            {
                findings.Add(ValidationFinding.Error(
                    "R11",
                    $"{root.name}: controller '{controller.name}' has no Any State transition conditioned on " +
                    $"{AnimalPrefabConstants.IsDeadParameter}. Death will not play when the unit dies " +
                    "mid-move or mid-attack",
                    root));
            }
        }

        private static void RequireParameter(
            List<ValidationFinding> findings,
            GameObject root,
            Dictionary<string, AnimatorControllerParameterType> parameters,
            string name,
            AnimatorControllerParameterType expected,
            string ruleId,
            FindingSeverity severity)
        {
            if (!parameters.TryGetValue(name, out var actual))
            {
                findings.Add(new ValidationFinding(
                    ruleId,
                    severity,
                    $"{root.name}: animator parameter '{name}' is missing, AnimalAnimator drives it as {expected}",
                    root));
                return;
            }

            if (actual != expected)
            {
                findings.Add(new ValidationFinding(
                    ruleId,
                    FindingSeverity.Error,
                    $"{root.name}: animator parameter '{name}' is {actual} but AnimalAnimator drives it as " +
                    $"{expected}. The call silently does nothing",
                    root));
            }
        }

        private static bool HasAnyStateDeathTransition(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine == null) continue;
                if (HasAnyStateDeathTransition(layer.stateMachine)) return true;
            }

            return false;
        }

        private static bool HasAnyStateDeathTransition(AnimatorStateMachine stateMachine)
        {
            var found = stateMachine.anyStateTransitions.Any(transition =>
                transition.conditions.Any(c => c.parameter == AnimalPrefabConstants.IsDeadParameter));

            if (found) return true;

            foreach (var child in stateMachine.stateMachines)
            {
                if (child.stateMachine == null) continue;
                if (HasAnyStateDeathTransition(child.stateMachine)) return true;
            }

            return false;
        }

        private static AnimatorController ResolveController(RuntimeAnimatorController runtime)
        {
            switch (runtime)
            {
                case AnimatorController direct:
                    return direct;
                case AnimatorOverrideController over:
                    return ResolveController(over.runtimeAnimatorController);
                default:
                    return null;
            }
        }

        private static void ValidateLayers(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalSideProfile side)
        {
            var expected = side.Layer;
            var foreign = AnimalSideProfile.Ally == side ? AnimalSideProfile.Enemy.Layer : AnimalSideProfile.Ally.Layer;
            var healthBarLayer = AnimalPrefabConstants.HealthBarLayer;

            if (root.layer != expected)
            {
                findings.Add(ValidationFinding.Error(
                    "R12",
                    $"{root.name}: root layer is {LayerMask.LayerToName(root.layer)}, expected {side.OwnLayerName}",
                    root));
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                var layer = collider.gameObject.layer;
                if (layer == healthBarLayer) continue;

                if (layer == foreign)
                {
                    findings.Add(ValidationFinding.Error(
                        "R12",
                        $"{root.name}: collider '{collider.name}' is on the opposing layer " +
                        $"{LayerMask.LayerToName(layer)}",
                        root));
                }
            }

            var colliderChild = root.transform.Find(side.ColliderChildName);
            if (colliderChild == null) return;

            if (colliderChild.gameObject.layer != expected)
            {
                findings.Add(ValidationFinding.Error(
                    "R12",
                    $"{root.name}: '{side.ColliderChildName}' child is on layer " +
                    $"{LayerMask.LayerToName(colliderChild.gameObject.layer)}, expected {side.OwnLayerName}. " +
                    "EnemySpawnService sets only the root layer at spawn, so children keep what is serialized",
                    root));
            }
        }

        private static void ValidateColliders(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalSideProfile side)
        {
            if (!SerializedFieldReader.TryGetObjectArray(facade, "_colliders", out var serialized)) return;

            if (serialized.Length == 0)
            {
                findings.Add(ValidationFinding.Error(
                    "R23",
                    $"{root.name}: facade _colliders is empty. AnimalHealth.Construct receives nothing and " +
                    "Dodge cannot disable anything",
                    root));
                return;
            }

            if (serialized.Any(c => c == null))
            {
                findings.Add(ValidationFinding.Error(
                    "R23",
                    $"{root.name}: facade _colliders contains a null entry",
                    root));
            }

            var healthBar = root.transform.Find(AnimalPrefabConstants.HealthBarChildName);

            var expected = root.GetComponentsInChildren<Collider>(true)
                .Where(c => c.gameObject.layer == side.Layer)
                .Where(c => healthBar == null || !c.transform.IsChildOf(healthBar))
                .ToList();

            var missing = expected.Where(c => !serialized.Contains(c)).ToList();
            if (missing.Count == 0) return;

            var names = string.Join(", ", missing.Select(c => c.name));
            findings.Add(ValidationFinding.Warning(
                "R23",
                $"{root.name}: facade _colliders omits {missing.Count} collider(s) on the {side.OwnLayerName} " +
                $"layer ({names}). Dodge will not disable them, so the unit stays hittable while dodging",
                root));
        }

        private static void ValidateRootTransform(List<ValidationFinding> findings, GameObject root)
        {
            if (root.transform.localPosition != Vector3.zero)
            {
                findings.Add(ValidationFinding.Warning(
                    "R20",
                    $"{root.name}: root localPosition is {root.transform.localPosition}, expected zero. " +
                    "This is a leftover scene position saved into the prefab",
                    root));
            }

            if (root.transform.localRotation != Quaternion.identity)
            {
                findings.Add(ValidationFinding.Warning(
                    "R20",
                    $"{root.name}: root localRotation is not identity. AnimalMovement drives root rotation, " +
                    "so facing must be corrected on the mesh child instead",
                    root));
            }
        }

        private static void ValidateRuntimeOnlyComponents(List<ValidationFinding> findings, GameObject root)
        {
            var authored = root.GetComponentsInChildren<MergeScaleAnimator>(true);
            if (authored.Length == 0) return;

            findings.Add(ValidationFinding.Error(
                "R15",
                $"{root.name}: MergeScaleAnimator is authored on the prefab. It must only be added at runtime " +
                "by PlayerAnimalFacade.EnsureScaleAnimator, ElephantMergeAttribute or ChickenMergeSkill",
                root));
        }

        private static void ValidateDatabase(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalPrefabAuditContext context)
        {
            if (context?.Database == null) return;

            var stats = context.Database.GetStats(facade.Type);
            if (stats == null)
            {
                findings.Add(ValidationFinding.Error(
                    "R17",
                    $"{root.name}: AnimalDatabase has no stats for {facade.Type}. " +
                    "ApplyStats silently does nothing and the unit keeps its default numbers",
                    root));
                return;
            }

            if (!ExpectedStatsTypes.TryGetValue(facade.Type, out var expectedType)) return;

            if (!expectedType.IsInstanceOfType(stats))
            {
                findings.Add(ValidationFinding.Error(
                    "R17",
                    $"{root.name}: stats asset for {facade.Type} is {stats.GetType().Name}, expected " +
                    $"{expectedType.Name}. The facade checks the concrete type and falls back to code defaults",
                    root));
            }
        }

        private static void ValidateMovementTuning(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalMovement movement)
        {
            if (movement == null) return;

            if (!SerializedFieldReader.TryGetFloat(movement, "_minAnimSpeed", out var min)) return;
            if (!SerializedFieldReader.TryGetFloat(movement, "_maxAnimSpeed", out var max)) return;

            if (max < min)
            {
                findings.Add(ValidationFinding.Warning(
                    "R25",
                    $"{root.name}: _maxAnimSpeed {max} is below _minAnimSpeed {min}",
                    root));
            }
        }

        private static void ValidateTypeUniqueness(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalPrefabAuditContext context)
        {
            if (context == null) return;
            if (!context.AllyPathsByType.TryGetValue(facade.Type, out var paths)) return;
            if (paths.Count <= 1) return;

            findings.Add(ValidationFinding.Error(
                "R18",
                $"{root.name}: {paths.Count} prefabs in the ally folder declare _type {facade.Type} " +
                $"({string.Join(", ", paths)}). AnimalFactory.Load throws on ToDictionary and the factory dies",
                root));
        }

        private static void ValidateMergePlacement(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalMovement movement,
            AnimalSideProfile side)
        {
            if (movement == null) return;

            var mergeTarget = root.GetComponentInChildren<MergeTarget>(true);
            if (mergeTarget == null)
            {
                findings.Add(ValidationFinding.Error(
                    "R22",
                    $"{root.name}: no MergeTarget anywhere in the hierarchy, so nothing can be merged into " +
                    "this animal",
                    root));
                return;
            }

            var sphere = mergeTarget.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                findings.Add(ValidationFinding.Error(
                    "R22",
                    $"{root.name}: MergeTarget on '{mergeTarget.name}' has no SphereCollider, so the " +
                    "placement raycast can never hit it",
                    root));
                return;
            }

            var scale = root.transform.localScale;
            var planarScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var worldRadius = sphere.radius * planarScale;
            var worldCenterX = sphere.center.x * scale.x;
            var worldCenterZ = sphere.center.z * scale.z;

            var probeZ = AnimalPrefabConstants.LargestRaycastOffsetInProject;
            var dx = worldCenterX;
            var dz = probeZ - worldCenterZ;

            if (dx * dx + dz * dz <= worldRadius * worldRadius) return;

            findings.Add(ValidationFinding.Error(
                "R22",
                $"{root.name}: the merge sphere does not cover the placement probe at world XZ (0, {probeZ}). " +
                "AnimalMovement.TryPlace raycasts down from the dragged animal position plus its " +
                "_raycastOffset, so nothing can be merged into this animal",
                root));
        }

        private static void ValidateMergeVisualSlots(
            List<ValidationFinding> findings,
            GameObject root,
            AnimalFacade facade,
            AnimalPrefabAuditContext context)
        {
            if (context == null) return;

            var present = new HashSet<AnimalType>(root
                .GetComponentsInChildren<MergeVisualSlot>(true)
                .Select(slot => slot.SourceType));

            foreach (var sourceType in context.SlotMergeAllyTypes)
            {
                if (sourceType == facade.Type) continue;
                if (present.Contains(sourceType)) continue;

                findings.Add(ValidationFinding.Error(
                    "R24",
                    $"{root.name}: no MergeVisualSlot with _sourceType {sourceType}. " +
                    $"Merging a {sourceType} into this animal grants the skill but shows no visual",
                    root));
            }
        }

        private static bool TryGetGridFootprint(AnimalType type, out UnitSize size)
        {
            size = UnitSize.Small;

            var method = typeof(GridManager).GetMethod(
                "GetFootprint",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null) return false;

            size = (UnitSize)method.Invoke(null, new object[] { type });
            return true;
        }
    }
}
#endif
