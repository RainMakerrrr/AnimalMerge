#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Facades;
using Code.Animals.Health;
using Code.Animals.Movement;
using Code.Animals.UI;
using Code.Animals.Vfx;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalPrefabAssembler
    {
        public static GameObject Assemble(AnimalRecipe recipe, List<ValidationFinding> findings)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(recipe.ModelPrefab);
            if (root == null)
            {
                findings.Add(ValidationFinding.Error(
                    "BUILD", $"{recipe.name}: could not instantiate the model prefab", recipe));
                return null;
            }

            PrefabUtility.UnpackPrefabInstance(
                root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            root.name = recipe.PrefabName;
            root.tag = "Untagged";
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = recipe.RootScale;

            ApplyLayerRecursively(root, recipe.Side.Layer);
            StripVendorRuntimeComponents(root);

            if (!ModelBoundsCalculator.TryCalculateRootLocalBounds(root, out var bounds))
            {
                findings.Add(ValidationFinding.Error(
                    "BUILD",
                    $"{recipe.name}: the model has no MeshRenderer or SkinnedMeshRenderer, so geometry " +
                    "cannot be derived",
                    recipe));
                Object.DestroyImmediate(root);
                return null;
            }

            var footprint = recipe.Footprint.ToUnitSize();
            var geometry = DerivedGeometry.Calculate(bounds, footprint, recipe.RootScale);
            Debug.Log($"[AnimalPrefabTool] {geometry.Describe(recipe.PrefabName)}");

            var animator = EnsureComponent<Animator>(root);
            animator.runtimeAnimatorController = recipe.AnimatorController;
            animator.avatar = recipe.Avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var movement = EnsureComponent<AnimalMovement>(root);
            var attack = EnsureAttackComponent(root, recipe);
            var animalAnimator = EnsureComponent<AnimalAnimator>(root);
            var health = EnsureComponent<AnimalHealth>(root);
            var damagePopup = EnsureComponent<DamagePopupController>(root);
            var facade = EnsureFacade(root, recipe, findings);

            if (facade == null)
            {
                Object.DestroyImmediate(root);
                return null;
            }

            var attackPoint = EnsureAttackPoint(root, recipe, geometry);
            var colliderChild = EnsureColliderChild(root, recipe, geometry);
            var healthBar = EnsureHealthBar(root, recipe, geometry, health, findings);

            WriteMovement(movement, recipe, footprint, animalAnimator);
            WriteAttack(attack, recipe, animalAnimator, attackPoint);
            WriteAnimator(animalAnimator, recipe, animator);
            WriteHealth(health, animalAnimator);
            WriteDamagePopup(damagePopup, recipe, health);

            if (recipe.Side == AnimalSideProfile.Ally)
                AllyPrefabAssembler.Assemble(root, (AllyAnimalRecipe)recipe, facade, colliderChild, findings);

            WriteFacade(facade, recipe, animalAnimator, attack, health, movement, healthBar);

            var authoredScaleAnimators = root.GetComponentsInChildren<MergeScaleAnimator>(true);
            foreach (var authored in authoredScaleAnimators)
                Object.DestroyImmediate(authored);

            return root;
        }

        private static void StripVendorRuntimeComponents(GameObject root)
        {
            foreach (var behaviour in root.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;

                var declaringNamespace = behaviour.GetType().Namespace;
                if (declaringNamespace != null && declaringNamespace.StartsWith("Code.")) continue;

                Debug.Log(
                    $"[AnimalPrefabTool] Removed {behaviour.GetType().Name} from the model root - " +
                    "vendor movement scripts fight AnimalMovement for the transform");

                Object.DestroyImmediate(behaviour);
            }

            var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) Object.DestroyImmediate(agent);

            var characterController = root.GetComponent<CharacterController>();
            if (characterController != null) Object.DestroyImmediate(characterController);

            var body = root.GetComponent<Rigidbody>();
            if (body != null) Object.DestroyImmediate(body);
        }

        private static void ApplyLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                ApplyLayerRecursively(child.gameObject, layer);
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }

        private static AnimalAttack EnsureAttackComponent(GameObject root, AnimalRecipe recipe)
        {
            var desired = recipe.AttackScript == null
                ? typeof(AnimalAttack)
                : recipe.AttackScript.GetClass();

            var existing = root.GetComponent<AnimalAttack>();
            if (existing != null && existing.GetType() == desired) return existing;
            if (existing != null) Object.DestroyImmediate(existing);

            return (AnimalAttack)root.AddComponent(desired);
        }

        private static AnimalFacade EnsureFacade(
            GameObject root,
            AnimalRecipe recipe,
            List<ValidationFinding> findings)
        {
            var desired = recipe.FacadeScript.GetClass();

            var existing = root.GetComponent<AnimalFacade>();
            if (existing != null && existing.GetType() == desired) return existing;
            if (existing != null) Object.DestroyImmediate(existing);

            var added = root.AddComponent(desired) as AnimalFacade;
            if (added == null)
            {
                findings.Add(ValidationFinding.Error(
                    "BUILD", $"{recipe.name}: could not add {desired.Name} to the root", recipe));
            }

            return added;
        }

        private static Transform EnsureAttackPoint(
            GameObject root,
            AnimalRecipe recipe,
            DerivedGeometry geometry)
        {
            var existing = root.transform.Find(AnimalPrefabConstants.AttackPointChildName)
                           ?? root.transform.Find(AnimalPrefabConstants.LegacyAttackPointChildName);

            if (existing == null)
            {
                var created = new GameObject(AnimalPrefabConstants.AttackPointChildName);
                created.transform.SetParent(root.transform, false);
                existing = created.transform;
            }

            existing.name = AnimalPrefabConstants.AttackPointChildName;
            existing.gameObject.layer = recipe.Side.Layer;
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one;
            existing.localPosition = recipe.OverrideAttackPointPosition
                ? recipe.AttackPointLocalPosition
                : geometry.AttackPointLocalPosition;

            return existing;
        }

        private static Transform EnsureColliderChild(
            GameObject root,
            AnimalRecipe recipe,
            DerivedGeometry geometry)
        {
            var name = recipe.Side.ColliderChildName;
            var existing = root.transform.Find(name);

            if (existing == null)
            {
                var created = new GameObject(name);
                created.transform.SetParent(root.transform, false);
                existing = created.transform;
            }

            existing.gameObject.layer = recipe.Side.Layer;
            existing.localPosition = Vector3.zero;
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one;

            var sphere = existing.GetComponent<SphereCollider>();
            if (sphere == null) sphere = existing.gameObject.AddComponent<SphereCollider>();

            sphere.isTrigger = recipe.Side.ColliderIsTrigger;
            sphere.radius = recipe.OverrideColliderShape ? recipe.ColliderRadius : geometry.ColliderRadius;
            sphere.center = recipe.OverrideColliderShape ? recipe.ColliderCenter : geometry.ColliderCenter;

            return existing;
        }

        private static GameObject EnsureHealthBar(
            GameObject root,
            AnimalRecipe recipe,
            DerivedGeometry geometry,
            AnimalHealth health,
            List<ValidationFinding> findings)
        {
            var existing = root.transform.Find(AnimalPrefabConstants.HealthBarChildName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalPrefabPaths.HealthBarPrefab);
            if (asset == null)
            {
                findings.Add(ValidationFinding.Error(
                    "BUILD",
                    $"{recipe.name}: HealthBar prefab not found at {AnimalPrefabPaths.HealthBarPrefab}",
                    recipe));
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            instance.name = AnimalPrefabConstants.HealthBarChildName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var view = instance.GetComponentInChildren<HealthBarView>(true);
            if (view != null) SerializedFieldWriter.SetObject(view, "_health", health);

            var offset = recipe.OverrideHealthBarHeight
                ? recipe.HealthBarHeightOffset
                : geometry.HealthBarHeightOffset;

            foreach (var rotator in instance.GetComponentsInChildren<BillboardRotator>(true))
                SerializedFieldWriter.SetFloat(rotator, "_heightOffset", offset);

            return instance;
        }

        private static void WriteMovement(
            AnimalMovement movement,
            AnimalRecipe recipe,
            Code.GridPathfinding.UnitSize footprint,
            AnimalAnimator animalAnimator)
        {
            SerializedFieldWriter.SetFloat(movement, "_yPos", 0f);
            SerializedFieldWriter.SetFloat(movement, "_zOffset", recipe.ZOffset);
            SerializedFieldWriter.SetUnitSize(movement, "_unitSize", footprint);
            SerializedFieldWriter.SetObject(movement, "_animator", animalAnimator);
            SerializedFieldWriter.ClearArray(movement, "_nodes");
            SerializedFieldWriter.SetFloat(movement, "_raycastOffset", recipe.RaycastOffset);
            SerializedFieldWriter.SetInt(movement, "_sizeEffectY", recipe.SizeEffectY);
            SerializedFieldWriter.SetEnum(movement, "_direction", (int)recipe.Side.Direction);
            SerializedFieldWriter.SetBool(movement, "_debugDrawPath", true);
            SerializedFieldWriter.SetFloat(movement, "_rotationSpeed", AnimalPrefabConstants.RotationSpeed);
            SerializedFieldWriter.SetFloat(movement, "_turnAnimationDuration",
                AnimalPrefabConstants.TurnAnimationDuration);
            SerializedFieldWriter.SetFloat(movement, "_turnDirectionSmoothSpeed",
                AnimalPrefabConstants.TurnDirectionSmoothSpeed);
            SerializedFieldWriter.SetFloat(movement, "_turnAmplification", AnimalPrefabConstants.TurnAmplification);
            SerializedFieldWriter.SetFloat(movement, "_turnAngleThreshold", AnimalPrefabConstants.TurnAngleThreshold);
            SerializedFieldWriter.SetInt(movement, "_tilesPerMoveBaseline", AnimalPrefabConstants.TilesPerMoveBaseline);
            SerializedFieldWriter.SetFloat(movement, "_baseMoveSpeed", AnimalPrefabConstants.BaseMoveSpeed);
            SerializedFieldWriter.SetFloat(movement, "_minAnimSpeed", recipe.MinAnimSpeed);
            SerializedFieldWriter.SetFloat(movement, "_maxAnimSpeed", recipe.MaxAnimSpeed);
            SerializedFieldWriter.SetColor(movement, "_moveRangeColor", recipe.Side.MoveRangeColor);
            SerializedFieldWriter.SetObject(movement, "_currentPathNode", null);
            SerializedFieldWriter.SetObject(movement, "_debugTargetCell", null);
        }

        private static void WriteAttack(
            AnimalAttack attack,
            AnimalRecipe recipe,
            AnimalAnimator animalAnimator,
            Transform attackPoint)
        {
            SerializedFieldWriter.SetObject(attack, "_animator", animalAnimator);
            SerializedFieldWriter.SetObject(attack, "_attackPoint", attackPoint);
            SerializedFieldWriter.SetFloat(attack, "_radius", recipe.AttackRadius);
            SerializedFieldWriter.SetFloat(attack, "_forwardReach", recipe.ForwardReach);
            SerializedFieldWriter.SetInt(attack, "_maxTargets", recipe.MaxTargets);
            SerializedFieldWriter.SetLayerMask(attack, "_mask", recipe.Side.AttackMask);
            SerializedFieldWriter.SetBool(attack, "_isAoE", recipe.IsAoE);
        }

        private static void WriteAnimator(
            AnimalAnimator animalAnimator,
            AnimalRecipe recipe,
            Animator animator)
        {
            SerializedFieldWriter.SetObject(animalAnimator, "_animator", animator);
            SerializedFieldWriter.SetObject(animalAnimator, "_attackClip", recipe.AttackClip);
            SerializedFieldWriter.SetFloat(animalAnimator, "_animatorSpeed", AnimalPrefabConstants.AnimatorSpeed);
        }

        private static void WriteHealth(AnimalHealth health, AnimalAnimator animalAnimator) =>
            SerializedFieldWriter.SetObject(health, "_animator", animalAnimator);

        private static void WriteDamagePopup(
            DamagePopupController controller,
            AnimalRecipe recipe,
            AnimalHealth health)
        {
            SerializedFieldWriter.SetObject(controller, "_health", health);
            SerializedFieldWriter.SetFloat(controller, "_spawnOffsetY", recipe.DamagePopupOffsetY);
            SerializedFieldWriter.SetFloat(controller, "_randomXRange", AnimalPrefabConstants.RandomXRange);

            var popup = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalPrefabPaths.DamagePopupPrefab);
            if (popup == null) return;

            var view = popup.GetComponent<DamagePopupView>();
            if (view != null) SerializedFieldWriter.SetObject(controller, "_popupPrefab", view);
        }

        private static void WriteFacade(
            AnimalFacade facade,
            AnimalRecipe recipe,
            AnimalAnimator animalAnimator,
            AnimalAttack attack,
            AnimalHealth health,
            AnimalMovement movement,
            GameObject healthBar)
        {
            SerializedFieldWriter.SetEnum(facade, "_type", (int)recipe.Type);
            SerializedFieldWriter.SetObject(facade, "_animator", animalAnimator);
            SerializedFieldWriter.SetObject(facade, "_attack", attack);
            SerializedFieldWriter.SetObject(facade, "_health", health);
            SerializedFieldWriter.SetObject(facade, "_movement", movement);

            var healthBarTransform = healthBar == null ? null : healthBar.transform;

            var colliders = facade.GetComponentsInChildren<Collider>(true)
                .Where(c => c.gameObject.layer == recipe.Side.Layer)
                .Where(c => healthBarTransform == null || !c.transform.IsChildOf(healthBarTransform))
                .Cast<Object>()
                .ToList();

            SerializedFieldWriter.SetObjectArray(facade, "_colliders", colliders);
        }
    }
}
#endif
